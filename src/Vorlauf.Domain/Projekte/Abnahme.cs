namespace Vorlauf.Domain.Projekte;

public sealed class Montagetermin
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateOnly? Start { get; set; }
    public DateOnly? Ende { get; set; }
    public string? Team { get; set; }
}

public sealed class Abnahme
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateOnly? InbetriebnahmeDatum { get; set; }
    public string? ProtokollText { get; set; }

    /// <summary>Unterschrift vom Canvas als PNG-Data-URL.</summary>
    public string? UnterschriftPngDataUrl { get; set; }

    /// <summary>
    /// Harter Guard vor Abgenommen → Berechnet: ohne Fachunternehmererklärung
    /// zahlt die KfW nicht aus.
    /// </summary>
    public bool FachunternehmererklaerungAusgestellt { get; set; }

    public DateTime? AbgenommenAmUtc { get; set; }
}
