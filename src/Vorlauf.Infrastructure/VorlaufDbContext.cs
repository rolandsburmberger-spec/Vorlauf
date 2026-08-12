using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Vorlauf.Domain.Foerderung;
using Vorlauf.Domain.Projekte;

namespace Vorlauf.Infrastructure;

/// <summary>
/// EF-Core-Kontext inkl. ASP.NET Core Identity (2 Rollen: Mitarbeiter, Admin).
/// Beträge decimal(18,2), Sätze decimal(5,4), Guid-PKs, Zeitstempel UTC.
/// </summary>
public sealed class VorlaufDbContext(DbContextOptions<VorlaufDbContext> options)
    : IdentityDbContext<IdentityUser>(options)
{
    public DbSet<Projekt> Projekte => Set<Projekt>();
    public DbSet<ProjektStatusHistorie> ProjektStatusHistorien => Set<ProjektStatusHistorie>();
    public DbSet<Foerderregelwerk> Foerderregelwerke => Set<Foerderregelwerk>();
    public DbSet<Foerderberechnung> Foerderberechnungen => Set<Foerderberechnung>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Foerderregelwerk>(e =>
        {
            e.OwnsMany(r => r.Bausteine, b => b.Property(x => x.Satz).HasPrecision(5, 4));
            e.OwnsMany(r => r.KostenGrenzen, b => b.Property(x => x.MaxKostenJeWohneinheit).HasPrecision(18, 2));
            e.OwnsMany(r => r.Deckel, b =>
            {
                b.Property(x => x.MaxSatz).HasPrecision(5, 4);
                b.Property(x => x.ZvEGrenze).HasPrecision(18, 2);
            });
        });

        builder.Entity<Foerderberechnung>(e =>
        {
            e.Property(x => x.FoerderfaehigeKosten).HasPrecision(18, 2);
            e.Property(x => x.GedeckelterSatz).HasPrecision(5, 4);
            e.Property(x => x.Zuschuss).HasPrecision(18, 2);
            e.OwnsOne(x => x.Eingabe, b =>
            {
                b.Property(x => x.InvestitionskostenBrutto).HasPrecision(18, 2);
                b.Property(x => x.ZuVersteuerndesEinkommen).HasPrecision(18, 2);
            });
            e.OwnsMany(x => x.Positionen, b =>
            {
                b.Property(x => x.Satz).HasPrecision(5, 4);
                b.Property(x => x.Bemessungsgrundlage).HasPrecision(18, 2);
                b.Property(x => x.Betrag).HasPrecision(18, 2);
            });
        });
    }
}
