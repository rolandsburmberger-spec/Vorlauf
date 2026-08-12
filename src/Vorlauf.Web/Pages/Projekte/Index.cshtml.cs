using Microsoft.AspNetCore.Mvc.RazorPages;
using Vorlauf.Domain.Ablage;
using Vorlauf.Domain.Projekte;

namespace Vorlauf.Web.Pages.Projekte;

public class ProjektlisteModel(IProjektStore store) : PageModel
{
    public IReadOnlyList<Projekt> Projekte { get; private set; } = [];
    public ProjektStatus? Status { get; private set; }
    public IReadOnlyList<ProjektStatus> AlleStatus { get; } = Enum.GetValues<ProjektStatus>();

    public void OnGet(ProjektStatus? status)
    {
        Status = status;
        Projekte = status is null
            ? store.Alle()
            : store.Alle().Where(p => p.Status == status).ToList();
    }
}
