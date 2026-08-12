using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vorlauf.Domain.Ablage;
using Vorlauf.Domain.Projekte;

namespace Vorlauf.Web.Pages.Projekte;

public class AbnahmeModel(IProjektStore store, TimeProvider zeit) : PageModel
{
    public Projekt Projekt { get; private set; } = null!;

    public IActionResult OnGet(Guid id)
    {
        var projekt = store.Finde(id);
        if (projekt is null) return NotFound();
        Projekt = projekt;
        return Page();
    }

    public IActionResult OnPost(
        Guid id,
        DateOnly? inbetriebnahme,
        string? protokoll,
        string? unterschriftDataUrl,
        bool fachunternehmererklaerung)
    {
        var projekt = store.Finde(id);
        if (projekt is null) return NotFound();

        var abnahme = projekt.Abnahme ?? new Abnahme();
        abnahme.InbetriebnahmeDatum = inbetriebnahme;
        abnahme.ProtokollText = protokoll;
        if (!string.IsNullOrWhiteSpace(unterschriftDataUrl) && unterschriftDataUrl.StartsWith("data:image/png;base64,", StringComparison.Ordinal))
            abnahme.UnterschriftPngDataUrl = unterschriftDataUrl;
        abnahme.FachunternehmererklaerungAusgestellt = fachunternehmererklaerung;
        abnahme.AbgenommenAmUtc = zeit.GetUtcNow().UtcDateTime;
        projekt.Abnahme = abnahme;
        store.Speichere(projekt);
        return RedirectToPage("/Projekte/Detail", new { id });
    }
}
