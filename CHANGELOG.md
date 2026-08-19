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
