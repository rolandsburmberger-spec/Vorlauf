namespace Vorlauf.Domain.Projekte;

/// <summary>
/// Konkrete Guards der Prozessstrecke (Plan §4). Jeder Guard prüft genau
/// einen Zielstatus und blockiert mit benanntem Grund — KfW-Stolperfallen
/// werden erzwungen, nicht notiert.
/// </summary>
public static class StandardGuards
{
    public static IReadOnlyCollection<IUebergangsGuard> Alle() =>
    [
        new ZielstatusGuard(ProjektStatus.Aufgenommen, p =>
            p.Aufnahme is { IstVollstaendig: true }
                ? null
                : "Aufnahme unvollständig oder Heizlast nicht berechnet."),

        new ZielstatusGuard(ProjektStatus.FoerderungGeprueft, p =>
            p.AktuelleFoerderberechnung is not null
                ? null
                : "Keine gespeicherte Förderberechnung mit gültigem Regelwerk."),

        new ZielstatusGuard(ProjektStatus.Angeboten, p =>
            p.AktuellesAngebot is null || p.AktuellesAngebot.Positionen.Count == 0
                ? "Angebot fehlt oder hat keine Positionen."
                : p.AktuellesAngebot.FoerdervorbehaltsklauselEnthalten
                    ? null
                    : "Fördervorbehaltsklausel ist nicht im Angebot/Vertrag bestätigt."),

        new ZielstatusGuard(ProjektStatus.Beauftragt, p =>
            p.AktuellesAngebot is { Angenommen: true, Vertragsdatum: not null }
                ? null
                : "Angebot nicht angenommen oder Vertragsdatum fehlt."),

        new ZielstatusGuard(ProjektStatus.Terminiert, p =>
            p.Montagetermin is { Start: not null } && !string.IsNullOrWhiteSpace(p.Montagetermin.Team)
                ? null
                : "Montagetermin oder Team fehlt."),

        new ZielstatusGuard(ProjektStatus.InMontage, p =>
            p.Montagetermin is { Start: not null }
                ? null
                : "Kein Startdatum gesetzt."),

        new ZielstatusGuard(ProjektStatus.Abgenommen, p =>
            p.Abnahme is { InbetriebnahmeDatum: not null }
            && !string.IsNullOrWhiteSpace(p.Abnahme.ProtokollText)
            && !string.IsNullOrWhiteSpace(p.Abnahme.UnterschriftPngDataUrl)
                ? null
                : "Inbetriebnahmedatum, Abnahmeprotokoll oder Unterschrift fehlt."),

        new ZielstatusGuard(ProjektStatus.Berechnet, p =>
            p.Abnahme is { FachunternehmererklaerungAusgestellt: true }
                ? null
                : "Fachunternehmererklärung nicht ausgestellt — ohne sie zahlt die KfW nicht aus."),

        new ZielstatusGuard(ProjektStatus.Abgeschlossen, p =>
            p.Rechnungen.Any(r => r.Typ == RechnungTyp.Schlussrechnung)
                ? null
                : "Keine Schlussrechnung erzeugt."),
    ];

    /// <summary>Guard, der nur für einen bestimmten Zielstatus prüft. Rückgabe null = erlaubt.</summary>
    private sealed class ZielstatusGuard(ProjektStatus ziel, Func<Projekt, string?> pruefung) : IUebergangsGuard
    {
        public GuardErgebnis Pruefe(Projekt projekt, ProjektStatus nach)
        {
            if (nach != ziel) return GuardErgebnis.Ok();
            var grund = pruefung(projekt);
            return grund is null ? GuardErgebnis.Ok() : GuardErgebnis.Blockiert(grund);
        }
    }
}
