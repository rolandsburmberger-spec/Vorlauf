using System.Text.Json;

namespace Vorlauf.Domain.Foerderung;

/// <summary>
/// Deserialisierte Bedingungsparameter eines <see cref="Foerderbaustein"/>.
/// Unbekannte JSON-Felder werden ignoriert; nicht abbildbare Bedingungen
/// (z. B. Anlagenmerkmale, die die Aufnahme noch nicht erfasst) führen dazu,
/// dass der Baustein NICHT gewährt wird — nie zu einer stillen Annahme.
/// </summary>
public sealed record BausteinBedingungen
{
    public bool NurSelbstnutzung { get; init; }
    public bool Funktionstuechtig { get; init; }
    public bool KeineFossileVerbleibt { get; init; }

    public decimal? ZvEMin { get; init; }
    public decimal? ZvEMax { get; init; }

    /// <summary>Erhöhung der zvE-Grenzen bei minderjährigem Kind (Merkblatt: 10.000 €).</summary>
    public decimal? FamilienzuschlagZvE { get; init; }

    public string[]? TypenBeliebigesAlter { get; init; }
    public string[]? TypenAbMindestalter { get; init; }
    public int? MindestalterJahre { get; init; }

    /// <summary>Anlagenbedingungen (z. B. Effizienzbonus im Alt-Regelwerk).</summary>
    public string[]? Anlage { get; init; }

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static BausteinBedingungen Parse(string? json) =>
        string.IsNullOrWhiteSpace(json)
            ? new BausteinBedingungen()
            : JsonSerializer.Deserialize<BausteinBedingungen>(json, Options)
              ?? new BausteinBedingungen();
}
