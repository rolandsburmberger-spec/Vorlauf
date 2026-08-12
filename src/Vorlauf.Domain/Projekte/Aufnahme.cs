using Vorlauf.Domain.Foerderung;

namespace Vorlauf.Domain.Projekte;

public enum Heizflaechen
{
    Heizkoerper,
    Flaechenheizung,
    Gemischt,
}

public enum Daemmzustand
{
    Unsaniert,
    Teilsaniert,
    Vollsaniert,
}

/// <summary>
/// Technische Aufnahme vor Ort (Plan §5/§7). „Vollständig" ist die
/// Guard-Bedingung für Anfrage → Aufgenommen.
/// </summary>
public sealed class Aufnahme
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public AltheizungsTyp? AltheizungTyp { get; set; }
    public int? AltheizungBaujahr { get; set; }
    public bool AltheizungFunktionstuechtig { get; set; }

    public Heizflaechen? Heizflaechen { get; set; }
    public int? VorlauftemperaturC { get; set; }
    public Daemmzustand? Daemmzustand { get; set; }

    /// <summary>Überschlag, kein Ersatz für DIN EN 12831.</summary>
    public decimal? BerechneteHeizlastKw { get; set; }
    public string? Geraeteempfehlung { get; set; }

    public string? Bemerkung { get; set; }
    public DateTime? AufgenommenAmUtc { get; set; }

    public bool IstVollstaendig =>
        AltheizungTyp is not null
        && AltheizungBaujahr is not null
        && Heizflaechen is not null
        && VorlauftemperaturC is not null
        && Daemmzustand is not null
        && BerechneteHeizlastKw is not null;
}
