using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vorlauf.Domain.Ablage;
using Vorlauf.Domain.Projekte;

namespace Vorlauf.Web.Pages.Projekte;

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

    /// <summary>Abschlagsrechnung über einen Bruttobetrag (üblich bei Auftragserteilung).</summary>
    public IActionResult OnPostAbschlag(Guid id, decimal betragBrutto)
    {
        var projekt = store.Finde(id);
        if (projekt?.AktuellesAngebot is not { Angenommen: true } angebot) return NotFound();
        if (betragBrutto <= 0) return RedirectToPage(new { id });

        var heute = DateOnly.FromDateTime(zeit.GetUtcNow().UtcDateTime);
        var lfdAbschlag = projekt.Rechnungen.Count(r => r.Typ == RechnungTyp.Abschlag) + 1;
        projekt.Rechnungen.Add(Vorlauf.Domain.Projekte.Rechnung.Abschlag(
            NaechsteNummer(projekt, heute),
            heute,
            $"{lfdAbschlag}. Abschlagszahlung auf Auftrag {angebot.Nummer}",
            betragNetto: Math.Round(betragBrutto / 1.19m, 2, MidpointRounding.AwayFromZero)));
        store.Speichere(projekt);
        return RedirectToPage(new { id });
    }

    public IActionResult OnPostSchlussrechnung(Guid id)
    {
        var projekt = store.Finde(id);
        if (projekt?.AktuellesAngebot is not { Angenommen: true } angebot) return NotFound();

        var heute = DateOnly.FromDateTime(zeit.GetUtcNow().UtcDateTime);
        projekt.Rechnungen.Add(Vorlauf.Domain.Projekte.Rechnung.AusAngebot(
            angebot,
            NaechsteNummer(projekt, heute),
            RechnungTyp.Schlussrechnung,
            heute,
            leistungsdatum: projekt.Abnahme?.InbetriebnahmeDatum));
        store.Speichere(projekt);
        return RedirectToPage(new { id });
    }

    private static string NaechsteNummer(Projekt projekt, DateOnly heute) =>
        $"RE-{heute.Year}-{projekt.Rechnungen.Count + 1:0000}";
}
