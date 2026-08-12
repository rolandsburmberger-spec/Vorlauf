using Microsoft.AspNetCore.Authentication.Cookies;
using Vorlauf.Domain.Ablage;
using Vorlauf.Domain.Foerderung;
using Vorlauf.Domain.Projekte;
using Vorlauf.Domain.Stammdaten;
using Vorlauf.Infrastructure.Dokumente;
using Vorlauf.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<FoerderRechner>();
builder.Services.AddSingleton(HeizlastRechner.MitStandardtabelle());
builder.Services.AddSingleton(new Zustandsautomat(TimeProvider.System, StandardGuards.Alle()));
builder.Services.AddSingleton<IReadOnlyList<Foerderregelwerk>>(_ => SeedRegelwerke.Alle());
builder.Services.AddSingleton<IProjektStore, InMemoryProjektStore>();

// Betriebs-Stammdaten (fiktiv): Pflichtangaben nach § 14 UStG für PDF und
// XRechnung. Stufe 2: pro Mandant konfigurierbar statt Seed.
builder.Services.AddSingleton(new Betrieb
{
    Name = "SHK Musterhaus GmbH",
    Inhaber = "Max Musterhaus",
    Strasse = "Handwerkerring 12",
    Plz = "36037",
    Ort = "Fulda",
    Telefon = "+49 661 1234567",
    Email = "info@shk-musterhaus.example",
    Steuernummer = "018 838 08150",
    Iban = "DE89 3704 0044 0532 0130 00",
    Bic = "COBADEFFXXX",
    Bank = "Commerzbank Fulda",
});
builder.Services.AddSingleton<PdfDokumente>();
builder.Services.AddSingleton<XRechnungExport>();

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

// Statische Dateien sind öffentlich — die Fallback-Policy würde sie sonst
// für anonyme Besucher (Landing, Login) auf /Login umleiten.
app.MapStaticAssets().AllowAnonymous();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
