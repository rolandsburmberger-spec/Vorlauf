using Vorlauf.Domain.Projekte;

namespace Vorlauf.Web;

/// <summary>
/// Anzeige-Mapping für Projektstatus: lesbares Label plus CSS-Klasse
/// der Temperatur-Rampe (kühles Rücklauf-Blau → warmes Vorlauf-Rot).
/// </summary>
public static class StatusAnzeige
{
    public static string Label(ProjektStatus status) => status switch
    {
        ProjektStatus.FoerderungGeprueft => "Förderung geprüft",
        ProjektStatus.InMontage => "In Montage",
        _ => status.ToString(),
    };

    public static string Css(ProjektStatus status) => "st-" + status.ToString().ToLowerInvariant();
}
