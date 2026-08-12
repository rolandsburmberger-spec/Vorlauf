namespace Vorlauf.Domain.Foerderung;

/// <summary>
/// Eingangswerte einer Förderberechnung. Wird vollständig im Snapshot
/// (<see cref="Foerderberechnung"/>) festgehalten.
/// </summary>
public sealed record FoerderEingabe
{
    /// <summary>Maßgeblich ist der (geplante) Tag der Antragstellung.</summary>
    public required DateOnly Stichtag { get; init; }

    public required decimal InvestitionskostenBrutto { get; init; }

    public int Wohneinheiten { get; init; } = 1;

    public bool Selbstnutzung { get; init; }

    /// <summary>
    /// Durchschnitt des zu versteuernden Haushaltseinkommens aus den
    /// Steuerbescheiden des 2. und 3. Jahres vor Antragstellung.
    /// null = kein Einkommensbonus beantragt/nachweisbar.
    /// </summary>
    public decimal? ZuVersteuerndesEinkommen { get; init; }

    /// <summary>
    /// Familienzuschlag: Kind unter 18 mit Hauptwohnsitz in der Wohneinheit
    /// und Kindergeldberechtigung (Merkblatt 458). Erhöht die zvE-Grenzen
    /// einmalig pauschal um 10.000 €.
    /// </summary>
    public bool MinderjaehrigesKindImHaushalt { get; init; }

    public AltheizungsTyp? Altheizung { get; init; }
    public int? AltheizungInbetriebnahmeJahr { get; init; }
    public bool AltheizungFunktionstuechtig { get; init; }

    /// <summary>Klimageschwindigkeitsbonus entfällt, wenn eine fossile Heizung verbleibt.</summary>
    public bool FossileHeizungVerbleibt { get; init; }
}
