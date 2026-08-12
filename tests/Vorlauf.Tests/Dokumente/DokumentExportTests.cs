using System.Text;
using System.Xml.Linq;
using Vorlauf.Domain.Projekte;
using Vorlauf.Domain.Stammdaten;
using Vorlauf.Infrastructure.Dokumente;

namespace Vorlauf.Tests.Dokumente;

/// <summary>
/// Abschlagsverrechnung (§ 14 Abs. 5 UStG) und Struktur der Exporte.
/// Die vollständige XRechnung-Validierung läuft in der CI gegen den
/// offiziellen KoSIT-Validator (Job „xrechnung-kosit").
/// </summary>
public class DokumentExportTests
{
    private static Betrieb TestBetrieb() => new()
    {
        Name = "SHK Musterhaus GmbH",
        Inhaber = "Max Musterhaus",
        Strasse = "Handwerkerring 12",
        Plz = "36037",
        Ort = "Fulda",
        Telefon = "+49 661 1234567",
        Email = "info@shk-musterhaus.example",
        Steuernummer = "018 838 08150",
        UStIdNr = "DE136589744",
        Iban = "DE89 3704 0044 0532 0130 00",
        Bic = "COBADEFFXXX",
        Bank = "Commerzbank Fulda",
    };

    private static Projekt TestProjekt()
    {
        var projekt = new Projekt
        {
            Bezeichnung = "WP Musterweg 1, Fulda",
            AngelegtUtc = new DateTime(2026, 5, 4, 8, 0, 0, DateTimeKind.Utc),
            Kunde = new Kunde { Name = "Familie Muster", Strasse = "Musterweg 1", PlzOrt = "36037 Fulda", Email = "familie.muster@example.com", Selbstnutzer = true },
            Gebaeude = new Gebaeude { Strasse = "Musterweg 1", PlzOrt = "36037 Fulda", Baujahr = 1994, WohnflaecheM2 = 160m },
        };
        projekt.Rechnungen.Add(Rechnung.Abschlag(
            "RE-2026-0001", new DateOnly(2026, 6, 15), "1. Abschlagszahlung auf Auftrag AN-2026-0001", 10000m));

        var schluss = new Rechnung
        {
            Nummer = "RE-2026-0002",
            Typ = RechnungTyp.Schlussrechnung,
            Datum = new DateOnly(2026, 8, 11),
            Leistungsdatum = new DateOnly(2026, 8, 10),
        };
        schluss.Positionen.Add(new RechnungPosition
        {
            Position = 1,
            Bezeichnung = "Wärmepumpe 12 kW inkl. Montage und Inbetriebnahme",
            Menge = 1m,
            Einheit = "Pausch.",
            EinzelpreisNetto = 26800m,
        });
        projekt.Rechnungen.Add(schluss);
        return projekt;
    }

    [Fact]
    public void Abschlag_rechnet_brutto_aus_netto()
    {
        var abschlag = Rechnung.Abschlag("RE-1", new DateOnly(2026, 6, 15), "Abschlag", 10000m);

        Assert.Equal(10000m, abschlag.SummeNetto);
        Assert.Equal(1900m, abschlag.SummeMwSt);
        Assert.Equal(11900m, abschlag.SummeBrutto);
    }

    [Fact]
    public void Schlussrechnung_setzt_Abschlaege_ab()
    {
        var projekt = TestProjekt();
        var schluss = projekt.Rechnungen.Single(r => r.Typ == RechnungTyp.Schlussrechnung);

        // 26.800 € netto + 19 % = 31.892 € brutto; Abschlag 11.900 € brutto
        Assert.Equal(31892m, schluss.SummeBrutto);
        Assert.Equal(11900m, Abschlagsverrechnung.AbschlaegeBrutto(projekt.Rechnungen.Where(r => r.Id != schluss.Id)));
        Assert.Equal(19992m, Abschlagsverrechnung.Restbetrag(schluss, projekt.Rechnungen));
    }

    [Fact]
    public void XRechnung_enthaelt_Pflichtfelder_und_Verrechnung()
    {
        var projekt = TestProjekt();
        var schluss = projekt.Rechnungen.Single(r => r.Typ == RechnungTyp.Schlussrechnung);

        var bytes = new XRechnungExport(TestBetrieb()).Erzeuge(projekt, schluss);
        var xml = Encoding.UTF8.GetString(bytes);
        var dokument = XDocument.Parse(xml); // wohlgeformt

        Assert.Equal("CrossIndustryInvoice", dokument.Root!.Name.LocalName);
        Assert.Contains("xrechnung_3.0", xml);            // CIUS XRechnung
        Assert.Contains("RE-2026-0002", xml);             // Rechnungsnummer
        Assert.Contains("DE89370400440532013000", xml);   // IBAN ohne Leerzeichen
        Assert.Contains("31892.00", xml);                 // Brutto
        Assert.Contains("11900.00", xml);                 // Vorauszahlung (BT-113)
        Assert.Contains("19992.00", xml);                 // fälliger Betrag (BT-115)
        Assert.Contains(XRechnungExport.ProjektReferenz(projekt), xml); // Käuferreferenz (BT-10)

        // BT-34/BT-49: elektronische Adressen beider Seiten — in der XRechnung Pflicht.
        // Ihr Fehlen hat die KoSIT-Validierung in der CI zu Fall gebracht.
        Assert.Contains("info@shk-musterhaus.example", xml);
        Assert.Matches(@"BuyerTradeParty[\s\S]*?URIUniversalCommunication[\s\S]*?familie\.muster@example\.com", xml);

        // BR-CO-26: ohne Verkäufer-Kennung (hier BT-31, Schema VA) lehnt der
        // KoSIT-Validator ab — die Steuernummer (BT-32/FC) allein reicht nicht.
        Assert.Matches(@"SpecifiedTaxRegistration[\s\S]*?schemeID=""VA""[^>]*>DE136589744", xml);
    }

    [Fact]
    public void XRechnung_ohne_KundenEmail_wird_abgelehnt()
    {
        var projekt = TestProjekt();
        projekt.Kunde!.Email = null;
        var schluss = projekt.Rechnungen.Single(r => r.Typ == RechnungTyp.Schlussrechnung);

        Assert.NotNull(XRechnungExport.FehlendeAngabe(projekt));
        var ex = Assert.Throws<XRechnungExport.XRechnungUnvollstaendigException>(
            () => new XRechnungExport(TestBetrieb()).Erzeuge(projekt, schluss));
        Assert.Contains("BT-49", ex.Message);
    }

    [Fact]
    public void Pdf_wird_fuer_Angebot_und_Rechnung_erzeugt()
    {
        var projekt = TestProjekt();
        var schluss = projekt.Rechnungen.Single(r => r.Typ == RechnungTyp.Schlussrechnung);
        var pdf = new PdfDokumente(TestBetrieb());

        var rechnungPdf = pdf.ErzeugeRechnungsPdf(projekt, schluss);
        Assert.True(rechnungPdf.Length > 5000);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(rechnungPdf[..4]));

        var angebot = new Angebot { Nummer = "AN-2026-0001", Datum = new DateOnly(2026, 5, 10) };
        angebot.Positionen.Add(new AngebotPosition { Position = 1, Bezeichnung = "Wärmepumpe 12 kW", EinzelpreisNetto = 26800m });
        var angebotPdf = pdf.ErzeugeAngebotsPdf(projekt, angebot, foerderung: null);
        Assert.True(angebotPdf.Length > 5000);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(angebotPdf[..4]));
    }
}
