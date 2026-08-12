using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Vorlauf.Web.Pages;

/// <summary>Öffentliche Projektseite (Landing): erklärt Vorlauf und führt zur Demo.</summary>
[AllowAnonymous]
public class IndexModel : PageModel
{
    public IActionResult OnGet()
        => User.Identity?.IsAuthenticated == true ? RedirectToPage("/Dashboard") : Page();
}
