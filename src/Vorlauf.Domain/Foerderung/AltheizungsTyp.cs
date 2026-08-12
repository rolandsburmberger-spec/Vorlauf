namespace Vorlauf.Domain.Foerderung;

/// <summary>
/// Typ der Bestandsheizung. Relevant für den Klimageschwindigkeitsbonus
/// (KfW-Merkblatt 458, Stand 07/2026): Öl, Kohle, Gasetagen- und
/// Nachtspeicherheizung qualifizieren altersunabhängig; Gasheizung und
/// Biomasseheizung erst ab 20 Jahren seit Inbetriebnahme.
/// </summary>
public enum AltheizungsTyp
{
    Oelheizung,
    Kohleheizung,
    Gasetagenheizung,
    Nachtspeicherheizung,
    Gasheizung,
    Biomasseheizung,
    Sonstige,
}
