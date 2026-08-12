using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Vorlauf.Web.Pages;

[AllowAnonymous]
public class ImpressumModel : PageModel
{
    public void OnGet() { }
}
