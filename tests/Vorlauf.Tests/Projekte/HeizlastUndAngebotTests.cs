using Vorlauf.Domain.Projekte;

namespace Vorlauf.Tests.Projekte;

public class HeizlastUndAngebotTests
{
    [Fact]
    public void H01_Altbau1965Unsaniert_150WattProM2()
    {
        var e = HeizlastRechner.MitStandardtabelle().Berechne(150m, 1965, Daemmzustand.Unsaniert);

        Assert.Equal(150m, e.WattProM2);
        Assert.Equal(22.5m, e.HeizlastKw);
    }

    [Fact]
    public void H02_Baujahr2005Teilsaniert()
    {
        var e = HeizlastRechner.MitStandardtabelle().Berechne(120m, 2005, Daemmzustand.Teilsaniert);

        Assert.Equal(55m, e.WattProM2);
        Assert.Equal(6.6m, e.HeizlastKw);
    }

    [Fact]
    public void H03_HeizkoerperMitHohemVorlauf_Pruefhinweis()
    {
        var aufnahme = new Aufnahme { Heizflaechen = Heizflaechen.Heizkoerper, VorlauftemperaturC = 65 };

        var hinweise = HeizlastRechner.Pruefhinweise(aufnahme);

        Assert.Contains(hinweise, h => h.Contains("55 °C"));
    }

    [Fact]
    public void H04_UngueltigeWohnflaeche_Fehler()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => HeizlastRechner.MitStandardtabelle().Berechne(0m, 1990, Daemmzustand.Teilsaniert));
    }

    [Fact]
    public void A01_AngebotssummenUndPreisNachFoerderung()
    {
        var angebot = new Angebot
        {
            Nummer = "AN-1",
            Datum = new DateOnly(2026, 8, 5),
            Positionen =
            {
                new AngebotPosition { Position = 1, Bezeichnung = "Wärmepumpe", Menge = 1m, EinzelpreisNetto = 10000m },
                new AngebotPosition { Position = 2, Bezeichnung = "Montage", Menge = 3m, Einheit = "Tag", EinzelpreisNetto = 500m },
            },
        };

        Assert.Equal(11500m, angebot.SummeNetto);
        Assert.Equal(2185m, angebot.SummeMwSt);
        Assert.Equal(13685m, angebot.SummeBrutto);
        Assert.Equal(805m, angebot.PreisNachFoerderung(12880m));
    }

    [Fact]
    public void A02_RechnungAusAngebot_UebernimmtPositionenUndSummen()
    {
        var angebot = new Angebot
        {
            Nummer = "AN-1",
            Datum = new DateOnly(2026, 8, 5),
            Positionen = { new AngebotPosition { Position = 1, Bezeichnung = "Wärmepumpe", EinzelpreisNetto = 18000m } },
        };

        var rechnung = Rechnung.AusAngebot(angebot, "RE-1", RechnungTyp.Schlussrechnung, new DateOnly(2026, 10, 1));

        Assert.Single(rechnung.Positionen);
        Assert.Equal(angebot.SummeBrutto, rechnung.SummeBrutto);
        Assert.Equal(RechnungTyp.Schlussrechnung, rechnung.Typ);
    }
}
