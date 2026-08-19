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
