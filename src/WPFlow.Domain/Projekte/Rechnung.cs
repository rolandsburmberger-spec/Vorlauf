namespace WPFlow.Domain.Projekte;

public enum RechnungTyp
{
    Abschlag,
    Teilrechnung,
    Schlussrechnung,
}

public sealed class Rechnung
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Nummer { get; init; }
    public required RechnungTyp Typ { get; init; }
    public required DateOnly Datum { get; init; }
    public List<RechnungPosition> Positionen { get; init; } = [];

    public decimal SummeNetto => Runde(Positionen.Sum(p => p.GesamtNetto));
    public decimal SummeMwSt => Runde(Positionen.Sum(p => p.GesamtNetto * p.MwStSatz));
    public decimal SummeBrutto => SummeNetto + SummeMwSt;

    /// <summary>Erzeugt eine Rechnung mit den Positionen eines Angebots.</summary>
    public static Rechnung AusAngebot(Angebot angebot, string nummer, RechnungTyp typ, DateOnly datum)
    {
        var rechnung = new Rechnung { Nummer = nummer, Typ = typ, Datum = datum };
        rechnung.Positionen.AddRange(angebot.Positionen.Select(p => new RechnungPosition
        {
            Position = p.Position,
            Bezeichnung = p.Bezeichnung,
            Menge = p.Menge,
            Einheit = p.Einheit,
            EinzelpreisNetto = p.EinzelpreisNetto,
            MwStSatz = p.MwStSatz,
        }));
        return rechnung;
    }

    private static decimal Runde(decimal x) => Math.Round(x, 2, MidpointRounding.AwayFromZero);
}

public sealed class RechnungPosition
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
