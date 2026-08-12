namespace Vorlauf.Domain.Foerderung;

/// <summary>
/// Deckelung des Gesamtfördersatzes. Merkblatt 458 (ab 21.07.2026):
/// Regel 70 %; 80 % nur für Selbstnutzer mit zvE ≤ 30.000 €
/// (Grenze +10.000 € durch Familienzuschlag).
/// </summary>
public sealed class Foerderdeckel
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required decimal MaxSatz { get; init; }

    /// <summary>null = Regel-Deckel ohne Einkommensbedingung.</summary>
    public decimal? ZvEGrenze { get; init; }
    public bool NurSelbstnutzung { get; init; }

    /// <summary>
    /// Erhöhung der zvE-Grenze bei Familienzuschlag (Merkblatt 458: +10.000 €).
    /// null = kein Familienzuschlag auf diesen Deckel anwendbar.
    /// </summary>
    public decimal? FamilienzuschlagZvE { get; init; }
}
