# Claude UsageChecker

[![Neueste Fassung](https://img.shields.io/github/v/release/sven-reichelt/Claude-UsageChecker?label=Fassung)](https://github.com/sven-reichelt/Claude-UsageChecker/releases/latest)
[![Vorabversion](https://img.shields.io/github/v/release/sven-reichelt/Claude-UsageChecker?include_prereleases&label=Vorabversion)](https://github.com/sven-reichelt/Claude-UsageChecker/releases)

*English version: [README.md](../../README.md) – das ist die maßgebliche Fassung;
weicht diese hier ab, gilt die englische.*

Zeigt das Sitzungs- und Wochenlimit des Claude-Abonnements dauerhaft im
Windows-Infobereich an – unabhängig von einer laufenden Claude-Code-Sitzung.
Ein Zeiger auf das Symbol genügt: Sitzung und Wochenlimit stehen mit Auslastung,
Reset-Uhrzeit und Restzeit im Tooltip. Das Kontextmenü listet **alle** gemeldeten
Limits auf, ein Klick öffnet die Detailansicht mit Fortschrittsbalken.

Mit eigener Anmeldung läuft sie unabhängig von Claude Code – bestätigt auf einem
Rechner ohne Claude-Code-Installation. Sie läuft unter Windows und, seit 0.8.0,
in der macOS-Menüleiste.

## Funktionsumfang

| Bereich | Stand |
| --- | --- |
| 5-Stunden-Sitzungslimit mit Restzeit | ✅ |
| Wochenlimit gesamt und je Modell (Name aus der API) | ✅ |
| Farbcodiertes Infobereich-Symbol (normal / angespannt / kritisch) | ✅ |
| Detailfenster mit Fortschrittsbalken und Reset-Uhrzeit | ✅ |
| Zusatzkontingent, sofern im Abo aktiviert | ✅ |
| Alle Limits im Kontextmenü | ✅ |
| Einstellbare Schwellen für Gelb und Rot | ✅ |
| Neun Sprachen, samt übersetztem Änderungsverlauf | ✅ |
| Übersicht der Neuerungen nach einer Aktualisierung | ✅ |
| Token verschlüsselt in der Windows-Anmeldeinformationsverwaltung | ✅ |
| Dauerhafte Einrichtung samt Autostart, auf Nachfrage | ✅ |
| Nur eine Instanz je Anmeldesitzung | ✅ |
| Eigene Anmeldung per OAuth mit PKCE – unabhängig von Claude Code | ✅ |
| Selbsttätige Erneuerung des eigenen Tokens | ✅ |
| Aktualisierung auf Knopfdruck, mit Prüfsummenkontrolle | ✅ |
| macOS-Menüleiste, Schlüsselbund und Autostart | ✅ |

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
  "five_hour": { "utilization":  6.0, "resets_at": "2026-08-20T00:30:00+00:00" },
  "seven_day": { "utilization": 18.0, "resets_at": "2026-08-23T01:00:00+00:00" },
  "limits": [
    { "kind": "session",       "percent":  6, "resets_at": "…", "scope": null },
    { "kind": "weekly_all",    "percent": 18, "resets_at": "…", "scope": null },
    { "kind": "weekly_scoped", "percent":  2, "resets_at": "…",
      "scope": { "model": { "display_name": "Fable" } } }
  ]
}
```

Maßgeblich ist die Liste `limits`: Nur sie benennt das begrenzte Modell im
Inhalt. Die älteren Einzelfelder (`seven_day_opus`, `seven_day_sonnet`) tragen
den Namen im Bezeichner und bleiben leer, sobald ein anderes Modell begrenzt
wird – sie dienen nur noch als Rückfall.

Zwei Eigenheiten des Endpunkts prägen den Entwurf:

1. **Der `User-Agent` ist Pflicht.** Ohne einen Claude-Code-User-Agent antwortet
   der Dienst dauerhaft mit HTTP 429.
2. **Er drosselt scharf.** Das Abrufintervall liegt daher bei mindestens
   180 Sekunden und lässt sich nicht darunter einstellen.

Details und die verworfenen Alternativen stehen in
[`docs/api-research.md`](../api-research.md).

### Unterschiede zwischen Pro und Max

Es gibt keine Plan-Erkennung. Jedes Fenster, das die API als `null` meldet, wird
schlicht weggelassen – die Anzeige richtet sich allein danach, was zurückkommt.

| Fenster | Pro | Max |
| --- | --- | --- |
| Sitzung (`kind: session`) | ja | ja |
| Woche gesamt (`kind: weekly_all`) | ja | ja |
| Wochenlimit je Modell (`kind: weekly_scoped`) | je nach Nutzung | je nach Nutzung |
| Zusatzkontingent (`extra_usage`) | vermutlich nein | wenn aktiviert |

Beobachtung aus der Praxis: Die modellbezogenen Wochenfenster erscheinen erst,
wenn das jeweilige Modell in der laufenden Woche genutzt wurde. Die Pro-Spalte ist
recherchiert, nicht gemessen.

**Modellnamen sind nicht fest verdrahtet.** Die Anwendung liest sie aus der
Antwort (`scope.model.display_name`) und beschriftet die Zeile damit – heute
etwa „Woche Fable“. Wechselt Anthropic das begrenzte Modell, erscheint das neue
ohne Änderung an der Anwendung. Die frühere Fassung las Felder mit dem
Modellnamen im Bezeichner (`seven_day_opus`, `seven_day_sonnet`); als das
Wochenlimit auf Fable überging, fehlte es dadurch vollständig in der Anzeige.

## Sprachen

Die Oberfläche liegt in neun Sprachen vor:

| | | |
| --- | --- | --- |
| Deutsch | English | Español |
| Français | Italiano | Português (Brasil) |
| Português (Portugal) | Русский | 简体中文 |

Beim ersten Start richtet sich die Anwendung nach der Sprache des Systems.
Ändern lässt sie sich im Einrichtungsfenster – dort wirkt die Wahl sofort und
wird von **beiden** Schaltflächen übernommen – und später jederzeit unter
**Einstellungen → Sprache**.

Mit der Sprache wechselt auch die Kultur für Zahlen, Datum und Uhrzeit: Wer die
Oberfläche auf Französisch stellt, erwartet dort keine deutschen Datumsangaben.

**Der Änderungsverlauf ist mitübersetzt.** Die Übersicht der Neuerungen nach
einer Aktualisierung erscheint also in derselben Sprache wie die Oberfläche.
Deutsch bleibt die Quelle und steht in [CHANGELOG.md](../changelog/de.md); die
Übersetzungen liegen unter [docs/changelog/](../changelog/). Fehlt eine, zeigt
das Fenster die deutsche Fassung und sagt das dazu.

Nicht übersetzt werden Produkt- und Modellnamen: „Claude UsageChecker“, „Claude
Code“ und der Name des Modells aus der API – „Fable“ heißt in jeder Sprache
Fable.

Die Texte liegen als JSON je Sprache in
`src/ClaudeUsageChecker.Core/Localization/Texts/` – bewusst keine
Satelliten-Assemblies aus `.resx`, weil die Veröffentlichung eine getrimmte
Einzeldatei ist. Wer eine Sprache ergänzen will, legt eine Datei an und trägt
sie in `Language.All` ein; ein Test meldet dann jeden Schlüssel, der noch fehlt.

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

### Was ist neu?

Nach einer Aktualisierung zeigt die Anwendung beim ersten Start, was sich seit
der zuvor gelaufenen Fassung geändert hat – auch über mehrere übersprungene
Fassungen hinweg. Die Quelle ist der mitgelieferte
[Änderungsverlauf](../changelog/de.md), der als Ressource im Programm steckt. Damit
steht die Übersicht ohne Netzzugriff bereit und zeigt zwangsläufig den Stand,
der zur laufenden Fassung gehört.

Wovon die Anwendung kommt, weiß sie nur aus der Einstellungsdatei: Dort steht
unter `lastRunVersion`, welche Fassung zuletzt lief. Beim allerersten Start
entfällt die Übersicht – wer die Anwendung gerade erst kennenlernt, braucht
keine Aufstellung dessen, was er nie gesehen hat.

Jederzeit erreichbar ist der vollständige Verlauf über **Über Claude
UsageChecker …** im Kontextmenü. Dort stehen auch Fassung und Projektseite.

## Farbe des Symbols

Das Symbol richtet sich nach dem jeweils angespanntesten Limit: Sitzung,
Wochenlimit gesamt und die modellbezogenen Wochenlimits. Ab welcher Auslastung
es gelb und ab
welcher es rot wird, steht in den Einstellungen – voreingestellt sind 75 % und
90 %. Die Warnschwelle muss unter der kritischen liegen; andernfalls fände sie
nie statt, und das Fenster sagt das, statt die Eingabe stillschweigend
zurechtzurücken.

## Roadmap

| Version | Inhalt |
| --- | --- |
| 0.1 | Windows-Infobereich, Sitzungs- und Wochenlimit, Token-Verwaltung ✅ |
| 0.2 | Eigene OAuth-Anmeldung, erste Veröffentlichung als Einzeldatei ✅ |
| 0.3 | Aktualisierung auf Knopfdruck mit Prüfsummenkontrolle ✅ |
| 0.4 | Dauerhafte Einrichtung mit Autostart ✅ |
| 0.5 | Einrichtung nach %LOCALAPPDATA%\Programs, wo Windows sie erwartet ✅ |
| 0.6 | Neun Sprachen, modellbezogene Limits, einstellbare Schwellenwerte, Übersicht der Neuerungen nach einem Update ✅ |
| 0.7 | Eigenes Menü im Infobereich, im Stil der Fenster ✅ |
| 0.8 | macOS-Menüleiste ✅ |
| 0.9 | Selbstaustausch unter macOS, und ein signiertes Bündel |

## Lizenz

[GNU General Public License v3.0 oder neuer](../../LICENSE).

Freie Software: benutzen, verstehen, weitergeben, verändern. Wer eine veränderte
Fassung weitergibt, muss deren Quelltext unter denselben Bedingungen mitgeben –
genau darum ging es bei der Wahl.
