using WPFlow.Domain.Ablage;
using WPFlow.Domain.Foerderung;
using WPFlow.Domain.Projekte;
using WPFlow.Domain.Stammdaten;

namespace WPFlow.Web.Services;

/// <summary>
/// Fiktive Demo-Projekte über alle Zustände. Der Seed läuft durch die echten
/// Guards und den echten Rechenkern — er ist damit gleichzeitig ein
/// Integrations-Smoke-Test beim Anwendungsstart.
/// </summary>
public static class DemoSeed
{
    public static void Fuelle(
        IProjektStore store,
        FoerderRechner rechner,
        Zustandsautomat automat,
        IReadOnlyList<Foerderregelwerk> regelwerke)
    {
        if (store.Alle().Count > 0) return;

        var stichtag = new DateOnly(2026, 8, 1);

        // 1: frische Anfrage
        store.Speichere(Neu("WP Ahornweg 3, Fulda", "Familie Bergmann", 1972, 145m));

        // 2: Anfrage mit Aufnahme, noch nicht weitergeschaltet
        var p2 = Neu("WP Lindenstraße 12, Petersberg", "Ehepaar Wolf", 1988, 130m);
        p2.Aufnahme = Aufnahme(AltheizungsTyp.Oelheizung, 1998, 130m, 1988);
        store.Speichere(p2);

        // 3: Aufgenommen
        var p3 = Neu("WP Am Rosenhang 7, Künzell", "Familie Sturm", 1965, 160m);
        p3.Aufnahme = Aufnahme(AltheizungsTyp.Gasheizung, 2003, 160m, 1965);
        automat.Wechsle(p3, ProjektStatus.Aufgenommen, "demo", "Aufnahme vor Ort");
        store.Speichere(p3);

        // 4: Förderung geprüft (Selbstnutzer, 1 Kind, niedriges zvE → 80 %-Deckel)
        var p4 = Neu("WP Birkenallee 21, Eichenzell", "Familie Keller", 1979, 120m);
        p4.Aufnahme = Aufnahme(AltheizungsTyp.Gasetagenheizung, 2010, 120m, 1979);
        automat.Wechsle(p4, ProjektStatus.Aufgenommen, "demo");
        p4.Foerderberechnungen.Add(rechner.Berechne(new FoerderEingabe
        {
            Stichtag = stichtag,
            InvestitionskostenBrutto = 29500m,
            Selbstnutzung = true,
            ZuVersteuerndesEinkommen = 28000m,
            MinderjaehrigesKindImHaushalt = true,
            Altheizung = AltheizungsTyp.Gasetagenheizung,
            AltheizungInbetriebnahmeJahr = 2010,
            AltheizungFunktionstuechtig = true,
        }, regelwerke));
        automat.Wechsle(p4, ProjektStatus.FoerderungGeprueft, "demo");
        store.Speichere(p4);

        // 5: Angeboten
        var p5 = Neu("WP Talblick 5, Hünfeld", "Herr Vogel", 1995, 140m);
        p5.Aufnahme = Aufnahme(AltheizungsTyp.Gasheizung, 2005, 140m, 1995);
        automat.Wechsle(p5, ProjektStatus.Aufgenommen, "demo");
        p5.Foerderberechnungen.Add(Standardberechnung(rechner, regelwerke, stichtag, 31000m));
        automat.Wechsle(p5, ProjektStatus.FoerderungGeprueft, "demo");
        p5.Angebote.Add(Angebot("AN-2026-0105", p5.AktuelleFoerderberechnung!.Id, 18500m));
        automat.Wechsle(p5, ProjektStatus.Angeboten, "demo");
        store.Speichere(p5);

        // 6: Beauftragt + terminiert
        var p6 = Neu("WP Wiesengrund 9, Fulda", "Familie Brandt", 1983, 150m);
        p6.Aufnahme = Aufnahme(AltheizungsTyp.Oelheizung, 1996, 150m, 1983);
        automat.Wechsle(p6, ProjektStatus.Aufgenommen, "demo");
        p6.Foerderberechnungen.Add(Standardberechnung(rechner, regelwerke, stichtag, 34000m));
        automat.Wechsle(p6, ProjektStatus.FoerderungGeprueft, "demo");
        p6.Angebote.Add(Angebot("AN-2026-0106", p6.AktuelleFoerderberechnung!.Id, 21000m));
        automat.Wechsle(p6, ProjektStatus.Angeboten, "demo");
        p6.AktuellesAngebot!.Angenommen = true;
        p6.AktuellesAngebot.Vertragsdatum = new DateOnly(2026, 8, 4);
        automat.Wechsle(p6, ProjektStatus.Beauftragt, "demo");
        p6.Montagetermin = new Montagetermin { Start = new DateOnly(2026, 9, 7), Ende = new DateOnly(2026, 9, 9), Team = "Team Nord" };
        automat.Wechsle(p6, ProjektStatus.Terminiert, "demo");
        store.Speichere(p6);

        // 7: In Montage
        var p7 = Neu("WP Steinweg 2, Großenlüder", "Frau Albrecht", 1958, 135m);
        p7.Aufnahme = Aufnahme(AltheizungsTyp.Nachtspeicherheizung, 1990, 135m, 1958);
        automat.Wechsle(p7, ProjektStatus.Aufgenommen, "demo");
        p7.Foerderberechnungen.Add(Standardberechnung(rechner, regelwerke, stichtag, 27500m));
        automat.Wechsle(p7, ProjektStatus.FoerderungGeprueft, "demo");
        p7.Angebote.Add(Angebot("AN-2026-0107", p7.AktuelleFoerderberechnung!.Id, 17200m));
        automat.Wechsle(p7, ProjektStatus.Angeboten, "demo");
        p7.AktuellesAngebot!.Angenommen = true;
        p7.AktuellesAngebot.Vertragsdatum = new DateOnly(2026, 7, 27);
        automat.Wechsle(p7, ProjektStatus.Beauftragt, "demo");
        p7.Montagetermin = new Montagetermin { Start = new DateOnly(2026, 8, 5), Team = "Team Süd" };
        automat.Wechsle(p7, ProjektStatus.Terminiert, "demo");
        automat.Wechsle(p7, ProjektStatus.InMontage, "demo");
        store.Speichere(p7);

        // 8: Abgeschlossen (kompletter Durchlauf)
        var p8 = Neu("WP Kirschblütenweg 14, Fulda", "Familie Winter", 1970, 128m);
        p8.Aufnahme = Aufnahme(AltheizungsTyp.Gasheizung, 2001, 128m, 1970);
        automat.Wechsle(p8, ProjektStatus.Aufgenommen, "demo");
        p8.Foerderberechnungen.Add(Standardberechnung(rechner, regelwerke, new DateOnly(2026, 7, 22), 30000m));
        automat.Wechsle(p8, ProjektStatus.FoerderungGeprueft, "demo");
        p8.Angebote.Add(Angebot("AN-2026-0092", p8.AktuelleFoerderberechnung!.Id, 19800m));
        automat.Wechsle(p8, ProjektStatus.Angeboten, "demo");
        p8.AktuellesAngebot!.Angenommen = true;
        p8.AktuellesAngebot.Vertragsdatum = new DateOnly(2026, 7, 24);
        automat.Wechsle(p8, ProjektStatus.Beauftragt, "demo");
        p8.Montagetermin = new Montagetermin { Start = new DateOnly(2026, 7, 29), Ende = new DateOnly(2026, 7, 31), Team = "Team Nord" };
        automat.Wechsle(p8, ProjektStatus.Terminiert, "demo");
        automat.Wechsle(p8, ProjektStatus.InMontage, "demo");
        p8.Abnahme = new Abnahme
        {
            InbetriebnahmeDatum = new DateOnly(2026, 7, 31),
            ProtokollText = "Anlage in Betrieb genommen, Einweisung erfolgt, hydraulischer Abgleich dokumentiert.",
            UnterschriftPngDataUrl = "data:image/png;base64,iVBORw0KGgo=",
            FachunternehmererklaerungAusgestellt = true,
        };
        automat.Wechsle(p8, ProjektStatus.Abgenommen, "demo");
        automat.Wechsle(p8, ProjektStatus.Berechnet, "demo");
        p8.Rechnungen.Add(Rechnung.AusAngebot(p8.AktuellesAngebot, "RE-2026-0088", RechnungTyp.Schlussrechnung, new DateOnly(2026, 8, 3)));
        automat.Wechsle(p8, ProjektStatus.Abgeschlossen, "demo");
        store.Speichere(p8);
    }

    private static Projekt Neu(string bezeichnung, string kunde, int baujahr, decimal flaeche) => new()
    {
        Bezeichnung = bezeichnung,
        AngelegtUtc = DateTime.UtcNow,
        Kunde = new Kunde { Name = kunde, Selbstnutzer = true },
        Gebaeude = new Gebaeude { Baujahr = baujahr, WohnflaecheM2 = flaeche, Wohneinheiten = 1 },
    };

    private static Aufnahme Aufnahme(AltheizungsTyp typ, int baujahrHeizung, decimal flaeche, int baujahrGebaeude)
    {
        var heizlast = HeizlastRechner.MitStandardtabelle()
            .Berechne(flaeche, baujahrGebaeude, Daemmzustand.Teilsaniert);
        return new Aufnahme
        {
            AltheizungTyp = typ,
            AltheizungBaujahr = baujahrHeizung,
            AltheizungFunktionstuechtig = true,
            Heizflaechen = WPFlow.Domain.Projekte.Heizflaechen.Heizkoerper,
            VorlauftemperaturC = 55,
            Daemmzustand = Daemmzustand.Teilsaniert,
            BerechneteHeizlastKw = heizlast.HeizlastKw,
            Geraeteempfehlung = heizlast.Geraeteempfehlung,
            AufgenommenAmUtc = DateTime.UtcNow,
        };
    }

    private static Foerderberechnung Standardberechnung(
        FoerderRechner rechner, IReadOnlyList<Foerderregelwerk> regelwerke, DateOnly stichtag, decimal kosten) =>
        rechner.Berechne(new FoerderEingabe
        {
            Stichtag = stichtag,
            InvestitionskostenBrutto = kosten,
            Selbstnutzung = true,
            ZuVersteuerndesEinkommen = 55000m,
            Altheizung = AltheizungsTyp.Gasheizung,
            AltheizungInbetriebnahmeJahr = 2003,
            AltheizungFunktionstuechtig = true,
        }, regelwerke);

    private static Angebot Angebot(string nummer, Guid berechnungId, decimal wpPreis) => new()
    {
        Nummer = nummer,
        Datum = new DateOnly(2026, 8, 3),
        FoerdervorbehaltsklauselEnthalten = true,
        FoerderberechnungId = berechnungId,
        Positionen =
        {
            new AngebotPosition { Position = 1, Bezeichnung = "Luft-Wasser-Wärmepumpe inkl. Zubehör", EinzelpreisNetto = wpPreis },
            new AngebotPosition { Position = 2, Bezeichnung = "Montage und Inbetriebnahme", Menge = 3m, Einheit = "Tag", EinzelpreisNetto = 950m },
            new AngebotPosition { Position = 3, Bezeichnung = "Demontage und Entsorgung Altanlage", EinzelpreisNetto = 1200m },
        },
    };
}
