namespace WPFlow.Domain.Foerderung;

/// <summary>
/// Versioniertes Regelwerk. Ein Stichtag trifft genau ein Regelwerk
/// (GueltigVon &lt;= Stichtag &lt; GueltigBis); kein Treffer ist ein Fehler,
/// nie eine stille Annahme.
/// </summary>
public sealed class Foerderregelwerk
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Bezeichnung { get; init; }
    public required DateOnly GueltigVon { get; init; }
    public DateOnly? GueltigBis { get; init; }
    public required string Fundstelle { get; init; }

    /// <summary>
    /// False für vorbereitete, lediglich angekündigte Stufen (z. B. Degression
    /// 01.02.2027) — die UI muss solche Ergebnisse als unverbindlich kennzeichnen.
    /// </summary>
    public bool Verbindlich { get; init; } = true;

    public List<Foerderbaustein> Bausteine { get; init; } = [];
    public List<FoerderkostenGrenze> KostenGrenzen { get; init; } = [];
    public List<Foerderdeckel> Deckel { get; init; } = [];

    public bool GiltAm(DateOnly stichtag) =>
        GueltigVon <= stichtag && (GueltigBis is null || stichtag < GueltigBis);
}
