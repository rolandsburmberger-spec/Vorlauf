namespace WPFlow.Domain.Foerderung;

/// <summary>
/// Staffel der förderfähigen Höchstkosten je Wohneinheit
/// (Merkblatt 458 ab 21.07.2026: 28.000 € / je 15.000 € für WE 2–6 / je 8.000 € ab WE 7).
/// </summary>
public sealed class FoerderkostenGrenze
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required int WohneinheitVon { get; init; }
    /// <summary>null = offenes Ende der Staffel.</summary>
    public int? WohneinheitBis { get; init; }
    public required decimal MaxKostenJeWohneinheit { get; init; }
}
