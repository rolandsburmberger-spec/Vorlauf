using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Vorlauf.Web.Pages;

/// <summary>
/// Demo-Login (Cookie-Auth). Wird beim EF-/Identity-Umstieg durch
/// ASP.NET Core Identity ersetzt — die Rollen (Mitarbeiter, Admin)
/// bleiben identisch.
/// </summary>
[AllowAnonymous]
public class LoginModel : PageModel
{
    private static readonly Dictionary<string, (string Passwort, string Rolle)> DemoBenutzer = new(StringComparer.OrdinalIgnoreCase)
    {
        ["demo"] = ("demo", "Mitarbeiter"),
        ["admin"] = ("admin", "Admin"),
    };

    public string? Fehler { get; private set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(string? benutzer, string? passwort)
    {
        if (benutzer is null
            || !DemoBenutzer.TryGetValue(benutzer, out var eintrag)
            || !string.Equals(eintrag.Passwort, passwort, StringComparison.Ordinal))
        {
            Fehler = "Benutzer oder Passwort falsch.";
            return Page();
        }

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, benutzer.ToLowerInvariant()), new Claim(ClaimTypes.Role, eintrag.Rolle)],
            CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        return RedirectToPage("/Dashboard");
    }

    public async Task<IActionResult> OnPostAbmeldenAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToPage("/Index");
    }
}
