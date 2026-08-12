namespace Vorlauf.Domain.Stammdaten;

/// <summary>
/// Stammdaten des ausführenden SHK-Betriebs — die Pflichtangaben nach
/// § 14 UStG für Rechnung, PDF und XRechnung. In Stufe 1 als
/// Seed-Singleton; PLZ und Ort getrennt, weil XRechnung beides
/// als eigene Felder verlangt.
/// </summary>
public sealed class Betrieb
{
    public required string Name { get; init; }
    public string? Inhaber { get; init; }
    public required string Strasse { get; init; }
    public required string Plz { get; init; }
    public required string Ort { get; init; }
    public required string Telefon { get; init; }
    public required string Email { get; init; }

    /// <summary>Steuernummer oder USt-IdNr — mindestens eine ist Pflicht (§ 14 Abs. 4 Nr. 2 UStG).</summary>
    public required string Steuernummer { get; init; }
    public string? UStIdNr { get; init; }

    public required string Iban { get; init; }
    public string? Bic { get; init; }
    public string? Bank { get; init; }
}
