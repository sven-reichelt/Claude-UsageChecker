# Claude UsageChecker

Zeigt das Sitzungs- und Wochenlimit des Claude-Abonnements dauerhaft im
Windows-Infobereich an – unabhängig von einer laufenden Claude-Code-Sitzung.
Ein Zeiger auf das Symbol genügt: Auslastung und Restzeit bis zum Reset stehen
im Tooltip, ein Klick öffnet die Detailansicht.

macOS (Menüleiste) ist vorbereitet, aber noch nicht umgesetzt – siehe [Roadmap](#roadmap).

## Funktionsumfang

| Bereich | Stand |
| --- | --- |
| 5-Stunden-Sitzungslimit mit Restzeit | ✅ |
| Wochenlimit gesamt, Opus und Sonnet | ✅ |
| Farbcodiertes Infobereich-Symbol (normal / angespannt / kritisch) | ✅ |
| Detailfenster mit Fortschrittsbalken und Reset-Uhrzeit | ✅ |
| Token verschlüsselt in der Windows-Anmeldeinformationsverwaltung | ✅ |
| Autostart mit Windows | ✅ |
| Aktualisierungsprüfung | 🚧 vorbereitet, siehe [Aktualisierungen](#aktualisierungen) |
| macOS-Menüleiste | 🚧 geplant |

## Datenquelle

Die Anwendung fragt den OAuth-Nutzungsendpunkt der Anthropic-API ab – dieselbe
Quelle, aus der auch `/usage` in Claude Code seine Werte bezieht:

```http
GET https://api.anthropic.com/api/oauth/usage
Authorization: Bearer <oauth_access_token>
anthropic-beta: oauth-2025-04-20
User-Agent:     claude-code/<version>
```

Antwortformat (gekürzt):

```json
{
  "five_hour":        { "utilization": 33.0, "resets_at": "2026-04-11T07:00:00+00:00" },
  "seven_day":        { "utilization": 13.0, "resets_at": "2026-04-17T00:59:59+00:00" },
  "seven_day_opus":   null,
  "seven_day_sonnet": { "utilization":  1.0, "resets_at": "2026-04-16T03:00:00+00:00" }
}
```

Zwei Eigenheiten des Endpunkts prägen den Entwurf:

1. **Der `User-Agent` ist Pflicht.** Ohne einen Claude-Code-User-Agent antwortet
   der Dienst dauerhaft mit HTTP 429.
2. **Er drosselt scharf.** Das Abrufintervall liegt daher bei mindestens
   180 Sekunden und lässt sich nicht darunter einstellen.

Details und die verworfenen Alternativen stehen in
[`docs/api-recherche.md`](docs/api-recherche.md).

## Einrichtung

### Voraussetzungen

* Windows 10/11
* [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) zum Bauen
* Ein aktives Claude-Abonnement (Pro oder Max)

### Bauen und starten

```powershell
git clone https://github.com/sven-reichelt/Claude-UsageChecker.git
cd Claude-UsageChecker
dotnet build
dotnet run --project src/ClaudeUsageChecker.App
```

### Token hinterlegen

Empfohlen ist ein eigenes Langzeit-Token. Es macht die Anwendung vollständig
unabhängig von Claude Code und läuft rund ein Jahr:

```powershell
claude setup-token
```

Das ausgegebene Token (`sk-ant-oat01-…`) im Kontextmenü des Infobereich-Symbols
unter **Einstellungen → Token speichern** einfügen. Es wird ausschließlich
verschlüsselt in der Windows-Anmeldeinformationsverwaltung abgelegt.

Ohne eigenes Token durchsucht die Anwendung folgende Quellen der Reihe nach:

| Reihenfolge | Quelle | Anmerkung |
| --- | --- | --- |
| 1 | Windows-Anmeldeinformationsverwaltung | empfohlen, langlebig |
| 2 | Umgebungsvariable `CLAUDE_CODE_OAUTH_TOKEN` | vor allem für Entwicklung |
| 3 | `%USERPROFILE%\.claude\.credentials.json` | Token von Claude Code, Laufzeit ca. 60 Minuten |

Quelle 3 wird **nur gelesen**. Die Anwendung erneuert niemals ein Token und
schreibt nichts in die Anmeldedaten von Claude Code zurück – siehe
[SECURITY.md](SECURITY.md).

## Projektstruktur

```
Claude-UsageChecker/
├── src/
│   ├── ClaudeUsageChecker.Core/     Plattformunabhängige Logik
│   │   ├── Api/                     HTTP-Client für /api/oauth/usage
│   │   ├── Authentication/          Tokenquellen und Fallback-Kette
│   │   ├── Configuration/           Optionen und JSON-Kontext
│   │   ├── Formatting/              Tooltip- und Detailtexte
│   │   ├── Models/                  Domänenmodell und API-DTOs
│   │   ├── Platform/                Secret-Store, Credential-Reader
│   │   └── Services/                Abrufschleife und Zustandsmodell
│   └── ClaudeUsageChecker.App/      Avalonia-Oberfläche
│       ├── Services/                Aktualisierung, Autostart
│       ├── Settings/                Benutzereinstellungen
│       ├── Tray/                    Infobereich-Symbol und Menü
│       └── Views/                   Detail- und Einstellungsfenster
├── tests/
│   └── ClaudeUsageChecker.Core.Tests/
├── build/                           Werkzeuge (Symbolgenerator)
├── assets/icons/                    Erzeugte Symbole
└── docs/                            Recherche und Architektur
```

Symbole werden aus Code erzeugt statt binär eingecheckt:

```powershell
node build/generate-icons.mjs
```

## Aktualisierungen

Der Update-Weg ist als austauschbare Schnittstelle (`IUpdateService`) angelegt.
Solange das Repository privat ist, ist `DisabledUpdateService` aktiv, denn
GitHub-Releases sind ohne Zugriffstoken nicht abrufbar. Wird das Repository
öffentlich, genügt der Wechsel auf `GitHubReleaseUpdateService` in
`App.axaml.cs`; dieser ist fertig implementiert und getestet.

Bewusste Entscheidung: Die Anwendung lädt Aktualisierungen **nicht** selbst
herunter und führt sie nicht aus. Sie meldet lediglich eine neuere Version und
öffnet die Release-Seite. Das Einspielen bleibt eine bewusste Handlung.

## Roadmap

| Version | Inhalt |
| --- | --- |
| 0.1 | Windows-Infobereich, Sitzungs- und Wochenlimit, Token-Verwaltung |
| 0.2 | Aktualisierungsprüfung aktiv, signiertes Installationspaket |
| 0.3 | macOS-Menüleiste (Schlüsselbund-Anbindung ist bereits vorhanden) |

## Lizenz

[GNU General Public License v3.0 oder neuer](LICENSE)
