namespace Vorlauf.Domain.Foerderung;

/// <summary>
/// Unveränderlicher Snapshot einer Berechnung: Regelwerk, alle Eingangswerte,
/// je angewandtem Baustein eine Position mit eigener Bemessungsgrundlage,
/// gedeckelter Endsatz, Zuschuss. Wird nie zur Anzeigezeit neu gerechnet.
/// </summary>
public sealed class Foerderberechnung
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required Guid RegelwerkId { get; init; }
    public required string RegelwerkBezeichnung { get; init; }
    public required bool RegelwerkVerbindlich { get; init; }
    public required DateTime ErstelltUtc { get; init; }
    public required FoerderEingabe Eingabe { get; init; }

    /// <summary>Gedeckelte förderfähige Kosten nach WE-Staffel.</summary>
    public required decimal FoerderfaehigeKosten { get; init; }

    /// <summary>Endsatz nach Deckelung, decimal(5,4).</summary>
    public required decimal GedeckelterSatz { get; init; }

    public required decimal Zuschuss { get; init; }

    public List<FoerderberechnungPosition> Positionen { get; init; } = [];
}

/// <summary>
/// Eine Position je angewandtem Baustein. Trägt eine eigene
/// Bemessungsgrundlage: bei Mehrfamilienhäusern gelten Boni nur für die
/// selbstgenutzte Wohneinheit — ein globaler „Endsatz × Gesamtkosten"
/// reicht dann nicht.
/// </summary>
public sealed class FoerderberechnungPosition
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required BausteinArt Art { get; init; }
    public required decimal Satz { get; init; }
    public required decimal Bemessungsgrundlage { get; init; }
    public required decimal Betrag { get; init; }
    public string? Bemerkung { get; init; }
}
