using WPFlow.Domain.Ablage;
using WPFlow.Domain.Projekte;

namespace WPFlow.Web.Services;

/// <summary>
/// In-Memory-Ablage für Demo-Betrieb und Entwicklung ohne Datenbank.
/// Produktion: EF-Core-Implementierung in WPFlow.Infrastructure.
/// </summary>
public sealed class InMemoryProjektStore : IProjektStore
{
    private readonly Dictionary<Guid, Projekt> _projekte = [];
    private readonly Lock _lock = new();

    public IReadOnlyList<Projekt> Alle()
    {
        lock (_lock)
        {
            return _projekte.Values.OrderByDescending(p => p.AngelegtUtc).ToList();
        }
    }

    public Projekt? Finde(Guid id)
    {
        lock (_lock)
        {
            return _projekte.GetValueOrDefault(id);
        }
    }

    public void Speichere(Projekt projekt)
    {
        lock (_lock)
        {
            _projekte[projekt.Id] = projekt;
        }
    }
}
