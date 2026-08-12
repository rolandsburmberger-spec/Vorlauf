using Vorlauf.Domain.Foerderung;
using Vorlauf.Domain.Stammdaten;

namespace Vorlauf.Domain.Projekte;

/// <summary>
/// Aggregat-Wurzel der Prozessstrecke. Statuswechsel nur über den
/// <see cref="Zustandsautomat"/>, nie durch direktes Setzen.
/// </summary>
public sealed class Projekt
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Bezeichnung { get; set; }
    public ProjektStatus Status { get; private set; } = ProjektStatus.Anfrage;
    public List<ProjektStatusHistorie> Historie { get; init; } = [];
    public DateTime AngelegtUtc { get; init; }

    public Kunde? Kunde { get; set; }
    public Gebaeude? Gebaeude { get; set; }
    public Aufnahme? Aufnahme { get; set; }
    public List<Foerderberechnung> Foerderberechnungen { get; init; } = [];
    public List<Angebot> Angebote { get; init; } = [];
    public Montagetermin? Montagetermin { get; set; }
    public Abnahme? Abnahme { get; set; }
    public List<Rechnung> Rechnungen { get; init; } = [];

    public Foerderberechnung? AktuelleFoerderberechnung =>
        Foerderberechnungen.Count > 0 ? Foerderberechnungen[^1] : null;

    public Angebot? AktuellesAngebot =>
        Angebote.Count > 0 ? Angebote[^1] : null;

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
