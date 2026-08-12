namespace Vorlauf.Domain.Foerderung;

/// <summary>
/// Rechenkern (Plan §6). Kein Fördersatz ist hier hartkodiert — alles kommt
/// aus dem versionierten Regelwerk. Ablauf:
/// 1. Regelwerk zum Stichtag wählen (kein Treffer → <see cref="KeinRegelwerkException"/>),
/// 2. förderfähige Kosten über die WE-Staffel deckeln,
/// 3. Bausteine mit Bedingungen auswerten — Boni mit eigener Bemessungsgrundlage
///    (bei Mehrfamilienhäusern nur die erste, selbstgenutzte Wohneinheit),
/// 4. Gesamtsatz deckeln (70 % / 80 %-Regel),
/// 5. unveränderlichen Snapshot erzeugen.
/// </summary>
public sealed class FoerderRechner(TimeProvider zeit)
{
    private readonly TimeProvider _zeit = zeit;

    public Foerderberechnung Berechne(FoerderEingabe eingabe, IReadOnlyCollection<Foerderregelwerk> regelwerke)
    {
        if (eingabe.Wohneinheiten < 1)
            throw new ArgumentOutOfRangeException(nameof(eingabe), "Mindestens eine Wohneinheit.");
        if (eingabe.InvestitionskostenBrutto < 0m)
            throw new ArgumentOutOfRangeException(nameof(eingabe), "Kosten dürfen nicht negativ sein.");

        var regelwerk = WaehleRegelwerk(eingabe.Stichtag, regelwerke);
        var foerderfaehigeKosten = DeckleKosten(eingabe, regelwerk);
        var bonusGrundlage = BonusBemessungsgrundlage(eingabe, regelwerk, foerderfaehigeKosten);

        var positionen = new List<FoerderberechnungPosition>();
        foreach (var baustein in AngewandteBausteine(eingabe, regelwerk))
        {
            var grundlage = baustein.Art == BausteinArt.Grundfoerderung
                ? foerderfaehigeKosten
                : bonusGrundlage;
            positionen.Add(new FoerderberechnungPosition
            {
                Art = baustein.Art,
                Satz = baustein.Satz,
                Bemessungsgrundlage = grundlage,
                Betrag = Runde(baustein.Satz * grundlage),
                Bemerkung = grundlage < foerderfaehigeKosten
                    ? "Nur erste (selbstgenutzte) Wohneinheit"
                    : null,
            });
        }

        var summeSaetze = positionen.Sum(p => p.Satz);
        var maxSatz = ErmittleDeckel(eingabe, regelwerk);
        var gedeckelterSatz = Math.Min(summeSaetze, maxSatz);
        var zuschuss = Math.Min(
            Runde(positionen.Sum(p => p.Betrag)),
            Runde(gedeckelterSatz * foerderfaehigeKosten));

        return new Foerderberechnung
        {
            RegelwerkId = regelwerk.Id,
            RegelwerkBezeichnung = regelwerk.Bezeichnung,
            RegelwerkVerbindlich = regelwerk.Verbindlich,
            ErstelltUtc = _zeit.GetUtcNow().UtcDateTime,
            Eingabe = eingabe,
            FoerderfaehigeKosten = foerderfaehigeKosten,
            GedeckelterSatz = gedeckelterSatz,
            Zuschuss = zuschuss,
            Positionen = positionen,
        };
    }

    private static Foerderregelwerk WaehleRegelwerk(DateOnly stichtag, IReadOnlyCollection<Foerderregelwerk> regelwerke)
    {
        var treffer = regelwerke.Where(r => r.GiltAm(stichtag)).ToList();
        return treffer.Count switch
        {
            0 => throw new KeinRegelwerkException(stichtag),
            1 => treffer[0],
            _ => throw new InvalidOperationException(
                $"Mehrere Regelwerke gültig am {stichtag:dd.MM.yyyy}: " +
                string.Join(" | ", treffer.Select(t => t.Bezeichnung))),
        };
    }

    private static decimal DeckleKosten(FoerderEingabe eingabe, Foerderregelwerk regelwerk)
    {
        var staffelSumme = 0m;
        for (var we = 1; we <= eingabe.Wohneinheiten; we++)
        {
            staffelSumme += GrenzeFuerWohneinheit(regelwerk, we);
        }
        return Math.Min(eingabe.InvestitionskostenBrutto, staffelSumme);
    }

    private static decimal BonusBemessungsgrundlage(FoerderEingabe eingabe, Foerderregelwerk regelwerk, decimal foerderfaehigeKosten)
    {
        // Klimageschwindigkeits- und Einkommensbonus gelten nur für die erste
        // (selbstgenutzte) Wohneinheit — bei einer WE identisch mit der Gesamtbasis.
        return eingabe.Wohneinheiten == 1
            ? foerderfaehigeKosten
            : Math.Min(foerderfaehigeKosten, GrenzeFuerWohneinheit(regelwerk, 1));
    }

    private static decimal GrenzeFuerWohneinheit(Foerderregelwerk regelwerk, int we)
    {
        var grenze = regelwerk.KostenGrenzen.FirstOrDefault(g =>
            g.WohneinheitVon <= we && (g.WohneinheitBis is null || we <= g.WohneinheitBis));
        return grenze?.MaxKostenJeWohneinheit
            ?? throw new InvalidOperationException(
                $"Regelwerk '{regelwerk.Bezeichnung}' hat keine Kostengrenze für Wohneinheit {we}.");
    }

    private static IEnumerable<Foerderbaustein> AngewandteBausteine(FoerderEingabe eingabe, Foerderregelwerk regelwerk)
    {
        var erfuellte = regelwerk.Bausteine
            .Where(b => BedingungenErfuellt(eingabe, BausteinBedingungen.Parse(b.BedingungenJson)))
            .ToList();

        // Einkommens-Staffel: genau eine Zeile darf greifen; bei Überlappung
        // (Datenfehler) zählt der höchste Satz statt einer stillen Summe.
        var einkommen = erfuellte
            .Where(b => b.Art == BausteinArt.Einkommen)
            .OrderByDescending(b => b.Satz)
            .Take(1);

        return erfuellte.Where(b => b.Art != BausteinArt.Einkommen).Concat(einkommen);
    }

    private static bool BedingungenErfuellt(FoerderEingabe e, BausteinBedingungen b)
    {
        if (b.NurSelbstnutzung && !e.Selbstnutzung) return false;
        if (b.Funktionstuechtig && !e.AltheizungFunktionstuechtig) return false;
        if (b.KeineFossileVerbleibt && e.FossileHeizungVerbleibt) return false;

        // Anlagenbedingungen (z. B. Effizienzbonus: natürliches Kältemittel):
        // solange die Aufnahme das Merkmal nicht erfasst, wird der Baustein
        // nicht gewährt — keine stille Annahme zugunsten des Ergebnisses.
        if (b.Anlage is { Length: > 0 }) return false;

        if (b.TypenBeliebigesAlter is not null || b.TypenAbMindestalter is not null)
        {
            if (e.Altheizung is null) return false;
            var typ = e.Altheizung.Value.ToString();

            var beliebig = b.TypenBeliebigesAlter?.Contains(typ, StringComparer.OrdinalIgnoreCase) == true;
            if (!beliebig)
            {
                var abAlter = b.TypenAbMindestalter?.Contains(typ, StringComparer.OrdinalIgnoreCase) == true;
                if (!abAlter) return false;
                if (e.AltheizungInbetriebnahmeJahr is null || b.MindestalterJahre is null) return false;
                if (e.Stichtag.Year - e.AltheizungInbetriebnahmeJahr.Value < b.MindestalterJahre.Value) return false;
            }
        }

        if (b.ZvEMin is not null || b.ZvEMax is not null)
        {
            if (e.ZuVersteuerndesEinkommen is null) return false;
            var zuschlag = e.MinderjaehrigesKindImHaushalt ? b.FamilienzuschlagZvE ?? 0m : 0m;
            var zvE = e.ZuVersteuerndesEinkommen.Value;
            if (b.ZvEMax is not null && zvE > b.ZvEMax.Value + zuschlag) return false;
            if (b.ZvEMin is not null && zvE <= b.ZvEMin.Value + zuschlag) return false;
        }

        return true;
    }

    private static decimal ErmittleDeckel(FoerderEingabe e, Foerderregelwerk regelwerk)
    {
        if (regelwerk.Deckel.Count == 0)
            throw new InvalidOperationException(
                $"Regelwerk '{regelwerk.Bezeichnung}' definiert keinen Förderdeckel.");

        var anwendbar = regelwerk.Deckel.Where(d =>
        {
            if (d.ZvEGrenze is null) return true;
            if (d.NurSelbstnutzung && !e.Selbstnutzung) return false;
            if (e.ZuVersteuerndesEinkommen is null) return false;
            var zuschlag = e.MinderjaehrigesKindImHaushalt ? d.FamilienzuschlagZvE ?? 0m : 0m;
            return e.ZuVersteuerndesEinkommen.Value <= d.ZvEGrenze.Value + zuschlag;
        }).ToList();

        return anwendbar.Count > 0
            ? anwendbar.Max(d => d.MaxSatz)
            : regelwerk.Deckel.Min(d => d.MaxSatz);
    }

    private static decimal Runde(decimal betrag) =>
        Math.Round(betrag, 2, MidpointRounding.AwayFromZero);
}
