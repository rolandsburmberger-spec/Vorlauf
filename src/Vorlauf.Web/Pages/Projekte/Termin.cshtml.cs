using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vorlauf.Domain.Ablage;
using Vorlauf.Domain.Projekte;

namespace Vorlauf.Web.Pages.Projekte;

public class TerminModel(IProjektStore store) : PageModel
{
    public Projekt Projekt { get; private set; } = null!;

    public IActionResult OnGet(Guid id)
    {
        var projekt = store.Finde(id);
        if (projekt is null) return NotFound();
        Projekt = projekt;
        return Page();
    }

    public IActionResult OnPost(Guid id, DateOnly? start, DateOnly? ende, string? team)
    {
        var projekt = store.Finde(id);
        if (projekt is null) return NotFound();

        projekt.Montagetermin ??= new Montagetermin();
        projekt.Montagetermin.Start = start;
        projekt.Montagetermin.Ende = ende;
        projekt.Montagetermin.Team = team;
        store.Speichere(projekt);
        return RedirectToPage("/Projekte/Detail", new { id });
    }
}
