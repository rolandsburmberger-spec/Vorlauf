using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vorlauf.Domain.Ablage;
using Vorlauf.Domain.Projekte;
using Vorlauf.Infrastructure.Dokumente;

namespace Vorlauf.Web.Pages.Projekte;

public class AngebotModel(IProjektStore store, TimeProvider zeit, PdfDokumente pdf) : PageModel
{
    public Projekt Projekt { get; private set; } = null!;
    public Vorlauf.Domain.Projekte.Angebot? Angebot { get; private set; }
    public decimal? Zuschuss { get; private set; }

    public IActionResult OnGet(Guid id) => Lade(id);

    public IActionResult OnGetPdf(Guid id)
    {
        var projekt = store.Finde(id);
        if (projekt?.AktuellesAngebot is not { } angebot) return NotFound();
        return File(
            pdf.ErzeugeAngebotsPdf(projekt, angebot, projekt.AktuelleFoerderberechnung),
            "application/pdf",
            $"{angebot.Nummer}.pdf");
    }

    public IActionResult OnPostAnlegen(Guid id)
    {
        var projekt = store.Finde(id);
        if (projekt is null) return NotFound();

        var heute = DateOnly.FromDateTime(zeit.GetUtcNow().UtcDateTime);
        projekt.Angebote.Add(new Vorlauf.Domain.Projekte.Angebot
        {
            Nummer = $"AN-{heute.Year}-{projekt.Angebote.Count + 1:0000}",
            Datum = heute,
            FoerderberechnungId = projekt.AktuelleFoerderberechnung?.Id,
        });
        store.Speichere(projekt);
        return RedirectToPage(new { id });
    }

    public IActionResult OnPostPositionZufuegen(Guid id, string bezeichnung, decimal menge, string einheit, decimal einzelpreis)
    {
        var projekt = store.Finde(id);
        if (projekt?.AktuellesAngebot is not { } angebot) return NotFound();

        angebot.Positionen.Add(new AngebotPosition
        {
            Position = angebot.Positionen.Count + 1,
            Bezeichnung = bezeichnung.Trim(),
            Menge = menge,
            Einheit = string.IsNullOrWhiteSpace(einheit) ? "Stk" : einheit.Trim(),
            EinzelpreisNetto = einzelpreis,
        });
        store.Speichere(projekt);
        return RedirectToPage(new { id });
    }

    public IActionResult OnPostPositionLoeschen(Guid id, Guid positionId)
    {
        var projekt = store.Finde(id);
        if (projekt?.AktuellesAngebot is not { } angebot) return NotFound();

        angebot.Positionen.RemoveAll(p => p.Id == positionId);
        store.Speichere(projekt);
        return RedirectToPage(new { id });
    }

    public IActionResult OnPostFlags(Guid id, bool vorbehalt)
    {
        var projekt = store.Finde(id);
        if (projekt?.AktuellesAngebot is not { } angebot) return NotFound();

        angebot.FoerdervorbehaltsklauselEnthalten = vorbehalt;
        store.Speichere(projekt);
        return RedirectToPage(new { id });
    }

    public IActionResult OnPostAnnehmen(Guid id, DateOnly? vertragsdatum)
    {
        var projekt = store.Finde(id);
        if (projekt?.AktuellesAngebot is not { } angebot) return NotFound();

        angebot.Angenommen = true;
        angebot.Vertragsdatum = vertragsdatum ?? DateOnly.FromDateTime(zeit.GetUtcNow().UtcDateTime);
        store.Speichere(projekt);
        return RedirectToPage(new { id });
    }

    private IActionResult Lade(Guid id)
    {
        var projekt = store.Finde(id);
        if (projekt is null) return NotFound();
        Projekt = projekt;
        Angebot = projekt.AktuellesAngebot;
        Zuschuss = projekt.AktuelleFoerderberechnung?.Zuschuss;
        return Page();
    }
}
