using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WPFlow.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Dev: SQLite. Produktion/Pilot: MySQL 8 über Pomelo (siehe Infrastructure.csproj).
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? "Data Source=wpflow.dev.db";
builder.Services.AddDbContext<WpflowDbContext>(o => o.UseSqlite(connectionString));

builder.Services
    .AddDefaultIdentity<IdentityUser>(o => o.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<WpflowDbContext>();

builder.Services.AddRazorPages();

var app = builder.Build();

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
