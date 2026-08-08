namespace WPFlow.Domain.Foerderung;

/// <summary>
/// Förderbausteine als Zeilen, nicht als Spalten: der Wegfall eines Bonus
/// (z. B. Effizienzbonus zum 21.07.2026) ist ein Datenereignis, keine Migration.
/// </summary>
public enum BausteinArt
{
    Grundfoerderung,
    Klimageschwindigkeit,
    Effizienz,
    Einkommen,
    Emissionsminderung,
}
