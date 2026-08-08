using WPFlow.Domain.Projekte;

namespace WPFlow.Tests.Projekte;

public class ZustandsautomatTests
{
    private static Zustandsautomat Automat(IReadOnlyCollection<IUebergangsGuard>? guards = null) =>
        new(TimeProvider.System, guards);

    private static Projekt Neu() => new() { Bezeichnung = "WP Musterweg 1" };

    [Fact]
    public void Z01_GueltigerUebergang_SetztStatusUndSchreibtHistorie()
    {
        var p = Neu();

        Automat().Wechsle(p, ProjektStatus.Aufgenommen, "meister", "Aufnahme vor Ort");

        Assert.Equal(ProjektStatus.Aufgenommen, p.Status);
        Assert.Single(p.Historie);
        Assert.Equal(ProjektStatus.Anfrage, p.Historie[0].Von);
        Assert.Equal(ProjektStatus.Aufgenommen, p.Historie[0].Nach);
        Assert.Equal("meister", p.Historie[0].Benutzer);
    }

    [Fact]
    public void Z02_KompletteKetteBisAbgeschlossen()
    {
        var p = Neu();
        var a = Automat();
        ProjektStatus[] kette =
        [
            ProjektStatus.Aufgenommen, ProjektStatus.FoerderungGeprueft,
            ProjektStatus.Angeboten, ProjektStatus.Beauftragt,
            ProjektStatus.Terminiert, ProjektStatus.InMontage,
            ProjektStatus.Abgenommen, ProjektStatus.Berechnet,
            ProjektStatus.Abgeschlossen,
        ];

        foreach (var ziel in kette)
            a.Wechsle(p, ziel, "buero");

        Assert.Equal(ProjektStatus.Abgeschlossen, p.Status);
        Assert.Equal(9, p.Historie.Count);
    }

    [Fact]
    public void Z03_NichtDefinierterUebergang_WirftUndAendertNichts()
    {
        var p = Neu();

        Assert.Throws<UngueltigerUebergangException>(
            () => Automat().Wechsle(p, ProjektStatus.InMontage, "buero"));

        Assert.Equal(ProjektStatus.Anfrage, p.Status);
        Assert.Empty(p.Historie);
    }

    [Fact]
    public void Z04_GuardBlockiert_WirftMitGrundUndAendertNichts()
    {
        var p = Neu();
        var automat = Automat([new BlockierGuard("Heizlast fehlt")]);

        var ex = Assert.Throws<UebergangBlockiertException>(
            () => automat.Wechsle(p, ProjektStatus.Aufgenommen, "buero"));

        Assert.Equal("Heizlast fehlt", ex.Grund);
        Assert.Equal(ProjektStatus.Anfrage, p.Status);
        Assert.Empty(p.Historie);
    }

    [Fact]
    public void Z05_VerlorenNurVorBeauftragt()
    {
        Assert.True(Zustandsautomat.IstUebergangDefiniert(ProjektStatus.Angeboten, ProjektStatus.Verloren));
        Assert.False(Zustandsautomat.IstUebergangDefiniert(ProjektStatus.Beauftragt, ProjektStatus.Verloren));
        Assert.False(Zustandsautomat.IstUebergangDefiniert(ProjektStatus.InMontage, ProjektStatus.Verloren));
    }

    [Fact]
    public void Z06_AbgeschlossenUndVerlorenSindEndzustaende()
    {
        Assert.Empty(Zustandsautomat.MoeglicheZiele(ProjektStatus.Abgeschlossen));
        Assert.Empty(Zustandsautomat.MoeglicheZiele(ProjektStatus.Verloren));
    }

    private sealed class BlockierGuard(string grund) : IUebergangsGuard
    {
        public GuardErgebnis Pruefe(Projekt projekt, ProjektStatus nach) =>
            GuardErgebnis.Blockiert(grund);
    }
}
