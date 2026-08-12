using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vorlauf.Domain.Ablage;
using Vorlauf.Domain.Projekte;

namespace Vorlauf.Web.Pages.Projekte;

public class DetailModel(IProjektStore store, Zustandsautomat automat) : PageModel
{
    public Projekt Projekt { get; private set; } = null!;
    public IReadOnlyList<ProjektStatus> MoeglicheZiele { get; private set; } = [];

    public IActionResult OnGet(Guid id)
    {
        var projekt = store.Finde(id);
        if (projekt is null) return NotFound();
        Projekt = projekt;
        MoeglicheZiele = Zustandsautomat.MoeglicheZiele(projekt.Status);
        return Page();
    }

    public IActionResult OnPostWechsel(Guid id, ProjektStatus ziel)
    {
        var projekt = store.Finde(id);
        if (projekt is null) return NotFound();

        try
        {
            automat.Wechsle(projekt, ziel, User.Identity?.Name ?? "unbekannt");
            store.Speichere(projekt);
            TempData["Ok"] = $"Status gewechselt: {ziel}";
        }
        catch (UebergangBlockiertException ex)
        {
            TempData["Fehler"] = ex.Grund;
        }
        catch (UngueltigerUebergangException ex)
        {
            TempData["Fehler"] = ex.Message;
        }

        return RedirectToPage(new { id });
    }
}
