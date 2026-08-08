namespace WPFlow.Domain.Foerderung;

/// <summary>
/// Rechenkern (M1). Ablauf laut Plan §6:
/// 1. Regelwerk zum Stichtag wählen (kein Treffer → <see cref="KeinRegelwerkException"/>),
/// 2. förderfähige Kosten über WE-Staffel deckeln,
/// 3. Bausteine mit Bedingungen auswerten (je Baustein eigene Bemessungsgrundlage),
/// 4. Gesamtsatz deckeln (70 % / 80 %-Regel),
/// 5. Snapshot erzeugen.
/// </summary>
public sealed class FoerderRechner(TimeProvider zeit)
{
    private readonly TimeProvider _zeit = zeit;

    public Foerderberechnung Berechne(FoerderEingabe eingabe, IReadOnlyCollection<Foerderregelwerk> regelwerke)
    {
        // M1: Implementierung gegen die Testfälle in WPFlow.Tests.
        // Kein Baustein-Satz wird hier hartkodiert — alles kommt aus dem Regelwerk.
        throw new NotImplementedException("M1: Förder-Rechenkern ist noch nicht implementiert.");
    }
}
