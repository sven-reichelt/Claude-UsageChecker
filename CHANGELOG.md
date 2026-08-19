# Änderungsverlauf

Das Format folgt [Keep a Changelog](https://keepachangelog.com/de/1.1.0/),
die Versionierung [Semantic Versioning](https://semver.org/lang/de/).

## [Unveröffentlicht]

### Hinzugefügt
- Grundgerüst: Avalonia-Anwendung für den Windows-Infobereich
- Abruf von `GET /api/oauth/usage` mit den erforderlichen Kopfzeilen
- Anzeige von 5-Stunden-Sitzungslimit und Wochenlimits (gesamt, Opus, Sonnet)
- Tokenkette: Windows-Anmeldeinformationsverwaltung → Umgebungsvariable →
  Anmeldedaten der Claude-Code-CLI (ausschließlich lesend)
- Farbcodiertes Infobereich-Symbol und Detailfenster mit Fortschrittsbalken
- Abrufschleife mit Mindestintervall von 180 Sekunden und exponentiellem Backoff
- Einstellungen inklusive Autostart mit Windows
- Austauschbare Aktualisierungsprüfung (`IUpdateService`)
- Symbolgenerator ohne externe Abhängigkeiten (`build/generate-icons.mjs`)

### Behoben
- Detail- und Einstellungsfenster ließen sich nicht öffnen: Eine selbst
  geschriebene, parameterlose `InitializeComponent`-Methode verdeckte die von
  Avalonia erzeugte Fassung. Das XAML wurde geladen, die Felder der benannten
  Steuerelemente blieben aber null, und der Konstruktor scheiterte mit einer
  `NullReferenceException`. Da die Anwendung kein Fenster besitzt, lief die
  Ausnahme bis in die Nachrichtenschleife durch und beendete sie kommentarlos.
- Fehler in Aktionen des Infobereichs beenden die Anwendung nicht mehr. Sie
  werden mit Kontext nach `crash.log` protokolliert (`ErrorGuard`), zusätzlich
  greifen globale Handler für unbehandelte und nicht abgewartete Ausnahmen.
- `WindowsCredentialStore.Write` gab den Tokenpuffer mit
  `ZeroFreeGlobalAllocUnicode` frei, obwohl er aus `AllocHGlobal` stammt. Jetzt
  wird der Puffer gezielt überschrieben und mit `FreeHGlobal` freigegeben.

### Hinzugefügt
- Kopfloses UI-Testprojekt (`ClaudeUsageChecker.App.Tests`, 7 Tests), das die
  Erzeugung beider Fenster und die Verknüpfung der benannten Steuerelemente
  absichert – genau die Fehlerklasse, die zuvor unbemerkt blieb.
- Die Anwendung läuft nur noch einmal je Anmeldesitzung. Ein zweiter Start legte
  bislang ein zweites Symbol im Infobereich an und fragte die API doppelt ab, was
  den drosselungsempfindlichen Endpunkt unnötig belastet.

### Geändert
- Der Tooltip nennt jetzt die Reset-**Uhrzeit** zusätzlich zur Restzeit, z. B.
  „Sitzung 19 % - Reset 16:30 (2 Std 17 Min)". Bei einem Reset an einem anderen
  Tag steht der Wochentag davor, ab einer Woche Abstand das Datum – eine bloße
  Uhrzeit wäre für das Wochenlimit mehrdeutig.
- Das Kontextmenü listet nun **alle** gemeldeten Limits statt nur der Sitzung,
  jeweils mit Auslastung und Restzeit. Die Uhrzeit ist dafür in den Tooltip
  gewandert.
- Die Aktualisierungsprüfung läuft gegen die GitHub-Releases des nun
  öffentlichen Repositorys. Ihr Ergebnis erscheint in der Detailansicht; bei
  einer neueren Version führt eine Schaltfläche zur Release-Seite. Zuvor blieb
  ein Klick auf „Auf Aktualisierungen prüfen" ohne jede Rückmeldung.

### Hinzugefügt
- Das Zusatzkontingent (`extra_usage`) erscheint in der Detailansicht mit
  Fortschrittsbalken und verbrauchten Credits, sofern das Abo es meldet. Die
  Werte wurden bislang abgerufen, aber nirgends angezeigt.

### Behoben
- Der Wochentag im Tooltip war mehrdeutig: `GetShortestDayName` liefert im
  Deutschen einen einzelnen Buchstaben, sodass „S" für Samstag wie für Sonntag
  stand. Jetzt wird die Abkürzung verwendet („So 02:59").

### Entfernt
- `DisabledUpdateService` – der Platzhalter für das private Repository hat keinen
  Aufrufer mehr.
