namespace Vorlauf.Domain.Projekte;

/// <summary>
/// Guard-Vorbedingung eines Statusübergangs (Plan §4). Konkrete Guards
/// (Aufnahme vollständig, Förderberechnung vorhanden, Fachunternehmererklärung
/// ausgestellt, …) entstehen mit ihren Entitäten in M2/M3.
/// </summary>
public interface IUebergangsGuard
{
    GuardErgebnis Pruefe(Projekt projekt, ProjektStatus nach);
}

public sealed record GuardErgebnis(bool Erlaubt, string? Grund)
{
    public static GuardErgebnis Ok() => new(true, null);
    public static GuardErgebnis Blockiert(string grund) => new(false, grund);
}

public sealed class UngueltigerUebergangException(ProjektStatus von, ProjektStatus nach)
    : InvalidOperationException($"Übergang {von} → {nach} ist nicht definiert.")
{
    public ProjektStatus Von { get; } = von;
    public ProjektStatus Nach { get; } = nach;
}

public sealed class UebergangBlockiertException(ProjektStatus von, ProjektStatus nach, string grund)
    : InvalidOperationException($"Übergang {von} → {nach} blockiert: {grund}")
{
    public ProjektStatus Von { get; } = von;
    public ProjektStatus Nach { get; } = nach;
    public string Grund { get; } = grund;
}
