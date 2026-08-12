namespace Vorlauf.Domain.Projekte;

/// <summary>
/// Explizite Übergangstabelle statt verstreuter if-Abfragen (Plan §4).
/// Jeder erfolgreiche Wechsel schreibt die Historie; Guards prüfen
/// Vorbedingungen und blockieren mit benanntem Grund.
/// </summary>
public sealed class Zustandsautomat(TimeProvider zeit, IReadOnlyCollection<IUebergangsGuard>? guards = null)
{
    private readonly TimeProvider _zeit = zeit;
    private readonly IReadOnlyCollection<IUebergangsGuard> _guards = guards ?? [];

    private static readonly IReadOnlySet<(ProjektStatus Von, ProjektStatus Nach)> Uebergaenge = new HashSet<(ProjektStatus, ProjektStatus)>
    {
        (ProjektStatus.Anfrage, ProjektStatus.Aufgenommen),
        (ProjektStatus.Aufgenommen, ProjektStatus.FoerderungGeprueft),
        (ProjektStatus.FoerderungGeprueft, ProjektStatus.Angeboten),
        (ProjektStatus.Angeboten, ProjektStatus.Beauftragt),
        (ProjektStatus.Beauftragt, ProjektStatus.Terminiert),
        (ProjektStatus.Terminiert, ProjektStatus.InMontage),
        (ProjektStatus.InMontage, ProjektStatus.Abgenommen),
        (ProjektStatus.Abgenommen, ProjektStatus.Berechnet),
        (ProjektStatus.Berechnet, ProjektStatus.Abgeschlossen),
        // Verloren: aus jedem Status vor Beauftragt.
        (ProjektStatus.Anfrage, ProjektStatus.Verloren),
        (ProjektStatus.Aufgenommen, ProjektStatus.Verloren),
        (ProjektStatus.FoerderungGeprueft, ProjektStatus.Verloren),
        (ProjektStatus.Angeboten, ProjektStatus.Verloren),
    };

    public static bool IstUebergangDefiniert(ProjektStatus von, ProjektStatus nach) =>
        Uebergaenge.Contains((von, nach));

    public static IReadOnlyList<ProjektStatus> MoeglicheZiele(ProjektStatus von) =>
        Uebergaenge.Where(u => u.Von == von).Select(u => u.Nach).ToList();

    /// <summary>
    /// Wechselt den Status oder wirft: <see cref="UngueltigerUebergangException"/>
    /// (nicht in der Tabelle) bzw. <see cref="UebergangBlockiertException"/>
    /// (Guard-Vorbedingung verletzt). Kein Wechsel ohne Historieneintrag.
    /// </summary>
    public void Wechsle(Projekt projekt, ProjektStatus nach, string benutzer, string? bemerkung = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(benutzer);

        var von = projekt.Status;
        if (!IstUebergangDefiniert(von, nach))
            throw new UngueltigerUebergangException(von, nach);

        foreach (var guard in _guards)
        {
            var ergebnis = guard.Pruefe(projekt, nach);
            if (!ergebnis.Erlaubt)
                throw new UebergangBlockiertException(von, nach, ergebnis.Grund ?? "Vorbedingung nicht erfüllt.");
        }

        projekt.SetzeStatus(nach, benutzer, bemerkung, _zeit.GetUtcNow().UtcDateTime);
    }
}
