namespace WPFlow.Domain.Foerderung;

/// <summary>Kein Regelwerk deckt den Stichtag ab — definierter Fehler, kein stilles Ergebnis.</summary>
public sealed class KeinRegelwerkException(DateOnly stichtag)
    : InvalidOperationException($"Kein Förderregelwerk gültig am {stichtag:dd.MM.yyyy}.")
{
    public DateOnly Stichtag { get; } = stichtag;
}
