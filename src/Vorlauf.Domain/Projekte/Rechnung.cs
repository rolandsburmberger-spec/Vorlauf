namespace Vorlauf.Domain.Projekte;

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

    /// <summary>Leistungs-/Lieferdatum (§ 14 Abs. 4 Nr. 6 UStG); bei Abschlägen offen.</summary>
    public DateOnly? Leistungsdatum { get; set; }

    public List<RechnungPosition> Positionen { get; init; } = [];

    public decimal SummeNetto => Runde(Positionen.Sum(p => p.GesamtNetto));
    public decimal SummeMwSt => Runde(Positionen.Sum(p => p.GesamtNetto * p.MwStSatz));
    public decimal SummeBrutto => SummeNetto + SummeMwSt;

    /// <summary>Erzeugt eine Rechnung mit den Positionen eines Angebots.</summary>
    public static Rechnung AusAngebot(Angebot angebot, string nummer, RechnungTyp typ, DateOnly datum, DateOnly? leistungsdatum = null)
    {
        var rechnung = new Rechnung { Nummer = nummer, Typ = typ, Datum = datum, Leistungsdatum = leistungsdatum };
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

    /// <summary>Erzeugt eine Abschlagsrechnung mit einer Pauschalposition (Betrag netto).</summary>
    public static Rechnung Abschlag(string nummer, DateOnly datum, string bezeichnung, decimal betragNetto, decimal mwstSatz = 0.19m)
    {
        var rechnung = new Rechnung { Nummer = nummer, Typ = RechnungTyp.Abschlag, Datum = datum };
        rechnung.Positionen.Add(new RechnungPosition
        {
            Position = 1,
            Bezeichnung = bezeichnung,
            Menge = 1m,
            Einheit = "Pausch.",
            EinzelpreisNetto = betragNetto,
            MwStSatz = mwstSatz,
        });
        return rechnung;
    }

    private static decimal Runde(decimal x) => Math.Round(x, 2, MidpointRounding.AwayFromZero);
}

/// <summary>
/// § 14 Abs. 5 UStG: Die Schlussrechnung muss bereits gestellte
/// Abschläge absetzen. Die Verrechnung ist reine Anzeige-/Exportlogik —
/// die gespeicherten Rechnungen bleiben unverändert.
/// </summary>
public static class Abschlagsverrechnung
{
    public static decimal AbschlaegeBrutto(IEnumerable<Rechnung> rechnungen) =>
        rechnungen.Where(r => r.Typ == RechnungTyp.Abschlag).Sum(r => r.SummeBrutto);

    public static decimal Restbetrag(Rechnung schlussrechnung, IEnumerable<Rechnung> alleRechnungen) =>
        schlussrechnung.SummeBrutto - AbschlaegeBrutto(alleRechnungen.Where(r => r.Id != schlussrechnung.Id));
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
