namespace Vorlauf.Domain.Stammdaten;

public enum GebaeudeTyp
{
    Einfamilienhaus,
    Zweifamilienhaus,
    Mehrfamilienhaus,
}

public sealed class Gebaeude
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string? Strasse { get; set; }
    public string? PlzOrt { get; set; }
    public int? Baujahr { get; set; }
    public decimal? WohnflaecheM2 { get; set; }
    public int Wohneinheiten { get; set; } = 1;
    public GebaeudeTyp Typ { get; set; } = GebaeudeTyp.Einfamilienhaus;
}
