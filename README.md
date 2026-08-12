# Vorlauf

**Prozessstrecke für Wärmepumpen-Projekte im SHK-Betrieb — vom Erstkontakt bis zur Schlussrechnung.**

Kein CRM, kein ERP. Eine Web-Anwendung für genau ein Geschäftsobjekt, dafür mit echter Fachlogik: Heizlast-Überschlag, KfW-458-Förderrechner mit versionierten Regelwerken, Statusautomat mit fachlichen Guards, XRechnung-Export.

C# · ASP.NET Core Razor Pages (.NET 10) · EF Core · QuestPDF · ZUGFeRD-csharp · xUnit · GitHub Actions

> *Vorlauf* (der): 1. Heizungstechnik — die warme Leitung vom Wärmeerzeuger zu den Heizflächen. 2. übertragen — der Vorsprung, den ein Betrieb hat, der seine Projekte im Griff hat. (Das Projekt hieß bis 08/2026 WPFlow.)

<!-- SCREENSHOT: Pipeline-Dashboard mit Projektliste. Ersetzen durch:
     ![Pipeline-Dashboard](docs/screenshots/dashboard.png) -->

---

## Was die Anwendung kann

- **Förderrechner (KfW 458)** — versionierte Regelwerke, Bausteinlogik, Deckel und Kostengrenzen. Sollwerte verifiziert gegen das KfW-Merkblatt 458 (Stand 07/2026, Bestellnr. 600 000 5131).
- **„Was kostet Warten?“** — Stichtagsvergleich zweier Regelwerksstände, damit der Kunde den Effekt einer Degressionsstufe in Euro sieht.
- **Heizlast-Überschlag** — Kurzverfahren über Gebäudetyp und Fläche als Grundlage für die Anlagenauslegung.
- **Statusautomat mit Guards** — Statuswechsel laufen ausschließlich über eine explizite Übergangstabelle. Beispiel: keine Schlussrechnung ohne Fachunternehmererklärung. Ein blockierter Übergang liefert den Grund im Klartext.
- **Prozessmasken** — mobile Aufnahme vor Ort, Angebot mit „Preis nach Förderung“, Abnahme mit Unterschrift-Canvas, Pipeline-Dashboard.
- **Rechnungen mit Export** — Abschlags- und Schlussrechnung, letztere mit automatischer Abschlagsverrechnung (§ 14 Abs. 5 UStG). Ausgabe als PDF und als **XRechnung** (EN 16931, CII-Syntax).
- **Prozess-Tour** — eine geführte Tour legt ein eigenes Demo-Projekt an und spielt die komplette Kette von der Anfrage bis zur Schlussrechnung durch, inklusive eines absichtlich blockierten Guards.

## Warum das technisch interessant ist

**Fördersätze stehen nie im Code.** Sie existieren ausschließlich als versionierte Regelwerks-Daten (`SeedRegelwerke`). Wenn der Gesetzgeber einen Bonus streicht, ist das ein Datenereignis — kein Deployment, keine Migration. Künftige, noch nicht beschlossene Degressionsstufen sind als `Verbindlich = false` markiert.

**Jede Berechnung ist ein unveränderlicher Snapshot** inklusive aller Eingangswerte und des verwendeten Regelwerks. Es wird nie zur Anzeigezeit neu gerechnet — sonst änderte sich ein zwei Monate altes Angebot rückwirkend, sobald die Förderung sinkt.

**Die Domain ist framework-frei.** `Vorlauf.Domain` ist eine reine Klassenbibliothek ohne ASP.NET- oder EF-Abhängigkeit. Rechenkern und Zustandsautomat sind dadurch ohne Host, ohne Datenbank und ohne Mocking testbar — 36 xUnit-Tests decken sie ab.

**Der Demo-Seed läuft durch die echten Guards.** Bricht der Seed beim Start, ist die Fachlogik kaputt. Kein zweiter, gnädigerer Pfad zum Befüllen der Anwendung.

**Die XRechnung wird gegen die offiziellen Prüfregeln validiert.** Ein selbstgebautes XML, das nur „gut aussieht“, ist wertlos: Die CI erzeugt bei jedem Push eine Beispielrechnung und prüft sie mit dem **KoSIT-Validator** gegen die Schematron-Regeln der XRechnung 3.0.2.

**Der KfW-Zuschuss wird nicht von der Rechnung abgezogen.** Der Kunde zahlt den vollen Betrag und erhält den Zuschuss nach Verwendungsnachweis direkt von der KfW. „Preis nach Förderung“ ist deshalb ein Angebots-Argument, keine Rechnungsposition — die Anwendung sagt das an jeder Stelle, an der die Verwechslung naheliegt.

## Aufbau

| Projekt | Inhalt |
|---|---|
| `src/Vorlauf.Domain` | Entitäten, Zustandsautomat, Förder-Rechenkern — ohne Framework-Abhängigkeiten |
| `src/Vorlauf.Infrastructure` | EF Core (9.0.x), PDF (QuestPDF), XRechnung (ZUGFeRD-csharp) |
| `src/Vorlauf.Web` | Razor Pages, Cookie-Auth, Prozessmasken, Landing-Seite, Prozess-Tour |
| `tests/Vorlauf.Tests` | xUnit — Rechenkern, Zustandsautomat, Guards, Heizlast, Angebot, Dokument-Exporte |
| `tools/Vorlauf.Beispiele` | erzeugt die Beispiel-XRechnung für die KoSIT-Validierung in der CI |

Zum Oberflächen-Konzept: In der Heizungstechnik ist der Vorlauf rot und der Rücklauf blau markiert. Daraus wird die Statusfarbe — Projekte „erwärmen“ sich entlang der Prozessstrecke von der kühlen Anfrage bis zum warmen Abschluss.

## Starten

```bash
dotnet restore
dotnet build
dotnet test                              # 36 Tests
dotnet run --project src/Vorlauf.Web     # http://localhost:5188 — Login: demo/demo
```

Voraussetzung: .NET-10-SDK. Demo-Daten werden beim Start geseedet.
CI (Restore/Build/Test sowie KoSIT-Validierung der XRechnung) läuft bei jedem Push über GitHub Actions.

## Deployment

Das Repository enthält ein `Dockerfile` und einen Render-Blueprint (`render.yaml`)
für den kostenlosen Demo-Betrieb. Auf render.com genügt „New" → „Blueprint" mit
diesem Repository; alles Weitere steht in der `render.yaml`.

Zwei Eigenheiten, die den Betrieb prägen:

- **Kein Datenbankdienst nötig.** Die Ablage läuft in-memory hinter dem Port
  `IProjektStore`. Das flüchtige Dateisystem ist damit kein Nachteil, sondern
  liefert den gewünschten Demo-Reset gratis: Jeder Neustart stellt den
  Seed-Stand wieder her.
- **Der Free-Tier fährt nach 15 Minuten ohne Anfrage herunter.** Der nächste
  Aufruf startet die Instanz wieder — das dauert rund eine Minute. Die
  Startseite weist darauf hin.

`libfontconfig1` wird im Laufzeit-Image nachinstalliert: QuestPDF rendert über
SkiaSharp und scheitert ohne dieses Paket zur Laufzeit. Die App liest den Port
aus der Umgebungsvariablen `PORT` und wertet `X-Forwarded-Proto` aus, damit
HTTPS-Redirect und Cookies hinter dem Reverse Proxy korrekt arbeiten.

Lokal mit Docker:

```bash
docker build -t vorlauf . && docker run --rm -p 8080:10000 vorlauf
```

## Stand und bewusste Grenzen

Kern fertig und klickbar: Rechenkern, Zustandsautomat, Prozessmasken, Dashboard, Rechnungswesen mit PDF- und XRechnung-Export.

Drei Dinge sind absichtlich noch provisorisch, weil sie den fachlichen Kern nicht berühren: die **Ablage liegt in-memory** hinter dem Port `IProjektStore`, die **Authentifizierung ist ein Cookie-Demo-Login**, die **Betriebs-Stammdaten** (Pflichtangaben nach § 14 UStG) stehen als Seed im Code. Der EF-/Identity-Umstieg ist vorbereitet (`Vorlauf.Infrastructure`) und besteht aus einer ProjectReference plus dem Austausch der Store-Registrierung in `Program.cs`.

Offen: die öffentliche Demo-URL (Deployment ist vorbereitet, siehe oben) sowie
lokal ausgelieferte Schriften statt Google Fonts.

**EF Core ist bewusst auf 9.0.x gepinnt**, weil Pomelo (MySQL-Provider) EF Core 10 noch nicht unterstützt (Stand 08/2026). Der 9er-Stack läuft unverändert unter `net10.0`; Bump, sobald Pomelo nachzieht. `SQLitePCLRaw` ist auf 2.1.12 gepinnt (Sicherheitslücke GHSA-2m69-gcr7-jv3q in der transitiven 2.1.10).

Bewusst nicht im Scope: DATANORM, IDS-Connect, GAEB, DATEV, Lohn- und Zeiterfassung, Lager, automatisierte KfW-Antragstellung, Multi-Gewerk. Jedes Feature muss die Frage beantworten: Hilft das der WP-Prozessstrecke?

---

Alle Förderausgaben tragen den Hinweis: *Unverbindliche Orientierung — maßgeblich sind die KfW-Bedingungen am Tag der Antragstellung.*
