# Sicherheit

Claude UsageChecker verarbeitet ein OAuth-Token, das vollen Zugriff auf ein
Claude-Abonnement gewährt. Der Umgang damit folgt einigen festen Regeln.

## Grundsätze

### 1. Keine personenbezogenen Daten im Repository

Weder Tokens noch Kontodaten, Nutzungswerte oder Protokolle gehören in die
Versionsverwaltung. `.gitignore` sperrt die einschlägigen Muster bereits an
erster Stelle – unter anderem `*.credentials.json`, `*.token`, `.env`, `*.pfx`,
`*.pem` und `settings.local.json`.

Vor jedem Commit prüfen:

```powershell
git diff --cached | Select-String -Pattern "sk-ant-", "Bearer ", "oat01"
```

### 2. Tokens niemals im Klartext auf der Platte

Das Token wird ausschließlich über den Secret-Store des Betriebssystems
abgelegt:

| Plattform | Ablage | Schutz |
| --- | --- | --- |
| Windows | Anmeldeinformationsverwaltung (`CredWriteW`) | DPAPI, an das Benutzerkonto gebunden |
| macOS | Schlüsselbund (geplant) | Keychain Services |

Die Einstellungsdatei `%LOCALAPPDATA%\ClaudeUsageChecker\settings.json` enthält
ausschließlich Verhaltenseinstellungen und niemals Geheimnisse.

### 3. Tokens werden nur gelesen, nie erneuert

Die Anwendung greift lesend auf `%USERPROFILE%\.claude\.credentials.json` zu,
schreibt dort aber nichts zurück und ruft keinen Refresh-Endpunkt auf. Der
Grund: Anthropic rotiert Refresh-Tokens. Würde diese Anwendung ein Token
erneuern, verlöre die Claude-Code-Installation ihre Anmeldung. Läuft das
mitgelesene Token ab, meldet die Oberfläche das offen, statt eigenmächtig zu
handeln.

Der `refreshToken` wird deshalb bewusst nicht einmal in ein Modell eingelesen
(siehe `ClaudeCliCredentials`).

### 4. Tokenwerte gelangen nie in Protokolle

`AccessToken.ToString()` gibt ausschließlich Herkunft und Ablaufzeitpunkt aus.
Ein Test (`ToString_GibtDenTokenwertNichtPreis`) sichert das ab.

### 5. Sparsame Netzwerkkommunikation

Es werden genau zwei Gegenstellen kontaktiert:

| Ziel | Zweck | Übertragene Daten |
| --- | --- | --- |
| `api.anthropic.com/api/oauth/usage` | Nutzungsstand abrufen | nur das Bearer-Token |
| `api.github.com` (optional) | Versionsprüfung | keine, nur ein GET |

Es gibt keine Telemetrie, keine Absturzberichte an Dritte und keine Analytik.
Absturzberichte werden lokal nach
`%LOCALAPPDATA%\ClaudeUsageChecker\crash.log` geschrieben und bleiben dort.

### 6. Keine selbsttätige Ausführung von Fremdcode

Die Aktualisierungsprüfung lädt nichts herunter und startet nichts. Sie öffnet
lediglich die Release-Seite im Browser.

### 7. Rücksicht auf die API

Der Endpunkt drosselt aggressiv. `MonitorOptions.MinimumInterval` erzwingt
mindestens 180 Sekunden zwischen zwei Abrufen; ein `Retry-After` des Servers hat
immer Vorrang, und nach Fehlschlägen greift ein exponentiell wachsender Backoff
bis 30 Minuten.

## Schwachstellen melden

Bitte **kein** öffentliches Issue anlegen. Meldungen gehen über
[GitHub Security Advisories](https://github.com/sven-reichelt/Claude-UsageChecker/security/advisories/new)
oder direkt an den Repository-Inhaber.

## Prüfliste vor einer Veröffentlichung

- [ ] `git log -p` nach Tokenmustern durchsucht (`sk-ant-`, `oat01`, `Bearer `)
- [ ] Keine Datei aus `%USERPROFILE%\.claude\` im Repository
- [ ] Screenshots enthalten keine Kontodaten
- [ ] `settings.json` und `crash.log` nicht eingecheckt
- [ ] Abhängigkeiten geprüft (`dotnet list package --vulnerable`)
