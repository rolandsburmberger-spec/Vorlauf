namespace WPFlow.Domain.Projekte;

/// <summary>
/// Explizite Übergangstabelle statt verstreuter if-Abfragen (Plan §4).
/// Guards (M2) prüfen Vorbedingungen wie „Fachunternehmererklärung
/// ausgestellt" vor Abgenommen → Berechnet.
/// </summary>
public static class Zustandsautomat
{
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

    /// <summary>M2: Guard-Auswertung je Übergang (Plan §4), dann Historie schreiben.</summary>
    public static void Wechsle(Projekt projekt, ProjektStatus nach, string benutzer, string? bemerkung = null)
    {
        throw new NotImplementedException("M2: Guards und Statuswechsel sind noch nicht implementiert.");
    }
}
