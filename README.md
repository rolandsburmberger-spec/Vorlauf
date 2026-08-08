# WPFlow — Prozessstrecke für Wärmepumpen-Projekte im SHK-Betrieb

Schlanke Web-Anwendung für **ein** Geschäftsobjekt: das Wärmepumpen-Projekt vom
Erstkontakt bis zur Schlussrechnung. Kein CRM, kein ERP — eine Prozessstrecke
mit Fachlogik: Heizlast-Überschlag, KfW-458-Förderrechner mit versionierten
Regelwerken, XRechnung-Export.

**Status: M0 (Fundament).** Die 10 Rechenkern-Tests in
`tests/WPFlow.Tests/Foerderung/FoerderRechnerTests.cs` sind bewusst rot —
sie sind die verbindliche Sollliste für M1. Sollwerte verifiziert gegen das
KfW-Merkblatt 458 (Stand 07/2026, Bestellnr. 600 000 5131).

## Struktur

| Projekt | Inhalt |
|---|---|
| `src/WPFlow.Domain` | Entitäten, Zustandsautomat, Förder-Rechenkern — ohne Framework-Abhängigkeiten, als reine Klassenbibliothek testbar |
| `src/WPFlow.Infrastructure` | EF Core (9.0.x, gepinnt — s. u.), Identity-Stores, später XRechnung/PDF |
| `src/WPFlow.Web` | Razor Pages, ASP.NET Core Identity (Rollen: Mitarbeiter, Admin) |
| `tests/WPFlow.Tests` | xUnit; Rechenkern und Zustandsautomat werden vollständig abgedeckt |

## Bauen

```bash
dotnet restore
dotnet build
dotnet test   # M0: 10 rote Tests = M1-Sollliste
```

.NET-10-SDK erforderlich (LTS). Dev-Datenbank: SQLite (automatisch);
Produktion/Pilot: MySQL 8 über Pomelo.

**Warum EF Core 9.0.x statt 10?** Pomelo (MySQL-Provider) unterstützt EF Core 10
noch nicht (Stand 08/2026). Der 9er-Stack läuft unverändert unter `net10.0`;
Bump, sobald Pomelo nachzieht.

## Fachliche Grundsätze

- Fördersätze existieren nur als versionierte Regelwerks-Daten (Seed:
  `SeedRegelwerke`), nie im Code. Wegfall/Änderung eines Bonus ist ein
  Datenereignis, keine Migration.
- Jede Berechnung erzeugt einen unveränderlichen Snapshot inklusive aller
  Eingangswerte — es wird nie zur Anzeigezeit neu gerechnet.
- Künftige Degressionsstufen sind als `Verbindlich = false` markiert; die
  angekündigte Q1/2027-Reform (WP-Grundförderung 15 % + 15 %
  EU-Wertschöpfungsbonus, neue Austauschlogik) kann sie noch ändern.
- Statuswechsel laufen ausschließlich über die explizite Übergangstabelle
  (`Zustandsautomat`) mit Guards — z. B. keine Schlussrechnung ohne
  Fachunternehmererklärung.

Alle Förderausgaben tragen den Hinweis: *Unverbindliche Orientierung —
maßgeblich sind die KfW-Bedingungen am Tag der Antragstellung.*
