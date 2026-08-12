// Erzeugt eine Beispiel-XRechnung (Schlussrechnung mit verrechnetem
// Abschlag) für die KoSIT-Validierung in der CI.
// Aufruf: dotnet run --project tools/Vorlauf.Beispiele -- <ausgabepfad>
using Vorlauf.Domain.Projekte;
using Vorlauf.Domain.Stammdaten;
using Vorlauf.Infrastructure.Dokumente;

var pfad = args.Length > 0 ? args[0] : "xrechnung-beispiel.xml";

var betrieb = new Betrieb
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

var projekt = new Projekt
{
    Bezeichnung = "WP Musterweg 1, Fulda",
    AngelegtUtc = new DateTime(2026, 5, 4, 8, 0, 0, DateTimeKind.Utc),
    Kunde = new Kunde
    {
        Name = "Familie Muster",
        Strasse = "Musterweg 1",
        PlzOrt = "36037 Fulda",
        Email = "familie.muster@example.com",
        Selbstnutzer = true,
    },
    Gebaeude = new Gebaeude { Strasse = "Musterweg 1", PlzOrt = "36037 Fulda", Baujahr = 1994, WohnflaecheM2 = 160m },
};

projekt.Rechnungen.Add(Rechnung.Abschlag(
    "RE-2026-0001", new DateOnly(2026, 6, 15),
    "1. Abschlagszahlung auf Auftrag AN-2026-0001", betragNetto: 10000m));

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

var xml = new XRechnungExport(betrieb).Erzeuge(projekt, schluss);
var vollerPfad = Path.GetFullPath(pfad);
Directory.CreateDirectory(Path.GetDirectoryName(vollerPfad)!);
File.WriteAllBytes(vollerPfad, xml);
Console.WriteLine($"XRechnung-Beispiel geschrieben: {vollerPfad} ({xml.Length} Bytes)");

// Optional: Beispiel-PDFs (Rechnung, Angebot) für Sichtprüfung.
if (args.Length > 1)
{
    var pdf = new PdfDokumente(betrieb);
    var rechnungPfad = Path.GetFullPath(args[1]);
    File.WriteAllBytes(rechnungPfad, pdf.ErzeugeRechnungsPdf(projekt, schluss));
    Console.WriteLine($"Rechnungs-PDF geschrieben: {rechnungPfad}");

    if (args.Length > 2)
    {
        var angebot = new Angebot { Nummer = "AN-2026-0001", Datum = new DateOnly(2026, 5, 10), FoerdervorbehaltsklauselEnthalten = true };
        angebot.Positionen.Add(new AngebotPosition
        {
            Position = 1,
            Bezeichnung = "Wärmepumpe 12 kW inkl. Montage und Inbetriebnahme",
            Menge = 1m,
            Einheit = "Pausch.",
            EinzelpreisNetto = 26800m,
        });
        var angebotPfad = Path.GetFullPath(args[2]);
        File.WriteAllBytes(angebotPfad, pdf.ErzeugeAngebotsPdf(projekt, angebot, foerderung: null));
        Console.WriteLine($"Angebots-PDF geschrieben: {angebotPfad}");
    }
}
