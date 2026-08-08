using WPFlow.Domain.Projekte;

namespace WPFlow.Domain.Ablage;

/// <summary>
/// Ablage-Port. Aktuelle Implementierung: In-Memory (Web, Demo-Betrieb).
/// EF-Core-Implementierung liegt in WPFlow.Infrastructure und wird
/// aktiviert, sobald NuGet-Restore verfügbar ist (eine DI-Zeile).
/// </summary>
public interface IProjektStore
{
    IReadOnlyList<Projekt> Alle();
    Projekt? Finde(Guid id);
    void Speichere(Projekt projekt);
}
