using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vorlauf.Domain.Ablage;
using Vorlauf.Domain.Foerderung;
using Vorlauf.Domain.Projekte;

namespace Vorlauf.Web.Pages.Projekte;

public class AufnahmeModel(IProjektStore store, HeizlastRechner heizlast, TimeProvider zeit) : PageModel
{
    public Projekt Projekt { get; private set; } = null!;
    public Vorlauf.Domain.Projekte.Aufnahme? Aufnahme { get; private set; }
    public IReadOnlyList<string> Hinweise { get; private set; } = [];

    public IActionResult OnGet(Guid id)
    {
        var projekt = store.Finde(id);
        if (projekt is null) return NotFound();
        Projekt = projekt;
        Aufnahme = projekt.Aufnahme;
        if (Aufnahme is not null) Hinweise = HeizlastRechner.Pruefhinweise(Aufnahme);
        return Page();
    }

    public IActionResult OnPost(
        Guid id,
        AltheizungsTyp? altheizungTyp,
        int? altheizungBaujahr,
        bool funktionstuechtig,
        Heizflaechen? heizflaechen,
        int? vorlauf,
        Daemmzustand? daemmzustand,
        string? bemerkung)
    {
        var projekt = store.Finde(id);
        if (projekt is null) return NotFound();

        var aufnahme = projekt.Aufnahme ?? new Vorlauf.Domain.Projekte.Aufnahme();
        aufnahme.AltheizungTyp = altheizungTyp;
        aufnahme.AltheizungBaujahr = altheizungBaujahr;
        aufnahme.AltheizungFunktionstuechtig = funktionstuechtig;
        aufnahme.Heizflaechen = heizflaechen;
        aufnahme.VorlauftemperaturC = vorlauf;
        aufnahme.Daemmzustand = daemmzustand;
        aufnahme.Bemerkung = bemerkung;
        aufnahme.AufgenommenAmUtc = zeit.GetUtcNow().UtcDateTime;

        if (projekt.Gebaeude is { WohnflaecheM2: { } flaeche, Baujahr: { } baujahr } && daemmzustand is { } dz)
        {
            var ergebnis = heizlast.Berechne(flaeche, baujahr, dz);
            aufnahme.BerechneteHeizlastKw = ergebnis.HeizlastKw;
            aufnahme.Geraeteempfehlung = ergebnis.Geraeteempfehlung;
        }

        projekt.Aufnahme = aufnahme;
        store.Speichere(projekt);

        Projekt = projekt;
        Aufnahme = aufnahme;
        Hinweise = HeizlastRechner.Pruefhinweise(aufnahme);
        return Page();
    }
}
