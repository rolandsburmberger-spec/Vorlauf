using Microsoft.AspNetCore.Mvc.RazorPages;
using Vorlauf.Domain.Ablage;
using Vorlauf.Domain.Projekte;

namespace Vorlauf.Web.Pages;

/// <summary>Pipeline-Dashboard: wie viele Projekte in welchem Status, wo hakt es.</summary>
public class DashboardModel(IProjektStore store, TimeProvider zeit) : PageModel
{
    public IReadOnlyList<(ProjektStatus Status, int Anzahl)> Pipeline { get; private set; } = [];
    public IReadOnlyList<(Projekt Projekt, int TageImStatus)> Ueberfaellig { get; private set; } = [];

    public void OnGet()
    {
        var projekte = store.Alle();
        Pipeline = Enum.GetValues<ProjektStatus>()
            .Where(s => s is not ProjektStatus.Abgeschlossen and not ProjektStatus.Verloren)
            .Select(s => (s, projekte.Count(p => p.Status == s)))
            .ToList();

        var heute = zeit.GetUtcNow().UtcDateTime;
        Ueberfaellig = projekte
            .Where(p => p.Status is not ProjektStatus.Abgeschlossen and not ProjektStatus.Verloren)
            .Select(p => (Projekt: p, Tage: TageImStatus(p, heute)))
            .Where(x => x.Tage > 14)
            .OrderByDescending(x => x.Tage)
            .ToList();
    }

    private static int TageImStatus(Projekt p, DateTime heuteUtc)
    {
        var seit = p.Historie.Count > 0 ? p.Historie[^1].ZeitpunktUtc : p.AngelegtUtc;
        return (int)(heuteUtc - seit).TotalDays;
    }
}
