# Claude UsageChecker

Zeigt das Sitzungs- und Wochenlimit des Claude-Abonnements dauerhaft im
Windows-Infobereich an – unabhängig von einer laufenden Claude-Code-Sitzung.
Ein Zeiger auf das Symbol genügt: Sitzung und Wochenlimit stehen mit Auslastung,
Reset-Uhrzeit und Restzeit im Tooltip. Das Kontextmenü listet **alle** gemeldeten
Limits auf, ein Klick öffnet die Detailansicht mit Fortschrittsbalken.

Mit eigener Anmeldung läuft sie unabhängig von Claude Code – bestätigt auf einem
Rechner ohne Claude-Code-Installation. macOS (Menüleiste) ist vorbereitet, aber
noch nicht umgesetzt – siehe [Roadmap](#roadmap).

## Funktionsumfang

| Bereich | Stand |
| --- | --- |
| 5-Stunden-Sitzungslimit mit Restzeit | ✅ |
| Wochenlimit gesamt, Opus und Sonnet | ✅ |
| Farbcodiertes Infobereich-Symbol (normal / angespannt / kritisch) | ✅ |
| Detailfenster mit Fortschrittsbalken und Reset-Uhrzeit | ✅ |
| Zusatzkontingent, sofern im Abo aktiviert | ✅ |
| Alle Limits im Kontextmenü | ✅ |
| Token verschlüsselt in der Windows-Anmeldeinformationsverwaltung | ✅ |
| Autostart mit Windows | ✅ |
| Nur eine Instanz je Anmeldesitzung | ✅ |
| Eigene Anmeldung per OAuth mit PKCE – unabhängig von Claude Code | ✅ |
| Selbsttätige Erneuerung des eigenen Tokens | ✅ |
| Aktualisierung auf Knopfdruck, mit Prüfsummenkontrolle | ✅ |
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

### Unterschiede zwischen Pro und Max

Es gibt keine Plan-Erkennung. Jedes Fenster, das die API als `null` meldet, wird
schlicht weggelassen – die Anzeige richtet sich allein danach, was zurückkommt.

| Fenster | Pro | Max |
| --- | --- | --- |
| `five_hour` (Sitzung) | ja | ja |
| `seven_day` (Woche gesamt) | ja | ja |
| `seven_day_opus` | nein, kein Opus im Abo | eigenes Wochenlimit |
| `seven_day_sonnet` | je nach Nutzung | je nach Nutzung |
| `extra_usage` | vermutlich nein | wenn aktiviert |

Beobachtung aus der Praxis: Die modellspezifischen Wochenfenster erscheinen erst,
wenn das jeweilige Modell in der laufenden Woche genutzt wurde. Die Pro-Spalte ist
recherchiert, nicht gemessen.

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

### Anmelden (empfohlen)

**Einstellungen → Anmelden …** startet einen eigenen OAuth-Vorgang mit PKCE.
Damit erhält die Anwendung ein eigenes Zugriffsrecht und braucht keine laufende
Claude-Code-Installation mehr:

1. Auf **Anmeldeseite im Browser öffnen** klicken – die Freigabe erfolgt auf
   claude.ai.
2. Den dort angezeigten Code in das Feld einfügen und **Anmeldung abschließen**.

Angefordert wird ausschließlich `user:profile` – das Recht, den Nutzungsstand zu
lesen. Ausdrücklich **nicht** das Recht, Anfragen im Namen des Kontos zu stellen
oder API-Schlüssel anzulegen. Das Token wird verschlüsselt abgelegt und läuft
selbsttätig weiter; ein erneutes Anmelden ist nicht nötig.

Bewusst ohne lokalen Webserver: Der Code wird von Hand eingefügt, statt über
eine Rückleitung auf `localhost` entgegengenommen zu werden. So öffnet die
Anwendung keinen Port.

Das Zugriffstoken gilt rund acht Stunden. Die Anwendung erneuert es fünf Minuten
vor Ablauf selbst über den Refresh-Token, der dabei rotiert – ein erneutes
Anmelden ist im laufenden Betrieb nicht nötig.

**Wie lange die Anmeldung eine Pause übersteht, ist unbekannt.** Anthropic
dokumentiert die Lebensdauer des Refresh-Tokens nicht und liefert sie in der
Antwort bislang nicht mit. Die Anwendung wertet das Feld
`refresh_token_expires_in` aus, falls es doch einmal kommt.

Sollte die Anmeldung abgelaufen sein, wird sie entfernt und die Detailansicht
weist darauf hin. Die Anzeige läuft dann – sofern vorhanden – über das Token von
Claude Code weiter; ein stilles Zurückfallen ohne Hinweis gibt es nicht.
Eine bloße Störung (Netzwerk, Serverfehler, Drosselung) lässt die Anmeldung
dagegen unangetastet.

### Ohne Anmeldung

Auch ohne eigenen Anmeldevorgang funktioniert die Anwendung, solange Claude Code
angemeldet ist. Die Quellen werden der Reihe nach durchprobiert:

| Reihenfolge | Quelle | Anmerkung |
| --- | --- | --- |
| 1 | Eigene Anmeldung (`ClaudeUsageChecker:OAuth`) | empfohlen, erneuert sich selbst |
| 2 | Von Hand hinterlegtes Token | Sonderfall, muss `user:profile` tragen |
| 3 | Umgebungsvariable `CLAUDE_CODE_OAUTH_TOKEN` | vor allem für Entwicklung |
| 4 | `%USERPROFILE%\.claude\.credentials.json` | Token von Claude Code |

Wird ein Token von der API abgelehnt, rückt die Anwendung zur nächsten Quelle
vor. Eine untaugliche Quelle legt sie also nicht lahm.

> **`claude setup-token` funktioniert hier nicht.**
> Solche Tokens (`sk-ant-oat01-…`) sind gültig und arbeiten einwandfrei gegen
> `/v1/messages`, tragen aber den Geltungsbereich `user:profile` nicht. Der
> Nutzungsendpunkt weist sie mit HTTP 403 ab:
> `OAuth token does not meet scope requirement user:profile`.
> Die Einstellungen prüfen ein eingegebenes Token deshalb vor dem Speichern und
> lehnen es mit dieser Begründung ab. Getestet am 19.08.2026.

Quelle 4 wird **nur gelesen**. Die Anwendung erneuert dieses Token nie und
schreibt nichts in die Anmeldedaten von Claude Code zurück. Das eigene Token
verwaltet sie dagegen vollständig – siehe [SECURITY.md](SECURITY.md).

> **Hinweis zur Client-ID.** Der Anmeldevorgang nutzt die öffentlich bekannte
> OAuth-Client-ID von Claude Code, da Anthropic keine Registrierung eigener
> Anwendungen anbietet. Es werden also ausschließlich eigene Kontodaten mit
> eigener Freigabe abgerufen, die Anwendung meldet sich gegenüber dem
> Autorisierungsserver aber als Claude Code an. Das ist kein offiziell
> unterstützter Weg und kann sich jederzeit ändern.

## Projektstruktur

```
Claude-UsageChecker/
├── src/
│   ├── ClaudeUsageChecker.Core/     Plattformunabhängige Logik
│   │   ├── Api/                     HTTP-Client für /api/oauth/usage
│   │   ├── Authentication/          Tokenquellen, darin OAuth/ für den eigenen Fluss
│   │   ├── Configuration/           Optionen und JSON-Kontext
│   │   ├── Formatting/              Tooltip- und Detailtexte
│   │   ├── Models/                  Domänenmodell und API-DTOs
│   │   ├── Platform/                Secret-Store, Credential-Reader
│   │   └── Services/                Abrufschleife und Zustandsmodell
│   └── ClaudeUsageChecker.App/      Avalonia-Oberfläche
│       ├── Services/                Aktualisierung, Autostart
│       ├── Settings/                Benutzereinstellungen
│       ├── Tray/                    Infobereich-Symbol und Menü
│       └── Views/                   Detail-, Einstellungs- und Anmeldefenster
├── tests/
│   ├── ClaudeUsageChecker.Core.Tests/   Logik, Formatierung, Tokenkette
│   └── ClaudeUsageChecker.App.Tests/    Kopflose UI-Tests (Avalonia.Headless)
├── build/                           Werkzeuge (Symbolgenerator)
├── assets/icons/                    Erzeugte Symbole
└── docs/                            Recherche und Architektur
```

Symbole werden aus Code erzeugt statt binär eingecheckt:

```powershell
node build/generate-icons.mjs
```

## Veröffentlichen

Eine Marke setzen genügt:

```powershell
git tag v0.2.0
git push origin v0.2.0
```

Der Ablauf `.github/workflows/release.yml` testet, baut eine eigenständige
Einzeldatei für Windows x64, prüft Größe und Startfähigkeit, bildet die
SHA-256-Summe und legt einen **Entwurf** der Veröffentlichung an. Erst das
Freigeben von Hand macht sie für die Aktualisierungsprüfung sichtbar – so geht
nichts ungeprüft hinaus.

Das Paket ist getrimmt und komprimiert. Gemessen gegen die unveränderte Fassung:

| Variante | Größe | Start | Arbeitsspeicher |
| --- | --- | --- | --- |
| unverändert | 93 MB | – | – |
| nur komprimiert | 45 MB | 7,2 s | 136 MB |
| **getrimmt + komprimiert** | **21 MB** | **2,3 s** | **87 MB** |

Trimming gewinnt auf allen drei Achsen – entfernter Code muss auch nicht geladen
und übersetzt werden. Die Einstellungen stehen in der Projektdatei, ein lokales
`dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true`
liefert dasselbe Ergebnis.

Das Paket ist nicht signiert – bewusst, denn es ist ein Hobbyprojekt. Windows
SmartScreen meldet deshalb beim ersten Start einen unbekannten Herausgeber; über
**Weitere Informationen → Trotzdem ausführen** bestätigen.

## Aktualisierungen

Die Prüfung läuft gegen die GitHub-Releases dieses Repositorys
(`GitHubReleaseUpdateService`, hinter der austauschbaren Schnittstelle
`IUpdateService`). Sie erfolgt beim Start – sofern in den Einstellungen aktiviert –
und jederzeit über **Auf Aktualisierungen prüfen …** im Kontextmenü.

Das Ergebnis erscheint in der Detailansicht. Bei einer neueren Version stehen
dort zwei Schaltflächen:

* **Jetzt einspielen und neu starten** – lädt die neue Fassung, prüft ihre
  SHA-256-Summe gegen die veröffentlichte, ersetzt die laufende Datei und
  startet neu. Ein Klick, kein manuelles Herunterladen.
* **Release-Seite öffnen** – für alle, die lieber selbst nachsehen.

Stimmt die Prüfsumme nicht oder fehlt sie, wird nichts eingespielt und nichts
ausgeführt. Eingespielt wird ausschließlich nach ausdrücklichem Klick, nie
still im Hintergrund. Die Einzelheiten und die Grenzen dieser Absicherung
stehen in [SECURITY.md](SECURITY.md).

Der Selbstaustausch setzt die veröffentlichte Einzeldatei voraus. Im
Entwicklungsstand liegen Dutzende Dateien nebeneinander – dort wird die
Schaltfläche gar nicht erst angeboten.

**Die Datei heißt in jeder Veröffentlichung gleich: `ClaudeUsageChecker.exe`.**
Das hat zwei Gründe. Der Selbstaustausch schreibt die neue Fassung an den Pfad
der laufenden Datei – ein versionierter Name behauptete danach eine falsche
Version. Und Windows merkt sich die Anheftung im Infobereich pro Pfad: Bliebe
der Name nicht gleich, landete das Symbol nach jedem Update wieder im
Überlaufbereich.

Solange es keine Veröffentlichung gibt, meldet die Prüfung das offen, statt zu
schweigen.

## Roadmap

| Version | Inhalt |
| --- | --- |
| 0.1 | Windows-Infobereich, Sitzungs- und Wochenlimit, Token-Verwaltung ✅ |
| 0.2 | Eigene OAuth-Anmeldung, erste Veröffentlichung als Einzeldatei ✅ |
| 0.3 | Aktualisierung auf Knopfdruck mit Prüfsummenkontrolle ✅ |
| 0.4 | macOS-Menüleiste (Schlüsselbund-Anbindung ist bereits vorhanden) |

## Lizenz

[GNU General Public License v3.0 oder neuer](LICENSE)
