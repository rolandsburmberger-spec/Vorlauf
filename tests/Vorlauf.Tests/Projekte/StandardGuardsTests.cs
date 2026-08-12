using Vorlauf.Domain.Foerderung;
using Vorlauf.Domain.Projekte;
using Vorlauf.Domain.Stammdaten;

namespace Vorlauf.Tests.Projekte;

public class StandardGuardsTests
{
    private static Zustandsautomat Automat() => new(TimeProvider.System, StandardGuards.Alle());

    private static Projekt Neu() => new()
    {
        Bezeichnung = "WP Musterweg 1",
        Kunde = new Kunde { Name = "Familie Muster", Selbstnutzer = true },
        Gebaeude = new Gebaeude { Baujahr = 1985, WohnflaecheM2 = 140m, Wohneinheiten = 1 },
    };

    private static Aufnahme VollstaendigeAufnahme() => new()
    {
        AltheizungTyp = AltheizungsTyp.Gasheizung,
        AltheizungBaujahr = 2004,
        AltheizungFunktionstuechtig = true,
        Heizflaechen = Heizflaechen.Heizkoerper,
        VorlauftemperaturC = 55,
        Daemmzustand = Daemmzustand.Teilsaniert,
        BerechneteHeizlastKw = 13.3m,
    };

    private static Foerderberechnung Berechnung() => new FoerderRechner(TimeProvider.System).Berechne(
        new FoerderEingabe
        {
            Stichtag = new DateOnly(2026, 8, 1),
            InvestitionskostenBrutto = 32000m,
            Selbstnutzung = true,
            ZuVersteuerndesEinkommen = 55000m,
            Altheizung = AltheizungsTyp.Gasheizung,
            AltheizungInbetriebnahmeJahr = 2004,
            AltheizungFunktionstuechtig = true,
        },
        SeedRegelwerke.Alle());

    private static Angebot Angebot(bool vorbehalt = true) => new()
    {
        Nummer = "AN-2026-001",
        Datum = new DateOnly(2026, 8, 5),
        FoerdervorbehaltsklauselEnthalten = vorbehalt,
        Positionen = { new AngebotPosition { Position = 1, Bezeichnung = "Wärmepumpe 12 kW", EinzelpreisNetto = 18000m } },
    };

    [Fact]
    public void G01_AufnahmeGuard()
    {
        var p = Neu();
        var a = Automat();

        var ex = Assert.Throws<UebergangBlockiertException>(
            () => a.Wechsle(p, ProjektStatus.Aufgenommen, "meister"));
        Assert.Contains(new[] { ex.Grund }, g => g.Contains("Aufnahme"));

        p.Aufnahme = VollstaendigeAufnahme();
        a.Wechsle(p, ProjektStatus.Aufgenommen, "meister");
        Assert.Equal(ProjektStatus.Aufgenommen, p.Status);
    }

    [Fact]
    public void G02_FoerderberechnungGuard()
    {
        var p = Neu();
        p.Aufnahme = VollstaendigeAufnahme();
        var a = Automat();
        a.Wechsle(p, ProjektStatus.Aufgenommen, "meister");

        Assert.Throws<UebergangBlockiertException>(
            () => a.Wechsle(p, ProjektStatus.FoerderungGeprueft, "buero"));

        p.Foerderberechnungen.Add(Berechnung());
        a.Wechsle(p, ProjektStatus.FoerderungGeprueft, "buero");
        Assert.Equal(ProjektStatus.FoerderungGeprueft, p.Status);
    }

    [Fact]
    public void G03_AngebotsGuard_ErzwingtPositionenUndFoerdervorbehalt()
    {
        var p = Neu();
        p.Aufnahme = VollstaendigeAufnahme();
        p.Foerderberechnungen.Add(Berechnung());
        var a = Automat();
        a.Wechsle(p, ProjektStatus.Aufgenommen, "m");
        a.Wechsle(p, ProjektStatus.FoerderungGeprueft, "b");

        Assert.Throws<UebergangBlockiertException>(
            () => a.Wechsle(p, ProjektStatus.Angeboten, "b"));

        p.Angebote.Add(Angebot(vorbehalt: false));
        var ex = Assert.Throws<UebergangBlockiertException>(
            () => a.Wechsle(p, ProjektStatus.Angeboten, "b"));
        Assert.Contains(new[] { ex.Grund }, g => g.Contains("Fördervorbehalt"));

        p.AktuellesAngebot!.FoerdervorbehaltsklauselEnthalten = true;
        a.Wechsle(p, ProjektStatus.Angeboten, "b");
        Assert.Equal(ProjektStatus.Angeboten, p.Status);
    }

    [Fact]
    public void G05_FachunternehmererklaerungIstHarterGuard()
    {
        var p = KomplettBisAbgenommen(out var a);

        var ex = Assert.Throws<UebergangBlockiertException>(
            () => a.Wechsle(p, ProjektStatus.Berechnet, "buero"));
        Assert.Contains(new[] { ex.Grund }, g => g.Contains("Fachunternehmererklärung"));

        p.Abnahme!.FachunternehmererklaerungAusgestellt = true;
        a.Wechsle(p, ProjektStatus.Berechnet, "buero");
        Assert.Equal(ProjektStatus.Berechnet, p.Status);
    }

    [Fact]
    public void G06_KompletteStreckeBisAbgeschlossen()
    {
        var p = KomplettBisAbgenommen(out var a);
        p.Abnahme!.FachunternehmererklaerungAusgestellt = true;
        a.Wechsle(p, ProjektStatus.Berechnet, "buero");

        Assert.Throws<UebergangBlockiertException>(
            () => a.Wechsle(p, ProjektStatus.Abgeschlossen, "buero"));

        p.Rechnungen.Add(Rechnung.AusAngebot(p.AktuellesAngebot!, "RE-2026-001", RechnungTyp.Schlussrechnung, new DateOnly(2026, 10, 1)));
        a.Wechsle(p, ProjektStatus.Abgeschlossen, "buero");

        Assert.Equal(ProjektStatus.Abgeschlossen, p.Status);
        Assert.Equal(9, p.Historie.Count);
    }

    private static Projekt KomplettBisAbgenommen(out Zustandsautomat a)
    {
        var p = Neu();
        p.Aufnahme = VollstaendigeAufnahme();
        p.Foerderberechnungen.Add(Berechnung());
        p.Angebote.Add(Angebot());
        a = Automat();
        a.Wechsle(p, ProjektStatus.Aufgenommen, "m");
        a.Wechsle(p, ProjektStatus.FoerderungGeprueft, "b");
        a.Wechsle(p, ProjektStatus.Angeboten, "b");

        p.AktuellesAngebot!.Angenommen = true;
        p.AktuellesAngebot.Vertragsdatum = new DateOnly(2026, 8, 20);
        a.Wechsle(p, ProjektStatus.Beauftragt, "b");

        p.Montagetermin = new Montagetermin { Start = new DateOnly(2026, 9, 14), Team = "Team A" };
        a.Wechsle(p, ProjektStatus.Terminiert, "b");
        a.Wechsle(p, ProjektStatus.InMontage, "b");

        p.Abnahme = new Abnahme
        {
            InbetriebnahmeDatum = new DateOnly(2026, 9, 16),
            ProtokollText = "Anlage in Betrieb genommen, Einweisung erfolgt.",
            UnterschriftPngDataUrl = "data:image/png;base64,AAAA",
        };
        a.Wechsle(p, ProjektStatus.Abgenommen, "m");
        return p;
    }
}
