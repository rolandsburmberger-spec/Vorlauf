# WPFlow

**Prozessstrecke für Wärmepumpen-Projekte im SHK-Betrieb — vom Erstkontakt bis zur Schlussrechnung.**

Kein CRM, kein ERP. Eine Web-Anwendung für genau ein Geschäftsobjekt, dafür mit echter Fachlogik: Heizlast-Überschlag, KfW-458-Förderrechner mit versionierten Regelwerken, Statusautomat mit fachlichen Guards.

C# · ASP.NET Core Razor Pages (.NET 10) · EF Core · xUnit · GitHub Actions

<!-- SCREENSHOT: Pipeline-Dashboard mit Projektliste. Ersetzen durch:
     ![Pipeline-Dashboard](docs/screenshots/dashboard.png) -->

---

## Was die Anwendung kann

- **Förderrechner (KfW 458)** — versionierte Regelwerke, Bausteinlogik, Deckel und Kostengrenzen. Sollwerte verifiziert gegen das KfW-Merkblatt 458 (Stand 07/2026, Bestellnr. 600 000 5131).
- **„Was kostet Warten?“** — Stichtagsvergleich zweier Regelwerksstände, damit der Kunde den Effekt einer Degressionsstufe in Euro sieht.
- **Heizlast-Überschlag** — Kurzverfahren über Gebäudetyp und Fläche als Grundlage für die Anlagenauslegung.
- **Statusautomat mit Guards** — Statuswechsel laufen ausschließlich über eine explizite Übergangstabelle. Beispiel: keine Schlussrechnung ohne Fachunternehmererklärung. Ein blockierter Übergang liefert den Grund im Klartext.
- **Prozessmasken** — mobile Aufnahme vor Ort, Angebot mit „Preis nach Förderung“, Abnahme mit Unterschrift-Canvas, Schlussrechnung, Pipeline-Dashboard.

## Warum das technisch interessant ist

**Fördersätze stehen nie im Code.** Sie existieren ausschließlich als versionierte Regelwerks-Daten (`SeedRegelwerke`). Wenn der Gesetzgeber einen Bonus streicht, ist das ein Datenereignis — kein Deployment, keine Migration. Künftige, noch nicht beschlossene Degressionsstufen sind als `Verbindlich = false` markiert.

**Jede Berechnung ist ein unveränderlicher Snapshot** inklusive aller Eingangswerte und des verwendeten Regelwerks. Es wird nie zur Anzeigezeit neu gerechnet — sonst änderte sich ein zwei Monate altes Angebot rückwirkend, sobald die Förderung sinkt.

**Die Domain ist framework-frei.** `WPFlow.Domain` ist eine reine Klassenbibliothek ohne ASP.NET- oder EF-Abhängigkeit. Rechenkern und Zustandsautomat sind dadurch ohne Host, ohne Datenbank und ohne Mocking testbar — 32 xUnit-Tests decken sie ab.

**Der Demo-Seed läuft durch die echten Guards.** Bricht der Seed beim Start, ist die Fachlogik kaputt. Kein zweiter, gnädigerer Pfad zum Befüllen der Anwendung.

## Aufbau

| Projekt | Inhalt |
|---|---|
| `src/WPFlow.Domain` | Entitäten, Zustandsautomat, Förder-Rechenkern — ohne Framework-Abhängigkeiten |
| `src/WPFlow.Infrastructure` | EF Core (9.0.x), Identity-Stores, später XRechnung/PDF |
| `src/WPFlow.Web` | Razor Pages, Cookie-Auth, Prozessmasken |
| `tests/WPFlow.Tests` | xUnit — Rechenkern, Zustandsautomat, Guards, Heizlast, Angebot |

## Starten

```bash
dotnet restore
dotnet build
dotnet test                            # 32 Tests
dotnet run --project src/WPFlow.Web    # Login: demo/demo
```

Voraussetzung: .NET-10-SDK. Demo-Daten werden beim Start geseedet.
CI (Restore/Build/Test) läuft bei jedem Push über GitHub Actions.

## Stand und bewusste Grenzen

Kern fertig und klickbar: Rechenkern, Zustandsautomat, Prozessmasken, Dashboard.

Zwei Dinge sind absichtlich noch provisorisch, weil sie den fachlichen Kern nicht berühren: die **Ablage liegt in-memory** hinter dem Port `IProjektStore`, die **Authentifizierung ist ein Cookie-Demo-Login**. Der EF-/Identity-Umstieg ist vorbereitet (`WPFlow.Infrastructure`) und besteht aus einer ProjectReference plus dem Austausch der Store-Registrierung in `Program.cs`.

Offen: PDF-Ausgabe, XRechnung-Export inkl. KoSIT-Validierung, öffentliches Demo-Hosting.

**EF Core ist bewusst auf 9.0.x gepinnt**, weil Pomelo (MySQL-Provider) EF Core 10 noch nicht unterstützt (Stand 08/2026). Der 9er-Stack läuft unverändert unter `net10.0`; Bump, sobald Pomelo nachzieht.

---

Alle Förderausgaben tragen den Hinweis: *Unverbindliche Orientierung — maßgeblich sind die KfW-Bedingungen am Tag der Antragstellung.*
