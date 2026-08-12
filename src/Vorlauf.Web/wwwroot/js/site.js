// Vorlauf — Prozess-Tour (Vanilla JS, ohne Bibliothek).
//
// Kein Vorzeige-Rundgang, sondern ein echtes Durchspiel: Die Tour legt ein
// eigenes Demo-Projekt an und führt es einmal komplett durch die
// Prozessstrecke — mit echten Formular-Submits und echten Guards.
// Der aktuelle Schritt liegt in sessionStorage; nach jeder Navigation
// zeigt die neue Seite "ihren" Schritt an.
(function () {
  'use strict';

  var KEY = 'vl-tour';          // Index des aktuellen Schritts
  var KEY_PENDING = 'vl-tour-pending';
  var BANNER_WEG = 'vl-tour-banner-weg';

  // ---------- kleine Helfer ----------

  function $(sel) { return document.querySelector(sel); }
  function pfad() { return location.pathname; }
  function heuteIso() { return new Date().toISOString().slice(0, 10); }

  function setzeWert(sel, wert) {
    var el = $(sel);
    if (el) { el.value = wert; }
  }
  function setzeHaken(sel, an) {
    var el = $(sel);
    if (el) { el.checked = an; }
  }
  function sende(sel) {
    var el = $(sel);
    if (el) { el.requestSubmit ? el.requestSubmit() : el.submit(); return true; }
    return false;
  }
  function klicke(sel) {
    var el = $(sel);
    if (el) { el.click(); return true; }
    return false;
  }

  // Zeichnet eine kleine "Unterschrift" auf den Abnahme-Canvas und legt
  // sie in das Hidden-Feld (der Seiten-eigene Submit-Handler überschreibt
  // nur, wenn der Nutzer selbst gezeichnet hat).
  function unterschreibe() {
    var canvas = $('#unterschrift');
    var feld = $('#unterschriftDataUrl');
    if (!canvas || !feld) { return; }
    var ctx = canvas.getContext('2d');
    ctx.lineWidth = 2;
    ctx.lineCap = 'round';
    ctx.beginPath();
    ctx.moveTo(60, 100);
    ctx.bezierCurveTo(110, 30, 150, 130, 200, 70);
    ctx.bezierCurveTo(240, 25, 270, 110, 330, 80);
    ctx.bezierCurveTo(360, 65, 390, 90, 430, 75);
    ctx.stroke();
    feld.value = canvas.toDataURL('image/png');
  }

  // ---------- die Prozess-Tour ----------
  //
  // Jeder Schritt: auf welcher Seite er lebt (page), was er markiert
  // (target), was er erzählt (title/text) — und was der Weiter-Button
  // TUT (weiter): echte Klicks und Submits, keine Simulation.
  // zeigen() füllt Formulare sichtbar aus, bevor der Nutzer bestätigt.

  var STEPS = [
    {
      page: function (p) { return p === '/Dashboard'; },
      target: '[data-tour="pipeline"]',
      title: 'Die Prozessstrecke',
      text: 'Jedes Projekt hat genau einen Status — von kühl (Anfrage) bis warm (Abschluss). Wir gehen die Strecke jetzt einmal komplett durch: mit einem eigenen Projekt, echten Guards und dem Förderrechner.',
      weiter: function () { location.href = '/Projekte/Neu'; }
    },
    {
      page: function (p) { return p === '/Projekte/Neu'; },
      target: '[data-tour="neu-form"]',
      title: 'Schritt 1: Anfrage anlegen',
      zeigen: function () {
        var uhr = new Date();
        setzeWert('input[name="bezeichnung"]', 'Tour-Projekt Musterweg 1 (' + uhr.getHours() + ':' + String(uhr.getMinutes()).padStart(2, '0') + ')');
        setzeWert('input[name="kundeName"]', 'Familie Muster');
        setzeHaken('#sn', true);
        setzeWert('input[name="baujahr"]', 1994);
        setzeWert('input[name="wohnflaeche"]', 160);
        setzeWert('input[name="wohneinheiten"]', 1);
      },
      text: 'Ich habe das Formular ausgefüllt: Einfamilienhaus von 1994, 160 m², Selbstnutzer — wichtig für die Boni. „Weiter" legt das Projekt an.',
      weiter: function () { sende('[data-tour="neu-form"] form'); }
    },
    {
      page: function (p) { return p.indexOf('/Projekte/Detail') === 0; },
      target: '[data-tour="statuswechsel"]',
      title: 'Status: Anfrage — und jetzt?',
      text: 'Versuchen wir sofort „→ Aufgenommen". Das wird schiefgehen, denn die Aufnahme fehlt noch — genau dafür gibt es Guards. „Weiter" klickt den Übergang an.',
      weiter: function () { klicke('[data-tour="statuswechsel"] button[value="Aufgenommen"]'); }
    },
    {
      page: function (p) { return p.indexOf('/Projekte/Detail') === 0; },
      target: '.alert-danger',
      title: 'Der Guard blockiert — mit Grund',
      text: 'Keine Notiz, kein Merkzettel: Der Zustandsautomat verweigert den Übergang und nennt die fehlende Vorbedingung. Also erledigen wir sie — auf zur Aufnahme.',
      weiter: function () { klicke('.vl-actions a[href*="/Projekte/Aufnahme/"]'); }
    },
    {
      page: function (p) { return p.indexOf('/Projekte/Aufnahme') === 0; },
      target: '[data-tour="aufnahme-form"]',
      title: 'Schritt 2: Aufnahme vor Ort',
      zeigen: function () {
        setzeWert('select[name="altheizungTyp"]', 'Oelheizung');
        setzeWert('input[name="altheizungBaujahr"]', 1998);
        setzeHaken('#ft', true);
        setzeWert('select[name="heizflaechen"]', 'Heizkoerper');
        setzeWert('input[name="vorlauf"]', 55);
        setzeWert('select[name="daemmzustand"]', 'Teilsaniert');
        setzeWert('input[name="bemerkung"]', 'Aufnahme im Rahmen der Tour');
      },
      text: 'Mobil ausfüllbar, wie vom Monteur vor Ort: Ölheizung von 1998, funktionstüchtig (Klimabonus-Voraussetzung!), Heizkörper mit 55 °C Vorlauf, teilsaniert. „Weiter" speichert und berechnet die Heizlast.',
      weiter: function () { sende('[data-tour="aufnahme-form"] form'); }
    },
    {
      page: function (p) { return p.indexOf('/Projekte/Aufnahme') === 0; },
      target: '[data-tour="heizlast"]',
      title: 'Heizlast-Überschlag',
      text: 'Wohnfläche × Baualtersklasse ergibt Heizlast und Geräteklassen-Empfehlung. Ein transparenter Überschlag fürs Kundengespräch — er ersetzt keine DIN EN 12831, und das sagt er auch. Zurück zum Projekt.',
      weiter: function () { klicke('a[href*="/Projekte/Detail/"]'); }
    },
    {
      page: function (p) { return p.indexOf('/Projekte/Detail') === 0; },
      target: '[data-tour="statuswechsel"]',
      title: 'Jetzt lässt der Guard durch',
      text: 'Aufnahme vollständig, Heizlast berechnet — „→ Aufgenommen" funktioniert jetzt. „Weiter" klickt.',
      weiter: function () { klicke('[data-tour="statuswechsel"] button[value="Aufgenommen"]'); }
    },
    {
      page: function (p) { return p.indexOf('/Projekte/Detail') === 0; },
      target: '.alert-success',
      title: 'Statuswechsel protokolliert',
      text: 'Der Wechsel steht in der Historie: wer, wann, von wo nach wo. Nächste Station: Förderung. Auch hier wartet ein Guard — ohne gespeicherte Berechnung kein „Förderung geprüft". Auf zum Herzstück.',
      weiter: function () { klicke('.vl-actions a[data-tour-foerdercheck]'); }
    },
    {
      page: function (p) { return p.indexOf('/Projekte/Foerdercheck') === 0; },
      target: '[data-tour="fc-form"]',
      title: 'Schritt 3: Förder-Check (KfW 458)',
      zeigen: function () {
        setzeWert('input[name="kosten"]', 32000);
        setzeWert('input[name="zvE"]', 42000);
        setzeWert('input[name="vergleichsStichtag"]', '2027-02-15');
      },
      text: '32.000 € Kosten, 42.000 € zvE — Selbstnutzung und die Öl-Altheizung kennt das Projekt schon. Als Vergleichs-Stichtag: Februar 2027, wenn die nächste Degressionsstufe gilt. „Weiter" berechnet.',
      weiter: function () { sende('[data-tour="fc-form"] form'); }
    },
    {
      page: function (p) { return p.indexOf('/Projekte/Foerdercheck') === 0; },
      target: '[data-tour="fc-ergebnis"]',
      title: '„Was kostet Warten?"',
      text: 'Links der Zuschuss nach aktuellem Regelwerk — Grundförderung plus Klimageschwindigkeits-Bonus, gedeckelt. Rechts derselbe Fall im Februar 2027: Warten kostet bares Geld. Die Berechnung ist als unveränderlicher Snapshot gespeichert.',
      weiter: function () { klicke('a[href*="/Projekte/Detail/"]'); }
    },
    {
      page: function (p) { return p.indexOf('/Projekte/Detail') === 0; },
      target: '[data-tour="statuswechsel"]',
      title: '→ Förderung geprüft',
      text: 'Mit gespeicherter Berechnung ist der Guard zufrieden. „Weiter" schaltet den Status.',
      weiter: function () { klicke('[data-tour="statuswechsel"] button[value="FoerderungGeprueft"]'); }
    },
    {
      page: function (p) { return p.indexOf('/Projekte/Detail') === 0; },
      target: '[data-tour="aktionen"]',
      title: 'Schritt 4: Das Angebot',
      text: 'Für „→ Angeboten" verlangt der Guard ein Angebot mit Positionen UND bestätigter Fördervorbehaltsklausel — die KfW-Stolperfalle Nr. 1 im Vertrag. „Weiter" öffnet das Angebot.',
      weiter: function () { klicke('.vl-actions a[href*="/Projekte/Angebot/"]'); }
    },
    {
      page: function (p) { return p.indexOf('/Projekte/Angebot') === 0; },
      target: '[data-tour="angebot-anlegen"]',
      title: 'Angebot anlegen',
      text: 'Die Nummer wird automatisch vergeben, die letzte Förderberechnung wird verknüpft. „Weiter" legt an.',
      weiter: function () { sende('[data-tour="angebot-anlegen"]'); }
    },
    {
      page: function (p) { return p.indexOf('/Projekte/Angebot') === 0; },
      target: '[data-tour="angebot-position"]',
      title: 'Position erfassen',
      zeigen: function () {
        setzeWert('[data-tour="angebot-position"] input[name="bezeichnung"]', 'Wärmepumpe 12 kW inkl. Montage und Inbetriebnahme');
        setzeWert('[data-tour="angebot-position"] input[name="menge"]', 1);
        setzeWert('[data-tour="angebot-position"] input[name="einheit"]', 'Pausch.');
        setzeWert('[data-tour="angebot-position"] input[name="einzelpreis"]', 26800);
      },
      text: 'Eine Pauschalposition genügt für die Demo: 26.800 € netto. „Weiter" fügt sie hinzu.',
      weiter: function () { sende('[data-tour="angebot-position"] form'); }
    },
    {
      page: function (p) { return p.indexOf('/Projekte/Angebot') === 0; },
      target: '[data-tour="angebot-klausel"]',
      title: 'Die Stolperfalle: Fördervorbehalt',
      text: 'Ohne Fördervorbehaltsklausel im Vertrag riskiert der Kunde seinen Zuschuss. Deshalb ist sie hier Pflicht-Haken und Guard zugleich. Unten steht übrigens schon der „Preis nach Förderung". „Weiter" setzt den Haken.',
      weiter: function () {
        setzeHaken('#fv', true);
        sende('[data-tour="angebot-klausel"]');
      }
    },
    {
      page: function (p) { return p.indexOf('/Projekte/Angebot') === 0; },
      target: '[data-tour="angebot-annehmen"]',
      title: 'Der Kunde sagt zu',
      text: '„Weiter" nimmt das Angebot mit heutigem Vertragsdatum an. Damit sind gleich zwei Übergänge frei: „→ Angeboten" und „→ Beauftragt".',
      weiter: function () {
        setzeWert('[data-tour="angebot-annehmen"] input[name="vertragsdatum"]', heuteIso());
        sende('[data-tour="angebot-annehmen"]');
      }
    },
    {
      page: function (p) { return p.indexOf('/Projekte/Angebot') === 0; },
      target: '[data-tour="angebot-summen"]',
      title: 'Angebot fixiert',
      text: 'Angenommen — Positionen sind jetzt eingefroren. Zurück zum Projekt für die nächsten Stationen.',
      weiter: function () { klicke('a.btn[href*="/Projekte/Detail/"]'); }
    },
    {
      page: function (p) { return p.indexOf('/Projekte/Detail') === 0; },
      target: '[data-tour="statuswechsel"]',
      title: '→ Angeboten',
      text: 'Positionen vorhanden, Klausel bestätigt — der Guard lässt durch.',
      weiter: function () { klicke('[data-tour="statuswechsel"] button[value="Angeboten"]'); }
    },
    {
      page: function (p) { return p.indexOf('/Projekte/Detail') === 0; },
      target: '[data-tour="statuswechsel"]',
      title: '→ Beauftragt',
      text: 'Angebot angenommen, Vertragsdatum erfasst — weiter geht’s.',
      weiter: function () { klicke('[data-tour="statuswechsel"] button[value="Beauftragt"]'); }
    },
    {
      page: function (p) { return p.indexOf('/Projekte/Detail') === 0; },
      target: '[data-tour="aktionen"]',
      title: 'Schritt 5: Montage planen',
      text: '„→ Terminiert" braucht Termin und Team. „Weiter" öffnet die Terminplanung.',
      weiter: function () { klicke('.vl-actions a[href*="/Projekte/Termin/"]'); }
    },
    {
      page: function (p) { return p.indexOf('/Projekte/Termin') === 0; },
      target: '[data-tour="termin-form"]',
      title: 'Termin + Team',
      zeigen: function () {
        setzeWert('input[name="start"]', heuteIso());
        setzeWert('input[name="team"]', 'Team Nord');
      },
      text: 'Start heute, Team Nord. „Weiter" speichert und springt zurück zum Projekt.',
      weiter: function () { sende('[data-tour="termin-form"] form'); }
    },
    {
      page: function (p) { return p.indexOf('/Projekte/Detail') === 0; },
      target: '[data-tour="statuswechsel"]',
      title: '→ Terminiert',
      text: 'Termin und Team stehen — „Weiter" schaltet.',
      weiter: function () { klicke('[data-tour="statuswechsel"] button[value="Terminiert"]'); }
    },
    {
      page: function (p) { return p.indexOf('/Projekte/Detail') === 0; },
      target: '[data-tour="statuswechsel"]',
      title: '→ In Montage',
      text: 'Startdatum gesetzt — die Anlage wird eingebaut.',
      weiter: function () { klicke('[data-tour="statuswechsel"] button[value="InMontage"]'); }
    },
    {
      page: function (p) { return p.indexOf('/Projekte/Detail') === 0; },
      target: '[data-tour="aktionen"]',
      title: 'Schritt 6: Abnahme am Tablet',
      text: '„→ Abgenommen" verlangt Inbetriebnahmedatum, Protokoll UND Unterschrift des Kunden. „Weiter" öffnet die Abnahme.',
      weiter: function () { klicke('.vl-actions a[href*="/Projekte/Abnahme/"]'); }
    },
    {
      page: function (p) { return p.indexOf('/Projekte/Abnahme') === 0; },
      target: '[data-tour="abnahme-form"]',
      title: 'Protokoll, Unterschrift, Fachunternehmererklärung',
      zeigen: function () {
        setzeWert('input[name="inbetriebnahme"]', heuteIso());
        setzeWert('textarea[name="protokoll"]', 'Anlage in Betrieb genommen, Einweisung erfolgt, keine Mängel festgestellt.');
        unterschreibe();
        setzeHaken('#fue', true);
      },
      text: 'Ich habe schon mal unterschrieben. Der wichtigste Haken ist die Fachunternehmererklärung: Ohne sie zahlt die KfW nicht aus — deshalb ist sie der Guard vor der Schlussrechnung. „Weiter" speichert.',
      weiter: function () { sende('[data-tour="abnahme-form"] form'); }
    },
    {
      page: function (p) { return p.indexOf('/Projekte/Detail') === 0; },
      target: '[data-tour="statuswechsel"]',
      title: '→ Abgenommen',
      text: 'Datum, Protokoll, Unterschrift — alles da.',
      weiter: function () { klicke('[data-tour="statuswechsel"] button[value="Abgenommen"]'); }
    },
    {
      page: function (p) { return p.indexOf('/Projekte/Detail') === 0; },
      target: '[data-tour="statuswechsel"]',
      title: '→ Berechnet',
      text: 'Die Fachunternehmererklärung ist ausgestellt — der härteste Guard der Strecke ist zufrieden.',
      weiter: function () { klicke('[data-tour="statuswechsel"] button[value="Berechnet"]'); }
    },
    {
      page: function (p) { return p.indexOf('/Projekte/Detail') === 0; },
      target: '[data-tour="aktionen"]',
      title: 'Schritt 7: Schlussrechnung',
      text: 'Letzte Station: Die Schlussrechnung entsteht direkt aus dem angenommenen Angebot. „Weiter" öffnet die Rechnungen.',
      weiter: function () { klicke('.vl-actions a[href*="/Projekte/Rechnung/"]'); }
    },
    {
      page: function (p) { return p.indexOf('/Projekte/Rechnung') === 0; },
      target: '[data-tour="schlussrechnung"]',
      title: 'Schlussrechnung erzeugen',
      text: '„Weiter" erzeugt die Rechnung aus dem angenommenen Angebot — das Leistungsdatum kommt aus der Abnahme. Abschlagsrechnungen wären hier ebenfalls möglich; die Schlussrechnung würde sie automatisch absetzen.',
      weiter: function () { sende('[data-tour="schlussrechnung"]'); }
    },
    {
      page: function (p) { return p.indexOf('/Projekte/Rechnung') === 0 && p.indexOf('/Projekte/RechnungDetail') !== 0; },
      target: '[data-tour="rechnungen-liste"]',
      title: 'Rechnung steht',
      text: 'Die Schlussrechnung ist da. Ein Klick auf die Nummer öffnet die Details — „Weiter" macht das für dich.',
      weiter: function () { klicke('[data-tour-rechnung]'); }
    },
    {
      page: function (p) { return p.indexOf('/Projekte/RechnungDetail') === 0; },
      target: '[data-tour="rechnung-downloads"]',
      title: 'PDF und XRechnung',
      text: 'Jede Rechnung gibt es als PDF und als XRechnung (EN 16931) — das XML wird in der CI gegen den offiziellen KoSIT-Validator geprüft. Und der KfW-Zuschuss? Wird nicht abgezogen: Der Kunde erhält ihn direkt von der KfW. Zurück zum Projekt für den letzten Übergang.',
      weiter: function () { klicke('.vl-crumb a[href*="/Projekte/Detail/"]'); }
    },
    {
      page: function (p) { return p.indexOf('/Projekte/Detail') === 0; },
      target: '[data-tour="statuswechsel"]',
      title: '→ Abgeschlossen',
      text: '„Weiter" schließt das Projekt ab.',
      weiter: function () { klicke('[data-tour="statuswechsel"] button[value="Abgeschlossen"]'); }
    },
    {
      page: function (p) { return p.indexOf('/Projekte/Detail') === 0; },
      target: '[data-tour="historie"]',
      title: 'Die komplette Kette — dokumentiert',
      text: 'Anfrage bis Abgeschlossen: jeder Übergang mit Zeitpunkt und Benutzer, lückenlos. Zum Abschluss noch ein Blick aufs Dashboard.',
      weiter: function () { location.href = '/Dashboard'; }
    },
    {
      page: function (p) { return p === '/Dashboard'; },
      target: '[data-tour="pipeline"]',
      title: 'Geschafft!',
      text: 'Du hast die Prozessstrecke einmal komplett durchlaufen — mit Guards, Heizlast-Überschlag, Förderrechner, Angebot, Abnahme und Schlussrechnung. Dein Tour-Projekt zählt jetzt bei „Abgeschlossen" mit. Erkunde frei weiter; die Demo-Daten werden regelmäßig zurückgesetzt.',
      final: true
    }
  ];

  // ---------- Anzeige ----------

  var pop = null, overlay = null, markiert = null;

  function aufraeumen() {
    if (pop) { pop.remove(); pop = null; }
    if (overlay) { overlay.remove(); overlay = null; }
    if (markiert) { markiert.classList.remove('vl-tour-highlight'); markiert = null; }
  }

  function beenden() {
    aufraeumen();
    sessionStorage.removeItem(KEY);
  }

  function bauePopover(schritt, index) {
    var el = document.createElement('div');
    el.className = 'vl-tour-pop';
    el.setAttribute('role', 'dialog');
    el.innerHTML =
      '<h3></h3><p></p>' +
      '<div class="vl-tour-foot">' +
      '  <span class="vl-tour-progress">' + (index + 1) + '/' + STEPS.length + '</span>' +
      '  <div class="vl-tour-btns">' +
      '    <button type="button" class="btn btn-sm btn-outline-secondary" data-t="ende">Beenden</button>' +
      '    <button type="button" class="btn btn-sm btn-primary" data-t="weiter">' + (schritt.final ? 'Fertig' : 'Weiter') + '</button>' +
      '  </div>' +
      '</div>';
    el.querySelector('h3').textContent = schritt.title;
    el.querySelector('p').textContent = schritt.text;
    el.querySelector('[data-t="ende"]').addEventListener('click', beenden);
    el.querySelector('[data-t="weiter"]').addEventListener('click', function () { klickWeiter(index); });
    return el;
  }

  function zeige(index) {
    aufraeumen();
    var schritt = STEPS[index];
    if (!schritt) { beenden(); return; }
    sessionStorage.setItem(KEY, String(index));

    if (schritt.zeigen) { try { schritt.zeigen(); } catch (e) { /* Formular fehlt — Popover trotzdem zeigen */ } }

    pop = bauePopover(schritt, index);
    var ziel = schritt.target ? $(schritt.target) : null;

    if (!ziel) {
      // Ziel-Element fehlt (unerwarteter Seitenzustand): Popover mittig
      // zeigen statt Aktionen blind auszuführen.
      overlay = document.createElement('div');
      overlay.style.cssText = 'position:fixed;inset:0;background:rgba(15,18,24,.55);z-index:1050;';
      document.body.appendChild(overlay);
      pop.style.cssText = 'position:fixed;top:50%;left:50%;transform:translate(-50%,-50%);';
      document.body.appendChild(pop);
      return;
    }

    markiert = ziel;
    ziel.classList.add('vl-tour-highlight');
    ziel.scrollIntoView({ block: 'center' });

    pop.style.visibility = 'hidden';
    document.body.appendChild(pop);
    var r = ziel.getBoundingClientRect();
    var popH = pop.offsetHeight, popW = pop.offsetWidth;
    var top = r.bottom + window.scrollY + 12;
    if (r.bottom + popH + 24 > window.innerHeight && r.top - popH - 24 > 0) {
      top = r.top + window.scrollY - popH - 12;
    }
    var left = Math.max(8, Math.min(r.left + window.scrollX, document.documentElement.clientWidth - popW - 8));
    pop.style.top = top + 'px';
    pop.style.left = left + 'px';
    pop.style.visibility = 'visible';
  }

  function klickWeiter(index) {
    var schritt = STEPS[index];
    if (schritt.final) { beenden(); return; }

    var naechster = index + 1;
    sessionStorage.setItem(KEY, String(naechster));

    if (schritt.weiter) {
      // Die Aktion navigiert oder lädt die Seite neu; danach zeigt die
      // neue Seite den nächsten Schritt an.
      try { schritt.weiter(); } catch (e) { beenden(); }
      return;
    }
    if (STEPS[naechster] && STEPS[naechster].page(pfad())) { zeige(naechster); return; }
    beenden();
  }

  function starten() {
    if (pfad() === '/Dashboard') { zeige(0); }
    else { sessionStorage.setItem(KEY, '0'); location.href = '/Dashboard'; }
  }

  // ---------- Verdrahtung ----------

  document.addEventListener('keydown', function (e) {
    if (e.key === 'Escape' && (pop || overlay)) { beenden(); }
  });

  document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('[data-vl-tour-start]').forEach(function (btn) {
      btn.addEventListener('click', starten);
    });

    // Dashboard-Banner: ausblendbar, Entscheidung bleibt gespeichert.
    var banner = $('[data-vl-tour-banner]');
    if (banner) {
      if (localStorage.getItem(BANNER_WEG) === '1') { banner.remove(); }
      else {
        var weg = banner.querySelector('[data-vl-tour-banner-weg]');
        if (weg) {
          weg.addEventListener('click', function () {
            localStorage.setItem(BANNER_WEG, '1');
            banner.remove();
          });
        }
      }
    }

    // Von der Landing-Seite angestoßene Tour: startet nach dem Login.
    if (sessionStorage.getItem(KEY_PENDING) === '1' && pfad() === '/Dashboard') {
      sessionStorage.removeItem(KEY_PENDING);
      zeige(0);
      return;
    }

    var gespeichert = parseInt(sessionStorage.getItem(KEY) || '', 10);
    if (isNaN(gespeichert)) { return; }
    var schritt = STEPS[gespeichert];
    if (schritt && schritt.page(pfad())) {
      zeige(gespeichert);
    }
    // Passt die Seite nicht (Nutzer ist ausgebrochen), bleibt die Tour
    // still — „Tour starten" setzt sie jederzeit von vorn auf.
  });
})();
