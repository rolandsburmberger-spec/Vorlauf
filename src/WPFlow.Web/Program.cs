using Microsoft.AspNetCore.Authentication.Cookies;
using WPFlow.Domain.Ablage;
using WPFlow.Domain.Foerderung;
using WPFlow.Domain.Projekte;
using WPFlow.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<FoerderRechner>();
builder.Services.AddSingleton(HeizlastRechner.MitStandardtabelle());
builder.Services.AddSingleton(new Zustandsautomat(TimeProvider.System, StandardGuards.Alle()));
builder.Services.AddSingleton<IReadOnlyList<Foerderregelwerk>>(_ => SeedRegelwerke.Alle());
builder.Services.AddSingleton<IProjektStore, InMemoryProjektStore>();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/Login";
        o.AccessDeniedPath = "/Login";
    });
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());

builder.Services.AddRazorPages();

var app = builder.Build();

// Demo-Seed: läuft durch die echten Guards — bricht der Seed, ist die Fachlogik kaputt.
DemoSeed.Fuelle(
    app.Services.GetRequiredService<IProjektStore>(),
    app.Services.GetRequiredService<FoerderRechner>(),
    app.Services.GetRequiredService<Zustandsautomat>(),
    app.Services.GetRequiredService<IReadOnlyList<Foerderregelwerk>>());

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
