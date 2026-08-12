namespace Vorlauf.Domain.Foerderung;

/// <summary>
/// Seed-Regelwerke. Zahlen verifiziert am 08.08.2026 gegen das
/// KfW-Merkblatt 458 (Stand 07/2026, Bestellnr. 600 000 5131) —
/// Sekundärquellen genügen nicht als Implementierungsgrundlage.
/// </summary>
public static class SeedRegelwerke
{
    /// <summary>BEG EM Heizungsförderung, Fassung bis 20.07.2026.</summary>
    public static Foerderregelwerk Alt2024() => new()
    {
        Bezeichnung = "BEG EM Heizungsförderung (KfW 458), Fassung 2024 – 20.07.2026",
        GueltigVon = new DateOnly(2024, 1, 1),
        GueltigBis = new DateOnly(2026, 7, 21),
        Fundstelle = "KfW-Merkblatt 458, Fassungen bis 07/2026",
        Bausteine =
        [
            new() { Art = BausteinArt.Grundfoerderung, Satz = 0.30m },
            new()
            {
                Art = BausteinArt.Klimageschwindigkeit, Satz = 0.20m,
                BedingungenJson = KlimabonusBedingungen,
            },
            new()
            {
                Art = BausteinArt.Effizienz, Satz = 0.05m,
                BedingungenJson = """{"anlage":["natuerlichesKaeltemittel","erdwaerme","wasser","abwasser"]}""",
            },
            new()
            {
                Art = BausteinArt.Einkommen, Satz = 0.30m,
                BedingungenJson = """{"nurSelbstnutzung":true,"zvEMax":40000}""",
            },
        ],
        KostenGrenzen =
        [
            new() { WohneinheitVon = 1, WohneinheitBis = 1, MaxKostenJeWohneinheit = 30000m },
            new() { WohneinheitVon = 2, WohneinheitBis = 6, MaxKostenJeWohneinheit = 15000m },
            new() { WohneinheitVon = 7, WohneinheitBis = null, MaxKostenJeWohneinheit = 8000m },
        ],
        Deckel = [new() { MaxSatz = 0.70m, ZvEGrenze = null }],
    };

    /// <summary>
    /// BEG EM Heizungsförderung ab 21.07.2026 (Merkblatt 458, Stand 07/2026):
    /// Klimageschwindigkeitsbonus 16 %, Effizienzbonus und EMZ entfallen,
    /// Einkommensbonus gestaffelt 40/30/10 %, Familienzuschlag (+10.000 € auf
    /// die zvE-Grenzen), Kosten 28.000 €, Deckel 70 % bzw. 80 % (Selbstnutzer,
    /// zvE ≤ 30.000 € — Grenze +10.000 € mit Familienzuschlag).
    /// </summary>
    public static Foerderregelwerk Juli2026() => new()
    {
        Bezeichnung = "BEG EM Heizungsförderung (KfW 458), Fassung ab 21.07.2026",
        GueltigVon = new DateOnly(2026, 7, 21),
        GueltigBis = new DateOnly(2027, 2, 1),
        Fundstelle = "KfW-Merkblatt 458, Stand 07/2026, Bestellnr. 600 000 5131",
        Bausteine =
        [
            new() { Art = BausteinArt.Grundfoerderung, Satz = 0.30m },
            new()
            {
                Art = BausteinArt.Klimageschwindigkeit, Satz = 0.16m,
                BedingungenJson = KlimabonusBedingungen,
            },
            new()
            {
                Art = BausteinArt.Einkommen, Satz = 0.40m,
                BedingungenJson = """{"nurSelbstnutzung":true,"zvEMax":30000,"familienzuschlagZvE":10000}""",
            },
            new()
            {
                Art = BausteinArt.Einkommen, Satz = 0.30m,
                BedingungenJson = """{"nurSelbstnutzung":true,"zvEMin":30000,"zvEMax":40000,"familienzuschlagZvE":10000}""",
            },
            new()
            {
                Art = BausteinArt.Einkommen, Satz = 0.10m,
                BedingungenJson = """{"nurSelbstnutzung":true,"zvEMin":40000,"zvEMax":50000,"familienzuschlagZvE":10000}""",
            },
        ],
        KostenGrenzen =
        [
            new() { WohneinheitVon = 1, WohneinheitBis = 1, MaxKostenJeWohneinheit = 28000m },
            new() { WohneinheitVon = 2, WohneinheitBis = 6, MaxKostenJeWohneinheit = 15000m },
            new() { WohneinheitVon = 7, WohneinheitBis = null, MaxKostenJeWohneinheit = 8000m },
        ],
        Deckel =
        [
            new() { MaxSatz = 0.70m, ZvEGrenze = null },
            new() { MaxSatz = 0.80m, ZvEGrenze = 30000m, NurSelbstnutzung = true, FamilienzuschlagZvE = 10000m },
        ],
    };

    /// <summary>
    /// ACHTUNG: Nur angekündigt, nicht richtlinienfest (Verbindlich = false).
    /// Die Q1/2027-Eckpunkte (WP-Grundförderung 15 % + 15 % EU-Wertschöpfungsbonus,
    /// Austauschlogik: Altanlage vor 2008 → nur 25 % der Kosten förderfähig,
    /// ab 2008 → gar nicht) können diese Stufen noch verändern. UI muss
    /// Ergebnisse daraus als unverbindlich kennzeichnen. Kalendereintrag je
    /// Stichtag: Regelwerk gegen die dann gültige Fassung prüfen.
    /// </summary>
    public static IReadOnlyList<Foerderregelwerk> AngekuendigteDegressionsstufen()
    {
        var stufen = new List<Foerderregelwerk>();
        var klimaSatz = 0.12m;
        var kosten1We = 27250m;
        var von = new DateOnly(2027, 2, 1);

        while (klimaSatz >= 0m)
        {
            var bis = von.AddMonths(6);
            var bausteine = new List<Foerderbaustein>
            {
                new() { Art = BausteinArt.Grundfoerderung, Satz = 0.30m },
                new()
                {
                    Art = BausteinArt.Einkommen, Satz = 0.40m,
                    BedingungenJson = """{"nurSelbstnutzung":true,"zvEMax":30000,"familienzuschlagZvE":10000}""",
                },
                new()
                {
                    Art = BausteinArt.Einkommen, Satz = 0.30m,
                    BedingungenJson = """{"nurSelbstnutzung":true,"zvEMin":30000,"zvEMax":40000,"familienzuschlagZvE":10000}""",
                },
                new()
                {
                    Art = BausteinArt.Einkommen, Satz = 0.10m,
                    BedingungenJson = """{"nurSelbstnutzung":true,"zvEMin":40000,"zvEMax":50000,"familienzuschlagZvE":10000}""",
                },
            };
            if (klimaSatz > 0m)
            {
                bausteine.Insert(1, new Foerderbaustein
                {
                    Art = BausteinArt.Klimageschwindigkeit, Satz = klimaSatz,
                    BedingungenJson = KlimabonusBedingungen,
                });
            }

            stufen.Add(new Foerderregelwerk
            {
                Bezeichnung = $"BEG EM Heizungsförderung, angekündigte Stufe ab {von:dd.MM.yyyy}",
                GueltigVon = von,
                GueltigBis = klimaSatz > 0m ? bis : null,
                Fundstelle = "Degression laut Merkblatt 458 (Stand 07/2026); Q1/2027-Reform offen",
                Verbindlich = false,
                Bausteine = bausteine,
                KostenGrenzen =
                [
                    new() { WohneinheitVon = 1, WohneinheitBis = 1, MaxKostenJeWohneinheit = kosten1We },
                    new() { WohneinheitVon = 2, WohneinheitBis = 6, MaxKostenJeWohneinheit = 15000m },
                    new() { WohneinheitVon = 7, WohneinheitBis = null, MaxKostenJeWohneinheit = 8000m },
                ],
                Deckel =
                [
                    new() { MaxSatz = 0.70m, ZvEGrenze = null },
                    new() { MaxSatz = 0.80m, ZvEGrenze = 30000m, NurSelbstnutzung = true, FamilienzuschlagZvE = 10000m },
                ],
            });

            klimaSatz -= 0.04m;
            kosten1We -= 750m;
            von = bis;
        }

        return stufen;
    }

    public static IReadOnlyList<Foerderregelwerk> Alle() =>
        [Alt2024(), Juli2026(), .. AngekuendigteDegressionsstufen()];

    /// <summary>
    /// Merkblatt 458: Selbstnutzer, Altheizung funktionstüchtig, keine fossile
    /// Heizung verbleibt; Öl/Kohle/Gasetagen/Nachtspeicher altersunabhängig,
    /// Gas- und Biomasseheizung ab 20 Jahren seit Inbetriebnahme.
    /// </summary>
    private const string KlimabonusBedingungen =
        """{"nurSelbstnutzung":true,"funktionstuechtig":true,"keineFossileVerbleibt":true,"typenBeliebigesAlter":["Oelheizung","Kohleheizung","Gasetagenheizung","Nachtspeicherheizung"],"typenAbMindestalter":["Gasheizung","Biomasseheizung"],"mindestalterJahre":20}""";
}
