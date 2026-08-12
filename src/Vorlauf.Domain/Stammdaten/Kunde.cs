namespace Vorlauf.Domain.Stammdaten;

public sealed class Kunde
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Name { get; set; }
    public string? Telefon { get; set; }
    public string? Email { get; set; }
    public string? Strasse { get; set; }
    public string? PlzOrt { get; set; }

    /// <summary>Selbstnutzer-Flag — Voraussetzung für Klima- und Einkommensbonus.</summary>
    public bool Selbstnutzer { get; set; }
}
