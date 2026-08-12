using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Vorlauf.Domain.Foerderung;
using Vorlauf.Domain.Projekte;
using Vorlauf.Domain.Stammdaten;

namespace Vorlauf.Infrastructure.Dokumente;

/// <summary>
/// Angebots- und Rechnungs-PDF (QuestPDF, Community-Lizenz) im
/// Vorlauf-Datenblatt-Stil. Beträge immer mit deutscher Formatierung,
/// unabhängig von der Server-Culture.
/// </summary>
public sealed class PdfDokumente(Betrieb betrieb)
{
    static PdfDokumente() => QuestPDF.Settings.License = LicenseType.Community;

    private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");

    private const string Warm = "#cf4420";
    private const string Ink = "#1d232c";
    private const string Muted = "#6f7681";
    private const string Linie = "#e4dfd3";
    private const string WarmHell = "#f8ece5";

    private static string Euro(decimal x) => x.ToString("N2", De) + " €";

    // ------------------------------------------------------------------
    //  Angebot
    // ------------------------------------------------------------------

    public byte[] ErzeugeAngebotsPdf(Projekt projekt, Angebot angebot, Foerderberechnung? foerderung)
    {
        return Dokument("Angebot", angebot.Nummer, spalte =>
        {
            spalte.Item().Element(e => AnschriftUndMeta(e, projekt,
                ("Datum", angebot.Datum.ToString("dd.MM.yyyy")),
                ("Projekt", projekt.Bezeichnung),
                ("Referenz", XRechnungExport.ProjektReferenz(projekt))));

            spalte.Item().PaddingTop(18).Text($"Angebot {angebot.Nummer}").FontSize(14).Bold();
            spalte.Item().PaddingTop(2).Text("Lieferung und Montage einer Wärmepumpenanlage").FontColor(Muted);

            spalte.Item().PaddingTop(10).Element(e => Positionstabelle(e, angebot.Positionen
                .OrderBy(p => p.Position)
                .Select(p => (p.Position, p.Bezeichnung, p.Menge, p.Einheit, p.EinzelpreisNetto, p.GesamtNetto))));

            spalte.Item().AlignRight().PaddingTop(8).Element(e => Summenblock(e,
            [
                ("Summe netto", Euro(angebot.SummeNetto), false),
                ($"MwSt.", Euro(angebot.SummeMwSt), false),
                ("Gesamtbetrag brutto", Euro(angebot.SummeBrutto), true),
            ]));

            if (foerderung is not null)
            {
                spalte.Item().PaddingTop(14).Background(WarmHell).Padding(10).Column(f =>
                {
                    f.Item().Text("Ihre Förderung (KfW 458 — Heizungsförderung)").Bold().FontColor(Warm);
                    f.Item().PaddingTop(4).Text(
                        $"Voraussichtlicher Zuschuss (Fördersatz {foerderung.GedeckelterSatz.ToString("P0", De)}, " +
                        $"Stand {foerderung.Eingabe.Stichtag:dd.MM.yyyy}): −{Euro(foerderung.Zuschuss)}");
                    f.Item().PaddingTop(2).Text($"Ihr Preis nach Förderung: {Euro(angebot.PreisNachFoerderung(foerderung.Zuschuss))}")
                        .FontSize(11).Bold();
                    f.Item().PaddingTop(4).Text(
                        "Unverbindliche Orientierung — maßgeblich sind die KfW-Bedingungen am Tag der Antragstellung. " +
                        "Der Zuschuss wird nach Verwendungsnachweis direkt von der KfW an Sie ausgezahlt.")
                        .FontSize(7.5f).FontColor(Muted);
                });
            }

            if (angebot.FoerdervorbehaltsklauselEnthalten)
            {
                spalte.Item().PaddingTop(8).Text(
                    "Fördervorbehalt: Dieser Vertrag wird unter dem Vorbehalt geschlossen, dass die beantragte " +
                    "Förderung bewilligt wird.").FontSize(8.5f);
            }

            spalte.Item().PaddingTop(14).Text(
                "Dieses Angebot ist 4 Wochen gültig. Wir freuen uns auf Ihren Auftrag!").FontColor(Muted);
        });
    }

    // ------------------------------------------------------------------
    //  Rechnung
    // ------------------------------------------------------------------

    public byte[] ErzeugeRechnungsPdf(Projekt projekt, Rechnung rechnung)
    {
        var titel = rechnung.Typ switch
        {
            RechnungTyp.Abschlag => "Abschlagsrechnung",
            RechnungTyp.Teilrechnung => "Teilrechnung",
            _ => "Schlussrechnung",
        };
        var leistung = rechnung.Leistungsdatum is { } l
            ? l.ToString("dd.MM.yyyy")
            : "wird erbracht (Anzahlung)";

        return Dokument(titel, rechnung.Nummer, spalte =>
        {
            spalte.Item().Element(e => AnschriftUndMeta(e, projekt,
                ("Rechnungsdatum", rechnung.Datum.ToString("dd.MM.yyyy")),
                ("Leistungsdatum", leistung),
                ("Projekt", projekt.Bezeichnung),
                ("Referenz", XRechnungExport.ProjektReferenz(projekt))));

            spalte.Item().PaddingTop(18).Text($"{titel} {rechnung.Nummer}").FontSize(14).Bold();

            spalte.Item().PaddingTop(10).Element(e => Positionstabelle(e, rechnung.Positionen
                .OrderBy(p => p.Position)
                .Select(p => (p.Position, p.Bezeichnung, p.Menge, p.Einheit, p.EinzelpreisNetto, p.GesamtNetto))));

            var zeilen = new List<(string, string, bool)>
            {
                ("Summe netto", Euro(rechnung.SummeNetto), false),
                ("MwSt.", Euro(rechnung.SummeMwSt), false),
                ("Gesamtbetrag brutto", Euro(rechnung.SummeBrutto), true),
            };

            var abschlaege = rechnung.Typ == RechnungTyp.Schlussrechnung
                ? projekt.Rechnungen.Where(r => r.Id != rechnung.Id && r.Typ == RechnungTyp.Abschlag).ToList()
                : [];
            foreach (var a in abschlaege)
                zeilen.Add(($"abzüglich {a.Nummer} vom {a.Datum:dd.MM.yyyy}", "−" + Euro(a.SummeBrutto), false));
            if (abschlaege.Count > 0)
                zeilen.Add(("Verbleibender Restbetrag", Euro(Abschlagsverrechnung.Restbetrag(rechnung, projekt.Rechnungen)), true));

            spalte.Item().AlignRight().PaddingTop(8).Element(e => Summenblock(e, zeilen));

            spalte.Item().PaddingTop(14).Text(
                $"Zahlbar ohne Abzug bis {rechnung.Datum.AddDays(14):dd.MM.yyyy} auf " +
                $"IBAN {betrieb.Iban}{(betrieb.Bank is null ? "" : $" ({betrieb.Bank})")}.");

            spalte.Item().PaddingTop(10).Background(WarmHell).Padding(10).Text(
                "Hinweis zur KfW-Förderung: Der Zuschuss wird nicht von dieser Rechnung abgezogen. " +
                "Sie zahlen den Rechnungsbetrag an uns und erhalten den Zuschuss nach Einreichung des " +
                "Verwendungsnachweises direkt von der KfW ausgezahlt.")
                .FontSize(8.5f);
        });
    }

    // ------------------------------------------------------------------
    //  Bausteine
    // ------------------------------------------------------------------

    private byte[] Dokument(string dokumentTyp, string nummer, Action<ColumnDescriptor> inhalt)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9.5f).FontColor(Ink));

                page.Header().Column(kopf =>
                {
                    kopf.Item().Row(row =>
                    {
                        row.RelativeItem().Column(links =>
                        {
                            links.Item().Text(betrieb.Name).FontSize(13).Bold();
                            links.Item().Text($"{betrieb.Strasse} · {betrieb.Plz} {betrieb.Ort} · {betrieb.Telefon}")
                                .FontSize(7.5f).FontColor(Muted);
                        });
                        row.ConstantItem(150).AlignRight().Column(rechts =>
                        {
                            rechts.Item().AlignRight().Text(dokumentTyp.ToUpperInvariant()).FontSize(10).Bold().FontColor(Warm);
                            rechts.Item().AlignRight().Text(nummer).FontSize(9).FontColor(Muted);
                        });
                    });
                    kopf.Item().PaddingTop(6).BorderBottom(2).BorderColor(Warm);
                });

                page.Content().PaddingTop(16).Column(inhalt);

                page.Footer().Column(fuss =>
                {
                    fuss.Item().BorderBottom(1).BorderColor(Linie);
                    fuss.Item().PaddingTop(4).Row(row =>
                    {
                        row.RelativeItem().Text($"{betrieb.Name}{(betrieb.Inhaber is null ? "" : $" · Inhaber: {betrieb.Inhaber}")}")
                            .FontSize(7).FontColor(Muted);
                        row.RelativeItem().AlignCenter()
                            .Text($"Steuernummer {betrieb.Steuernummer}{(betrieb.UStIdNr is null ? "" : $" · USt-IdNr. {betrieb.UStIdNr}")}")
                            .FontSize(7).FontColor(Muted);
                        row.RelativeItem().AlignRight()
                            .Text($"IBAN {betrieb.Iban}{(betrieb.Bic is null ? "" : $" · BIC {betrieb.Bic}")}")
                            .FontSize(7).FontColor(Muted);
                    });
                });
            });
        }).GeneratePdf();
    }

    private static void AnschriftUndMeta(IContainer container, Projekt projekt, params (string K, string V)[] meta)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(anschrift =>
            {
                anschrift.Item().Text(projekt.Kunde?.Name ?? "—").Bold();
                if ((projekt.Kunde?.Strasse ?? projekt.Gebaeude?.Strasse) is { } strasse)
                    anschrift.Item().Text(strasse);
                if ((projekt.Kunde?.PlzOrt ?? projekt.Gebaeude?.PlzOrt) is { } plzOrt)
                    anschrift.Item().Text(plzOrt);
            });
            row.ConstantItem(190).Column(rechts =>
            {
                foreach (var (k, v) in meta)
                {
                    rechts.Item().Row(r =>
                    {
                        r.ConstantItem(85).Text(k).FontSize(8).FontColor(Muted);
                        r.RelativeItem().Text(v).FontSize(8.5f);
                    });
                }
            });
        });
    }

    private static void Positionstabelle(
        IContainer container,
        IEnumerable<(int Pos, string Bezeichnung, decimal Menge, string Einheit, decimal Ep, decimal Gesamt)> positionen)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.ConstantColumn(26);
                c.RelativeColumn();
                c.ConstantColumn(45);
                c.ConstantColumn(50);
                c.ConstantColumn(70);
                c.ConstantColumn(75);
            });

            table.Header(h =>
            {
                foreach (var (text, rechts) in new[] { ("Pos", false), ("Bezeichnung", false), ("Menge", true), ("Einheit", false), ("EP netto", true), ("Gesamt netto", true) })
                {
                    var zelle = h.Cell().BorderBottom(1.2f).BorderColor(Ink).PaddingBottom(3);
                    (rechts ? zelle.AlignRight() : zelle).Text(text).FontSize(7.5f).Bold().FontColor(Muted);
                }
            });

            foreach (var p in positionen)
            {
                table.Cell().Element(Zelle).Text(p.Pos.ToString());
                table.Cell().Element(Zelle).Text(p.Bezeichnung);
                table.Cell().Element(Zelle).AlignRight().Text(p.Menge.ToString("0.##", De));
                table.Cell().Element(Zelle).Text(p.Einheit);
                table.Cell().Element(Zelle).AlignRight().Text(Euro(p.Ep));
                table.Cell().Element(Zelle).AlignRight().Text(Euro(p.Gesamt));
            }

            static IContainer Zelle(IContainer c) =>
                c.BorderBottom(0.7f).BorderColor(Linie).PaddingVertical(3.5f);
        });
    }

    private static void Summenblock(IContainer container, IEnumerable<(string K, string V, bool Fett)> zeilen)
    {
        container.Column(summen =>
        {
            foreach (var (k, v, fett) in zeilen)
            {
                summen.Item().Row(r =>
                {
                    var key = r.ConstantItem(190).AlignRight().PaddingRight(12);
                    var val = r.ConstantItem(95).AlignRight();
                    if (fett)
                    {
                        key.Text(k).Bold();
                        val.BorderTop(1).BorderColor(Ink).Text(v).Bold();
                    }
                    else
                    {
                        key.Text(k);
                        val.Text(v);
                    }
                });
            }
        });
    }
}
