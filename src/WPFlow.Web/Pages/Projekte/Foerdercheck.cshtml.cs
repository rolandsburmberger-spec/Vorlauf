using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WPFlow.Domain.Ablage;
using WPFlow.Domain.Foerderung;
using WPFlow.Domain.Projekte;

namespace WPFlow.Web.Pages.Projekte;

/// <summary>
/// Herzstück: Zuschuss zum Stichtag, Snapshot-Speicherung und der
/// „Was kostet Warten?"-Vergleich zweier Stichtage.
/// </summary>
public class FoerdercheckModel(
    IProjektStore store,
    FoerderRechner rechner,
    IReadOnlyList<Foerderregelwerk> regelwerke,
    TimeProvider zeit) : PageModel
{
    public Projekt Projekt { get; private set; } = null!;
    public DateOnly Stichtag { get; private set; }
    public DateOnly? VergleichsStichtag { get; private set; }
    public decimal Kosten { get; private set; }
    public decimal? ZvE { get; private set; }
    public bool Kind { get; private set; }

    public Foerderberechnung? Ergebnis { get; private set; }
    public Foerderberechnung? Vergleich { get; private set; }
    public decimal Differenz => (Ergebnis?.Zuschuss ?? 0m) - (Vergleich?.Zuschuss ?? 0m);
    public decimal? PreisNachFoerderung { get; private set; }
    public string? Fehler { get; private set; }

    public string Disclaimer =>
        "Unverbindliche Orientierung. Maßgeblich sind die KfW-Bedingungen am Tag der Antragstellung.";

    public IActionResult OnGet(Guid id)
    {
        var projekt = store.Finde(id);
        if (projekt is null) return NotFound();
        Projekt = projekt;
        Stichtag = DateOnly.FromDateTime(zeit.GetUtcNow().UtcDateTime);
        Kosten = projekt.AktuellesAngebot?.SummeBrutto ?? 30000m;
        return Page();
    }

    public IActionResult OnPost(
        Guid id,
        DateOnly stichtag,
        decimal kosten,
        decimal? zvE,
        bool kind,
        DateOnly? vergleichsStichtag)
    {
        var projekt = store.Finde(id);
        if (projekt is null) return NotFound();
        Projekt = projekt;
        Stichtag = stichtag;
        VergleichsStichtag = vergleichsStichtag;
        Kosten = kosten;
        ZvE = zvE;
        Kind = kind;

        var eingabe = new FoerderEingabe
        {
            Stichtag = stichtag,
            InvestitionskostenBrutto = kosten,
            Wohneinheiten = projekt.Gebaeude?.Wohneinheiten ?? 1,
            Selbstnutzung = projekt.Kunde?.Selbstnutzer ?? false,
            ZuVersteuerndesEinkommen = zvE,
            MinderjaehrigesKindImHaushalt = kind,
            Altheizung = projekt.Aufnahme?.AltheizungTyp,
            AltheizungInbetriebnahmeJahr = projekt.Aufnahme?.AltheizungBaujahr,
            AltheizungFunktionstuechtig = projekt.Aufnahme?.AltheizungFunktionstuechtig ?? false,
        };

        try
        {
            Ergebnis = rechner.Berechne(eingabe, regelwerke);
            projekt.Foerderberechnungen.Add(Ergebnis);
            store.Speichere(projekt);

            if (projekt.AktuellesAngebot is { } angebot)
                PreisNachFoerderung = angebot.PreisNachFoerderung(Ergebnis.Zuschuss);

            if (vergleichsStichtag is { } vs)
                Vergleich = rechner.Berechne(eingabe with { Stichtag = vs }, regelwerke);
        }
        catch (KeinRegelwerkException ex)
        {
            Fehler = ex.Message;
        }

        return Page();
    }
}
