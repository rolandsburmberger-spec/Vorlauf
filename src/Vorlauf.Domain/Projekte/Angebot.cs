namespace Vorlauf.Domain.Projekte;

public sealed class Angebot
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Nummer { get; init; }
    public required DateOnly Datum { get; set; }
    public List<AngebotPosition> Positionen { get; init; } = [];

    /// <summary>Guard-Bedingung für FoerderungGeprueft → Angeboten (KfW-Stolperfalle).</summary>
    public bool FoerdervorbehaltsklauselEnthalten { get; set; }

    public bool Angenommen { get; set; }
    public DateOnly? Vertragsdatum { get; set; }

    /// <summary>Verknüpfte Förderberechnung für den Angebotsblock „Preis nach Förderung".</summary>
    public Guid? FoerderberechnungId { get; set; }

    public decimal SummeNetto => Runde(Positionen.Sum(p => p.GesamtNetto));
    public decimal SummeMwSt => Runde(Positionen.Sum(p => p.GesamtNetto * p.MwStSatz));
    public decimal SummeBrutto => SummeNetto + SummeMwSt;

    public decimal PreisNachFoerderung(decimal zuschuss) => Math.Max(0m, SummeBrutto - zuschuss);

    private static decimal Runde(decimal x) => Math.Round(x, 2, MidpointRounding.AwayFromZero);
}

public sealed class AngebotPosition
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public int Position { get; set; }
    public required string Bezeichnung { get; set; }
    public decimal Menge { get; set; } = 1m;
    public string Einheit { get; set; } = "Stk";
    public decimal EinzelpreisNetto { get; set; }
    public decimal MwStSatz { get; set; } = 0.19m;

    public decimal GesamtNetto => Math.Round(Menge * EinzelpreisNetto, 2, MidpointRounding.AwayFromZero);
}
