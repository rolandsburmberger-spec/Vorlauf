namespace WPFlow.Domain.Projekte;

/// <summary>
/// Aggregat-Wurzel der Prozessstrecke (M2: Kunde, Gebäude, Aufnahme,
/// Angebot, Abnahme, Rechnung folgen). Statuswechsel nur über den
/// <see cref="Zustandsautomat"/>, nie durch direktes Setzen.
/// </summary>
public sealed class Projekt
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Bezeichnung { get; set; }
    public ProjektStatus Status { get; private set; } = ProjektStatus.Anfrage;
    public List<ProjektStatusHistorie> Historie { get; init; } = [];

    internal void SetzeStatus(ProjektStatus neu, string benutzer, string? bemerkung, DateTime zeitpunktUtc)
    {
        Historie.Add(new ProjektStatusHistorie
        {
            ProjektId = Id,
            Von = Status,
            Nach = neu,
            Benutzer = benutzer,
            Bemerkung = bemerkung,
            ZeitpunktUtc = zeitpunktUtc,
        });
        Status = neu;
    }
}
