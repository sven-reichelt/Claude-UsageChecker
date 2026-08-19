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

Zwei getrennte Einträge:

| Eintrag | Inhalt |
| --- | --- |
| `ClaudeUsageChecker:OAuth` | Eigene Anmeldung (Access- und Refresh-Token) |
| `ClaudeUsageChecker:OAuthToken` | Von Hand hinterlegtes Einzeltoken (Sonderfall) |

Die Einstellungsdatei `%LOCALAPPDATA%\ClaudeUsageChecker\settings.json` enthält
ausschließlich Verhaltenseinstellungen und niemals Geheimnisse.

### 3. Fremde Anmeldedaten werden nur gelesen, eigene selbst verwaltet

Hier ist streng zu trennen:

**Anmeldedaten von Claude Code** (`%USERPROFILE%\.claude\.credentials.json` bzw.
macOS-Schlüsselbund) werden ausschließlich **gelesen**. Die Anwendung schreibt
dort nichts zurück und erneuert diese Tokens nicht. Der Grund: Anthropic rotiert
Refresh-Tokens – eine Erneuerung durch diese Anwendung würde die Anmeldung der
Claude-Code-Installation entwerten. Der `refreshToken` wird deshalb nicht einmal
in ein Modell eingelesen (siehe `ClaudeCliCredentials`).

**Eigene Anmeldedaten** aus dem OAuth-Fluss dieser Anwendung gehören ihr allein.
Sie werden sehr wohl erneuert, sobald sie ablaufen – ein rotierender
Refresh-Token entwertet hier nichts Fremdes. Genau das macht die Anwendung
unabhängig von einer laufenden Claude-Code-Installation.

Beide liegen in getrennten Einträgen des Secret-Stores und werden nie vermischt.

### 3a. Der eigene Anmeldefluss

* **PKCE mit S256** (RFC 7636) bindet den Codetausch an den Vorgang, der ihn
  angefordert hat. Verifier und `state` werden je Vorgang neu aus
  `RandomNumberGenerator` erzeugt.
* **Least Privilege:** Angefordert wird ausschließlich `user:profile` – das
  Recht, den Nutzungsstand zu lesen. Ausdrücklich **nicht** `user:inference`
  (Anfragen im Namen des Kontos stellen) und **nicht** `org:create_api_key`.
* **Kein lokaler Webserver.** Der Code wird von Hand eingefügt statt über eine
  Rückleitung auf `localhost` entgegengenommen. Das erspart einen offenen Port
  und einen lauschenden Dienst auf dem Rechner des Nutzers.
* Ein Code aus einem anderen Vorgang wird am `state` erkannt und gar nicht erst
  abgeschickt.

### 4. Tokenwerte gelangen nie in Protokolle

`AccessToken.ToString()` gibt ausschließlich Herkunft und Ablaufzeitpunkt aus.
Ein Test (`ToString_GibtDenTokenwertNichtPreis`) sichert das ab.

### 5. Sparsame Netzwerkkommunikation

Es werden genau zwei Gegenstellen kontaktiert:

| Ziel | Zweck | Übertragene Daten |
| --- | --- | --- |
| `api.anthropic.com/api/oauth/usage` | Nutzungsstand abrufen | nur das Bearer-Token |
| `claude.ai/oauth/authorize` | Anmeldeseite, nur im Browser des Nutzers | – |
| `platform.claude.com/v1/oauth/token` | Code tauschen, Token erneuern | Code, PKCE-Verifier bzw. Refresh-Token |
| `api.github.com` (optional) | Versionsprüfung | keine, nur ein GET |

Es gibt keine Telemetrie, keine Absturzberichte an Dritte und keine Analytik.
Absturzberichte werden lokal nach
`%LOCALAPPDATA%\ClaudeUsageChecker\crash.log` geschrieben und bleiben dort.

### 6. Aktualisierungen: heruntergeladener Code nur mit geprüfter Herkunft

Die Anwendung kann sich auf Knopfdruck selbst ersetzen. Sie lädt dabei eine
ausführbare Datei aus dem Netz und startet sie – der heikelste Vorgang im
gesamten Programm. Ursprünglich war das bewusst ausgeschlossen; die Entscheidung
wurde umgekehrt, weil ein Hinweis, den man von Hand abarbeiten muss, in der
Praxis liegen bleibt und die Anwendung dann veraltet läuft.

Abgesichert ist das durch drei Bedingungen. Fehlt eine, wird nichts eingespielt:

1. **Geprüfte Prüfsumme.** Zu jeder Veröffentlichung gehört eine
   SHA-256-Summe. Die heruntergeladene Datei wird gehasht und verglichen. Bei
   Abweichung wird sie verworfen und **nicht** ausgeführt. Ohne
   Prüfsummendatei wird gar nicht erst begonnen.
2. **Adresse aus der GitHub-Antwort.** Die Download-Adresse stammt aus der
   API-Antwort zu genau diesem Repository und wird nicht aus Dateinamen
   zusammengesetzt oder erraten. Adressen ohne HTTPS werden verworfen.
3. **Ausdrückliche Handlung des Nutzers.** Eingespielt wird nur nach einem
   Klick auf **Jetzt einspielen und neu starten**. Es gibt keine stille
   Aktualisierung im Hintergrund.

**Was die Prüfsumme nicht leistet.** Sie ersetzt keine Signatur: Wer eine
Veröffentlichung anlegen kann, legt auch die passende Prüfsumme an. Sie schützt
gegen beschädigte und unterwegs veränderte Downloads – nicht gegen ein
übernommenes Konto.

Das ist eine bewusst getroffene Entscheidung: Veröffentlichungen erstellt
ausschließlich der Repository-Inhaber, das Bedrohungsmodell ist der fehlerhafte
Download, nicht der Angreifer mit Schreibrechten. Damit gehört allerdings die
Absicherung des GitHub-Kontos zur Sicherheitskette – ohne
Zwei-Faktor-Authentifizierung dort ist der Schutz hier hinfällig.

Wer strengere Anforderungen hat, signiert die Pakete mit einem
Codesignatur-Zertifikat und prüft die Signatur statt der Summe. Für dieses
Hobbyprojekt steht der Aufwand in keinem Verhältnis.

Der Austausch selbst nutzt aus, dass Windows eine laufende Datei zwar nicht
überschreiben, wohl aber umbenennen lässt: umbenennen, neue Datei an den alten
Platz, neue Fassung starten, selbst beenden. Scheitert der zweite Schritt, wird
der erste zurückgenommen – es bleibt immer ein lauffähiges Programm zurück.

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
