using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WPFlow.Domain.Ablage;
using WPFlow.Domain.Projekte;
using WPFlow.Domain.Stammdaten;

namespace WPFlow.Web.Pages.Projekte;

public class NeuModel(IProjektStore store, TimeProvider zeit) : PageModel
{
    public void OnGet()
    {
    }

    public IActionResult OnPost(
        string bezeichnung,
        string kundeName,
        bool selbstnutzer,
        int? baujahr,
        decimal? wohnflaeche,
        int wohneinheiten = 1)
    {
        if (string.IsNullOrWhiteSpace(bezeichnung) || string.IsNullOrWhiteSpace(kundeName))
            return Page();

        var projekt = new Projekt
        {
            Bezeichnung = bezeichnung.Trim(),
            AngelegtUtc = zeit.GetUtcNow().UtcDateTime,
            Kunde = new Kunde { Name = kundeName.Trim(), Selbstnutzer = selbstnutzer },
            Gebaeude = new Gebaeude
            {
                Baujahr = baujahr,
                WohnflaecheM2 = wohnflaeche,
                Wohneinheiten = Math.Max(1, wohneinheiten),
            },
        };
        store.Speichere(projekt);
        return RedirectToPage("/Projekte/Detail", new { id = projekt.Id });
    }
}
