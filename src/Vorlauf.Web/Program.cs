using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Vorlauf.Domain.Ablage;
using Vorlauf.Domain.Foerderung;
using Vorlauf.Domain.Projekte;
using Vorlauf.Domain.Stammdaten;
using Vorlauf.Infrastructure.Dokumente;
using Vorlauf.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Hosting-Plattformen (Render u. a.) geben den Port über die Umgebungs-
// variable PORT vor. Lokal bleibt es bei den launchSettings.
if (Environment.GetEnvironmentVariable("PORT") is { Length: > 0 } port)
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Hinter dem Reverse Proxy der Hosting-Plattform: Ohne die weitergereichten
// Header sieht die App nur HTTP — HTTPS-Redirect und Cookie-Sicherheit
// verhielten sich dann falsch.
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Der Proxy hat keine feste, vorab bekannte IP. Das Leeren der Listen
    // ist hier vertretbar, weil die Instanz ausschließlich über diesen
    // Proxy erreichbar ist und nicht direkt aus dem Internet.
    o.KnownNetworks.Clear();
    o.KnownProxies.Clear();
});

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
    // Fiktiv, aber prüfziffernkorrekt. In der XRechnung Pflicht: EN 16931
    // BR-CO-26 verlangt eine Verkäufer-Kennung (BT-29/30/31); die
    // Steuernummer allein (BT-32) genügt der Regel nicht.
    UStIdNr = "DE136589744",
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

// Muss vor allem anderen laufen, damit nachgelagerte Middleware das
// ursprüngliche Schema (https) sieht.
app.UseForwardedHeaders();

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
