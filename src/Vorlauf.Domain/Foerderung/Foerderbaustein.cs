namespace Vorlauf.Domain.Foerderung;

/// <summary>
/// Ein Prozentsatz-Baustein eines Regelwerks. Bedingungen liegen als
/// JSON-Parameter am Baustein (zvE-Grenzen, qualifizierende Heizungstypen,
/// Mindestalter, Selbstnutzung, förderfähige-Kosten-Quote, Gerätebedingungen
/// wie EU-Wertschöpfung ab Q1/2027) — das Schema muss auch Änderungen der
/// Bemessungsgrundlage abbilden können, nicht nur Sätze.
/// </summary>
public sealed class Foerderbaustein
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required BausteinArt Art { get; init; }

    /// <summary>Fördersatz als Anteil, decimal(5,4). Beispiel: 0.16m.</summary>
    public required decimal Satz { get; init; }

    /// <summary>Bedingungsparameter als JSON; null = bedingungslos.</summary>
    public string? BedingungenJson { get; init; }

    public string? Bemerkung { get; init; }
}
