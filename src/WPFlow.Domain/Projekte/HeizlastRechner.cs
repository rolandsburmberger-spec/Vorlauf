namespace WPFlow.Domain.Projekte;

/// <summary>
/// Transparenter Heizlast-Überschlag (Plan §7): Wohnfläche × spezifischer
/// Heizwärmebedarf nach Baualtersklasse und Dämmzustand. Werte als
/// Konfigurationstabelle, nicht hartkodiert im Rechner — gleiche Philosophie
/// wie beim Förderkern. Ersetzt keine normgerechte Heizlastberechnung.
/// </summary>
public sealed class HeizlastRechner(IReadOnlyList<HeizlastTabellenzeile> tabelle)
{
    public static HeizlastRechner MitStandardtabelle() => new(Standardtabelle);

    /// <summary>Spezifischer Heizwärmebedarf in W/m² je Baualtersklasse/Dämmzustand (Überschlagswerte).</summary>
    public static readonly IReadOnlyList<HeizlastTabellenzeile> Standardtabelle =
    [
        new(1918, 170, 140, 90),
        new(1948, 160, 130, 85),
        new(1977, 150, 120, 80),
        new(1994, 110, 95, 70),
        new(2001, 85, 75, 60),
        new(2015, 60, 55, 45),
        new(int.MaxValue, 40, 40, 35),
    ];

    public HeizlastErgebnis Berechne(decimal wohnflaecheM2, int baujahr, Daemmzustand daemmzustand)
    {
        if (wohnflaecheM2 <= 0m)
            throw new ArgumentOutOfRangeException(nameof(wohnflaecheM2), "Wohnfläche muss positiv sein.");

        var zeile = tabelle.FirstOrDefault(z => baujahr <= z.BaujahrBisEinschliesslich)
            ?? throw new InvalidOperationException($"Heizlast-Tabelle deckt Baujahr {baujahr} nicht ab.");

        var wattProM2 = daemmzustand switch
        {
            Daemmzustand.Unsaniert => zeile.UnsaniertWattProM2,
            Daemmzustand.Teilsaniert => zeile.TeilsaniertWattProM2,
            Daemmzustand.Vollsaniert => zeile.VollsaniertWattProM2,
            _ => throw new ArgumentOutOfRangeException(nameof(daemmzustand)),
        };

        var kw = Math.Round(wohnflaecheM2 * wattProM2 / 1000m, 1, MidpointRounding.AwayFromZero);
        return new HeizlastErgebnis(kw, wattProM2, Geraeteklasse(kw));
    }

    /// <summary>Plausibilitäts-Hinweise (Plan §7), keine Blocker.</summary>
    public static IReadOnlyList<string> Pruefhinweise(Aufnahme aufnahme)
    {
        var hinweise = new List<string>();
        if (aufnahme is { Heizflaechen: Projekte.Heizflaechen.Heizkoerper or Projekte.Heizflaechen.Gemischt, VorlauftemperaturC: > 55 })
            hinweise.Add("Heizkörper mit Vorlauf über 55 °C: Heizflächen prüfen (ggf. tauschen oder Vorlauf absenken).");
        if (aufnahme.BerechneteHeizlastKw is > 20m)
            hinweise.Add("Heizlast über 20 kW: für ein EFH ungewöhnlich — Eingaben prüfen, ggf. Kaskade/Sonderfall.");
        return hinweise;
    }

    private static string Geraeteklasse(decimal kw)
    {
        var untere = Math.Floor(kw);
        return $"Geräteklasse ca. {untere:0}–{untere + 2:0} kW (Überschlag)";
    }
}

/// <param name="BaujahrBisEinschliesslich">Obergrenze der Baualtersklasse.</param>
public sealed record HeizlastTabellenzeile(
    int BaujahrBisEinschliesslich,
    decimal UnsaniertWattProM2,
    decimal TeilsaniertWattProM2,
    decimal VollsaniertWattProM2);

public sealed record HeizlastErgebnis(decimal HeizlastKw, decimal WattProM2, string Geraeteempfehlung);
