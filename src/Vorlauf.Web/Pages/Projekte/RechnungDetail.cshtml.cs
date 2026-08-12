using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vorlauf.Domain.Ablage;
using Vorlauf.Domain.Projekte;
using Vorlauf.Infrastructure.Dokumente;

namespace Vorlauf.Web.Pages.Projekte;

public class RechnungDetailModel(IProjektStore store, PdfDokumente pdf, XRechnungExport xrechnung) : PageModel
{
    public Projekt Projekt { get; private set; } = null!;
    public Rechnung Rechnung { get; private set; } = null!;
    public IReadOnlyList<Rechnung> VerrechneteAbschlaege { get; private set; } = [];
    public decimal Restbetrag { get; private set; }

    public IActionResult OnGet(Guid id, Guid rechnungId)
    {
        if (Lade(id, rechnungId) is { } fehler) return fehler;
        return Page();
    }

    public IActionResult OnGetPdf(Guid id, Guid rechnungId)
    {
        if (Lade(id, rechnungId) is { } fehler) return fehler;
        return File(pdf.ErzeugeRechnungsPdf(Projekt, Rechnung), "application/pdf", $"{Rechnung.Nummer}.pdf");
    }

    public IActionResult OnGetXml(Guid id, Guid rechnungId)
    {
        if (Lade(id, rechnungId) is { } fehler) return fehler;
        return File(xrechnung.Erzeuge(Projekt, Rechnung), "application/xml", $"{Rechnung.Nummer}-xrechnung.xml");
    }

    private IActionResult? Lade(Guid id, Guid rechnungId)
    {
        var projekt = store.Finde(id);
        var rechnung = projekt?.Rechnungen.FirstOrDefault(r => r.Id == rechnungId);
        if (projekt is null || rechnung is null) return NotFound();

        Projekt = projekt;
        Rechnung = rechnung;
        VerrechneteAbschlaege = rechnung.Typ == RechnungTyp.Schlussrechnung
            ? projekt.Rechnungen.Where(r => r.Id != rechnung.Id && r.Typ == RechnungTyp.Abschlag).ToList()
            : [];
        Restbetrag = Abschlagsverrechnung.Restbetrag(rechnung, projekt.Rechnungen);
        return null;
    }
}
