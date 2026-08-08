namespace WPFlow.Domain.Projekte;

public sealed class ProjektStatusHistorie
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required Guid ProjektId { get; init; }
    public required ProjektStatus Von { get; init; }
    public required ProjektStatus Nach { get; init; }
    public required DateTime ZeitpunktUtc { get; init; }
    public required string Benutzer { get; init; }
    public string? Bemerkung { get; init; }
}
