using s2industries.ZUGFeRD;
using Vorlauf.Domain.Projekte;
using Vorlauf.Domain.Stammdaten;

namespace Vorlauf.Infrastructure.Dokumente;

/// <summary>
/// XRechnung-Export (EN 16931 / CIUS XRechnung, CII-Syntax) über
/// ZUGFeRD-csharp — nicht selbst gebaut, das Format hat Prüfregeln
/// (Plan §8). Die erzeugten XML werden in der CI gegen den offiziellen
/// KoSIT-Validator geprüft.
/// </summary>
public sealed class XRechnungExport(Betrieb betrieb)
{
    /// <summary>Prüft, ob alle XRechnung-Pflichtangaben am Projekt vorhanden sind.</summary>
    public static string? FehlendeAngabe(Projekt projekt) =>
        string.IsNullOrWhiteSpace(projekt.Kunde?.Email)
            ? "E-Mail-Adresse des Kunden (BT-49 — in der XRechnung Pflicht)"
            : string.IsNullOrWhiteSpace(projekt.Kunde?.PlzOrt ?? projekt.Gebaeude?.PlzOrt)
                ? "Anschrift des Kunden (BT-52/BT-53 — in der XRechnung Pflicht)"
                : null;

    public byte[] Erzeuge(Projekt projekt, Rechnung rechnung)
    {
        if (FehlendeAngabe(projekt) is { } fehlt)
            throw new XRechnungUnvollstaendigException($"Für die XRechnung fehlt: {fehlt}.");

        var d = InvoiceDescriptor.CreateInvoice(
            rechnung.Nummer,
            rechnung.Datum.ToDateTime(TimeOnly.MinValue),
            CurrencyCodes.EUR);

        // BT-23: Geschäftsprozess, in der XRechnung Pflicht
        // (PEPPOL-EN16931-R001). Fehlt er, weist der KoSIT-Validator ab.
        d.BusinessProcess = "urn:fdc:peppol.eu:2017:poacc:billing:01:1.0";

        // BR-DE-15: Käuferreferenz ist in der XRechnung Pflicht. Für
        // Privatkunden gibt es keine Leitweg-ID — Referenz ist die Projektnummer.
        d.ReferenceOrderNo = ProjektReferenz(projekt);

        d.SetSeller(
            name: betrieb.Name,
            postcode: betrieb.Plz,
            city: betrieb.Ort,
            street: betrieb.Strasse,
            country: CountryCodes.DE);
        d.SetSellerContact(
            name: betrieb.Inhaber ?? betrieb.Name,
            emailAddress: betrieb.Email,
            phoneno: betrieb.Telefon);
        if (!string.IsNullOrWhiteSpace(betrieb.UStIdNr))
            d.AddSellerTaxRegistration(betrieb.UStIdNr, TaxRegistrationSchemeID.VA);
        else
            d.AddSellerTaxRegistration(betrieb.Steuernummer, TaxRegistrationSchemeID.FC);

        // BT-34: elektronische Adresse des Verkäufers, in der XRechnung Pflicht.
        d.SetSellerElectronicAddress(betrieb.Email, ElectronicAddressSchemeIdentifiers.ElectronicMailSmtp);

        var (plz, ort) = TrennePlzOrt(projekt.Kunde?.PlzOrt ?? projekt.Gebaeude?.PlzOrt);
        d.SetBuyer(
            name: projekt.Kunde?.Name ?? "Unbekannt",
            postcode: plz,
            city: ort,
            street: projekt.Kunde?.Strasse ?? projekt.Gebaeude?.Strasse ?? "",
            country: CountryCodes.DE);

        // BT-49: elektronische Adresse des Käufers, in der XRechnung Pflicht.
        d.SetBuyerElectronicAddress(projekt.Kunde!.Email!, ElectronicAddressSchemeIdentifiers.ElectronicMailSmtp);

        foreach (var pos in rechnung.Positionen.OrderBy(p => p.Position))
        {
            d.AddTradeLineItem(
                name: pos.Bezeichnung,
                netUnitPrice: pos.EinzelpreisNetto,
                unitCode: QuantityCodes.H87,
                billedQuantity: pos.Menge,
                taxType: TaxTypes.VAT,
                categoryCode: TaxCategoryCodes.S,
                taxPercent: pos.MwStSatz * 100m);
        }

        foreach (var satzGruppe in rechnung.Positionen.GroupBy(p => p.MwStSatz))
        {
            var basis = Math.Round(satzGruppe.Sum(p => p.GesamtNetto), 2, MidpointRounding.AwayFromZero);
            d.AddApplicableTradeTax(
                basisAmount: basis,
                percent: satzGruppe.Key * 100m,
                taxAmount: Math.Round(basis * satzGruppe.Key, 2, MidpointRounding.AwayFromZero),
                typeCode: TaxTypes.VAT,
                categoryCode: TaxCategoryCodes.S);
        }

        // § 14 Abs. 5 UStG: Schlussrechnung setzt gestellte Abschläge ab
        // (BT-113 Vorauszahlung, BT-115 fälliger Betrag).
        var vorausgezahlt = rechnung.Typ == RechnungTyp.Schlussrechnung
            ? Abschlagsverrechnung.AbschlaegeBrutto(projekt.Rechnungen.Where(r => r.Id != rechnung.Id))
            : 0m;
        d.SetTotals(
            lineTotalAmount: rechnung.SummeNetto,
            taxBasisAmount: rechnung.SummeNetto,
            taxTotalAmount: rechnung.SummeMwSt,
            grandTotalAmount: rechnung.SummeBrutto,
            totalPrepaidAmount: vorausgezahlt,
            duePayableAmount: rechnung.SummeBrutto - vorausgezahlt);

        d.SetPaymentMeans(PaymentMeansTypeCodes.SEPACreditTransfer, "Überweisung");
        d.AddCreditorFinancialAccount(
            iban: betrieb.Iban.Replace(" ", ""),
            bic: (betrieb.Bic ?? "").Replace(" ", ""),
            bankName: betrieb.Bank ?? "");
        d.AddTradePaymentTerms(
            $"Zahlbar ohne Abzug bis {rechnung.Datum.AddDays(14):dd.MM.yyyy}.",
            rechnung.Datum.AddDays(14).ToDateTime(TimeOnly.MinValue));

        if (rechnung.Leistungsdatum is { } leistung)
            d.ActualDeliveryDate = leistung.ToDateTime(TimeOnly.MinValue);

        using var stream = new MemoryStream();
        d.Save(stream, ZUGFeRDVersion.Version23, Profile.XRechnung);
        return stream.ToArray();
    }

    /// <summary>Kurze, stabile Projektreferenz für BT-10 (Käuferreferenz).</summary>
    public static string ProjektReferenz(Projekt projekt) =>
        $"VP-{projekt.Id.ToString("N")[..8].ToUpperInvariant()}";

    /// <summary>Am Projekt fehlt eine Pflichtangabe der XRechnung.</summary>
    public sealed class XRechnungUnvollstaendigException(string nachricht) : InvalidOperationException(nachricht);

    private static (string Plz, string Ort) TrennePlzOrt(string? plzOrt)
    {
        if (string.IsNullOrWhiteSpace(plzOrt)) return ("", "");
        var teile = plzOrt.Trim().Split(' ', 2);
        return teile.Length == 2 && teile[0].All(char.IsDigit)
            ? (teile[0], teile[1])
            : ("", plzOrt.Trim());
    }
}
