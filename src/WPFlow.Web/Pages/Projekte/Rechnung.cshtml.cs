using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WPFlow.Domain.Ablage;
using WPFlow.Domain.Projekte;

namespace WPFlow.Web.Pages.Projekte;

public class RechnungModel(IProjektStore store, TimeProvider zeit) : PageModel
{
    public Projekt Projekt { get; private set; } = null!;

    public IActionResult OnGet(Guid id)
    {
        var projekt = store.Finde(id);
        if (projekt is null) return NotFound();
        Projekt = projekt;
        return Page();
    }

    public IActionResult OnPostSchlussrechnung(Guid id)
    {
        var projekt = store.Finde(id);
        if (projekt?.AktuellesAngebot is not { Angenommen: true } angebot) return NotFound();

        var heute = DateOnly.FromDateTime(zeit.GetUtcNow().UtcDateTime);
        projekt.Rechnungen.Add(WPFlow.Domain.Projekte.Rechnung.AusAngebot(
            angebot,
            $"RE-{heute.Year}-{projekt.Rechnungen.Count + 1:0000}",
            RechnungTyp.Schlussrechnung,
            heute));
        store.Speichere(projekt);
        return RedirectToPage(new { id });
    }
}
