using Vorlauf.Domain.Foerderung;

namespace Vorlauf.Tests.Foerderung;

/// <summary>
/// Pflicht-Testfälle des Förder-Rechenkerns (M1). Sollwerte verifiziert am
/// 08.08.2026 gegen das KfW-Merkblatt 458 (Stand 07/2026, Bestellnr.
/// 600 000 5131). Diese Tests sind bei M0 bewusst rot — sie sind die
/// Sollliste für M1. Ursprünglicher Plan-Testfall 3 („76 %, kein Deckel =
/// 21.280 €") war gegen das Merkblatt unerreichbar und wurde korrigiert:
/// Konstellationen mit 80 %-Deckel erhalten immer den 40 %-Einkommensbonus
/// (86 % → 80 %); 76 % läuft stets in den 70 %-Deckel.
/// </summary>
public class FoerderRechnerTests
{
    private static readonly IReadOnlyCollection<Foerderregelwerk> Regelwerke = SeedRegelwerke.Alle();

    private static readonly DateOnly StichtagNeu = new(2026, 8, 1);

    private static FoerderRechner Rechner() => new(TimeProvider.System);

    private static FoerderEingabe Standardfall() => new()
    {
        Stichtag = StichtagNeu,
        InvestitionskostenBrutto = 30000m,
        Wohneinheiten = 1,
        Selbstnutzung = true,
        ZuVersteuerndesEinkommen = 55000m,
        Altheizung = AltheizungsTyp.Gasheizung,
        AltheizungInbetriebnahmeJahr = 2004,
        AltheizungFunktionstuechtig = true,
        FossileHeizungVerbleibt = false,
    };

    [Fact]
    public void T01_Selbstnutzer_Gasheizung22Jahre_ZvEUeber50k()
    {
        // 30 % Grund + 16 % Klima = 46 % von 28.000 € (Kosten gedeckelt).
        var b = Rechner().Berechne(Standardfall(), Regelwerke);

        Assert.Equal(28000m, b.FoerderfaehigeKosten);
        Assert.Equal(0.46m, b.GedeckelterSatz);
        Assert.Equal(12880m, b.Zuschuss);
    }

    [Fact]
    public void T02_Selbstnutzer_EinKind_ZvE28k_GedeckeltAuf80Prozent()
    {
        // 30 + 16 + 40 (zvE ≤ 30k) = 86 % → Deckel 80 % (Selbstnutzer, zvE ≤ 30k).
        var e = Standardfall() with
        {
            InvestitionskostenBrutto = 28000m,
            ZuVersteuerndesEinkommen = 28000m,
            MinderjaehrigesKindImHaushalt = true,
        };

        var b = Rechner().Berechne(e, Regelwerke);

        Assert.Equal(0.80m, b.GedeckelterSatz);
        Assert.Equal(22400m, b.Zuschuss);
    }

    [Fact]
    public void T03_Selbstnutzer_KeinKind_ZvE35k_GedeckeltAuf70Prozent()
    {
        // Korrigierter Testfall: 30 + 16 + 30 (zvE 30–40k) = 76 % → Deckel 70 %.
        // (Plan-Original „76 %, kein Deckel = 21.280 €" ist laut Merkblatt unmöglich.)
        var e = Standardfall() with
        {
            InvestitionskostenBrutto = 28000m,
            ZuVersteuerndesEinkommen = 35000m,
        };

        var b = Rechner().Berechne(e, Regelwerke);

        Assert.Equal(0.70m, b.GedeckelterSatz);
        Assert.Equal(19600m, b.Zuschuss);
    }

    [Fact]
    public void T04_Vermieter_NurGrundfoerderung()
    {
        var e = Standardfall() with { Selbstnutzung = false, ZuVersteuerndesEinkommen = null };

        var b = Rechner().Berechne(e, Regelwerke);

        Assert.Equal(0.30m, b.GedeckelterSatz);
        Assert.Equal(8400m, b.Zuschuss);
        Assert.Single(b.Positionen);
        Assert.Equal(BausteinArt.Grundfoerderung, b.Positionen[0].Art);
    }

    [Fact]
    public void T05_AltheizungDefekt_KeinKlimabonus()
    {
        var e = Standardfall() with { AltheizungFunktionstuechtig = false };

        var b = Rechner().Berechne(e, Regelwerke);

        Assert.DoesNotContain(b.Positionen, p => p.Art == BausteinArt.Klimageschwindigkeit);
        Assert.Equal(8400m, b.Zuschuss);
    }

    [Fact]
    public void T06_Gasheizung15Jahre_KeinKlimabonus()
    {
        // Gasheizung qualifiziert erst ab 20 Jahren seit Inbetriebnahme.
        var e = Standardfall() with { AltheizungInbetriebnahmeJahr = 2011 };

        var b = Rechner().Berechne(e, Regelwerke);

        Assert.DoesNotContain(b.Positionen, p => p.Art == BausteinArt.Klimageschwindigkeit);
        Assert.Equal(8400m, b.Zuschuss);
    }

    [Fact]
    public void T07_Biomasseheizung25Jahre_KlimabonusJa()
    {
        // Merkblatt 458: auch Biomasseheizung ≥ 20 Jahre qualifiziert
        // (fehlte im ursprünglichen Plan).
        var e = Standardfall() with
        {
            Altheizung = AltheizungsTyp.Biomasseheizung,
            AltheizungInbetriebnahmeJahr = 2001,
        };

        var b = Rechner().Berechne(e, Regelwerke);

        Assert.Contains(b.Positionen, p => p.Art == BausteinArt.Klimageschwindigkeit);
        Assert.Equal(12880m, b.Zuschuss);
    }

    [Fact]
    public void T08_Mfh6We_KostenstaffelUndDeckel()
    {
        // 28.000 + 5 × 15.000 = 103.000 € Kostenbasis. Vermieter → 30 %.
        // Hinweis M1: Boni gelten nur für die selbstgenutzte WE — je Position
        // eigene Bemessungsgrundlage, 70 % auf die Gesamtbasis ist nur der
        // theoretische Deckel (72.100 €).
        var e = Standardfall() with
        {
            InvestitionskostenBrutto = 150000m,
            Wohneinheiten = 6,
            Selbstnutzung = false,
            ZuVersteuerndesEinkommen = null,
        };

        var b = Rechner().Berechne(e, Regelwerke);

        Assert.Equal(103000m, b.FoerderfaehigeKosten);
        Assert.Equal(30900m, b.Zuschuss);
    }

    [Fact]
    public void T09_Stichtag15Juli2026_AltesRegelwerkGreift()
    {
        // Alt-Regelwerk: 30 % Grund + 20 % Klima = 50 % von 30.000 € = 15.000 €
        // (zvE > 40k → kein Einkommensbonus; kein Effizienzbonus-Anspruch).
        var e = Standardfall() with { Stichtag = new DateOnly(2026, 7, 15) };

        var b = Rechner().Berechne(e, Regelwerke);

        Assert.Equal(30000m, b.FoerderfaehigeKosten);
        Assert.Equal(0.50m, b.GedeckelterSatz);
        Assert.Equal(15000m, b.Zuschuss);
    }

    [Fact]
    public void T10_StichtagOhneRegelwerk_DefinierterFehler()
    {
        var e = Standardfall() with { Stichtag = new DateOnly(2023, 6, 1) };

        Assert.Throws<KeinRegelwerkException>(() => Rechner().Berechne(e, Regelwerke));
    }

    [Fact]
    public void T11_KostenUnterGrenze_KeineDeckelungDerKosten()
    {
        var e = Standardfall() with { InvestitionskostenBrutto = 20000m };

        var b = Rechner().Berechne(e, Regelwerke);

        Assert.Equal(20000m, b.FoerderfaehigeKosten);
        Assert.Equal(9200m, b.Zuschuss);
    }

    [Fact]
    public void T12_Degressionsstufe2027_UnverbindlichGekennzeichnet()
    {
        // „Was kostet Warten?": Stichtag 01.03.2027 → Klima nur noch 12 %,
        // Kosten 27.250 € → 42 % = 11.445 € (statt 12.880 €). Regelwerk ist
        // nur angekündigt → Snapshot muss Unverbindlichkeit tragen.
        var e = Standardfall() with { Stichtag = new DateOnly(2027, 3, 1) };

        var b = Rechner().Berechne(e, Regelwerke);

        Assert.False(b.RegelwerkVerbindlich);
        Assert.Equal(27250m, b.FoerderfaehigeKosten);
        Assert.Equal(0.42m, b.GedeckelterSatz);
        Assert.Equal(11445m, b.Zuschuss);
    }

    [Fact]
    public void T13_ZvEExakt30000_Einkommensbonus40ProzentUndDeckel80()
    {
        // Grenze inklusiv: „bis 30.000 €" → 40 %-Stufe und 80 %-Deckel.
        var e = Standardfall() with
        {
            InvestitionskostenBrutto = 28000m,
            ZuVersteuerndesEinkommen = 30000m,
        };

        var b = Rechner().Berechne(e, Regelwerke);

        Assert.Equal(0.80m, b.GedeckelterSatz);
        Assert.Equal(22400m, b.Zuschuss);
    }

    [Fact]
    public void T14_Gasetagenheizung11Jahre_KlimabonusOhneAltersgrenze()
    {
        // Gasetagenheizung qualifiziert altersunabhängig (Merkblatt 458).
        var e = Standardfall() with
        {
            Altheizung = AltheizungsTyp.Gasetagenheizung,
            AltheizungInbetriebnahmeJahr = 2015,
        };

        var b = Rechner().Berechne(e, Regelwerke);

        Assert.Contains(b.Positionen, p => p.Art == BausteinArt.Klimageschwindigkeit);
        Assert.Equal(12880m, b.Zuschuss);
    }

    [Fact]
    public void T15_FossileHeizungVerbleibt_KeinKlimabonus()
    {
        var e = Standardfall() with { FossileHeizungVerbleibt = true };

        var b = Rechner().Berechne(e, Regelwerke);

        Assert.DoesNotContain(b.Positionen, p => p.Art == BausteinArt.Klimageschwindigkeit);
        Assert.Equal(8400m, b.Zuschuss);
    }
}
