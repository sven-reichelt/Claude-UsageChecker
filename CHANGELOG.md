# Änderungsverlauf

Das Format folgt [Keep a Changelog](https://keepachangelog.com/de/1.1.0/),
die Versionierung [Semantic Versioning](https://semver.org/lang/de/).

## [Unveröffentlicht]

## [0.5.0] – 2026-08-19

### Geändert
- Der Zielort der Einrichtung ist jetzt
  `%LOCALAPPDATA%\Programs\ClaudeUsageChecker` statt
  `%USERPROFILE%\ClaudeUsageChecker`. Das ist der von Windows vorgesehene Ort
  für Anwendungen ohne Administratorrechte – dort liegen etwa auch VS Code und
  Signal. Die Wurzel des Benutzerprofils bleibt damit frei, wo neben Dokumenten
  und Downloads niemand Programme erwartet.

  **Bereits eingerichtete Fassungen ziehen nicht von selbst um.** Sie laufen
  weiter vom alten Ort. Zum Umziehen genügt: Einstellungen öffnen und speichern
  – bei gesetztem Autostart-Haken wird dabei an den neuen Ort kopiert. Das alte
  Verzeichnis kann danach von Hand gelöscht werden.

## [0.4.2] – 2026-08-19

### Behoben
- Wer die Einrichtung beim ersten Start überspringt und später nur den Haken
  „Mit Windows starten" setzt, bekam einen Autostart-Eintrag, der auf den
  Download-Ordner zeigte – beim ersten Aufräumen dort wäre er wertlos gewesen.
  Der Haken zieht das Umziehen jetzt ebenfalls nach sich, mit vorherigem
  Hinweis auf Zielpfad und Neustart.
- Das **Abwählen** lässt die Anwendung dagegen, wo sie ist. Entfernt wird nur
  der Autostart-Eintrag; einmal eingerichtet bleibt eingerichtet.

## [0.4.1] – 2026-08-19

### Behoben
- Die Entpackungsordner früherer Fassungen blieben im Temporärverzeichnis
  liegen. Eine komprimierte Einzeldatei kann ihre nativen Bibliotheken nicht
  aus dem Bündel laden – die .NET-Laufzeit packt sie nach
  `%TEMP%\.net\ClaudeUsageChecker\<Kennung>` aus, und da die Kennung am Inhalt
  hängt, bekam jede Version einen eigenen Ordner. Rund 16 MB je Update, die
  sich unbegrenzt sammelten. Die Anwendung räumt sie jetzt selbst weg.

### Dokumentation
- [SECURITY.md](SECURITY.md) listet vollständig auf, was die Anwendung wo
  ablegt und was nach einer Deinstallation zurückbliebe.

## [0.4.0] – 2026-08-19

### Hinzugefügt
- **Dauerhafte Einrichtung.** Läuft die Anwendung außerhalb ihres Zielorts,
  bietet sie beim ersten Start einmalig an, sich nach
  `%USERPROFILE%\ClaudeUsageChecker` zu kopieren, den Autostart einzurichten
  und von dort neu zu starten. Grund ist nicht Ordnungsliebe: Autostart,
  Anheftung im Infobereich und Selbstaustausch hängen alle am Pfad der
  ausführbaren Datei – liegt sie im Download-Ordner, bricht alles drei, sobald
  dort aufgeräumt wird.
- Der Autostart wird zusammen mit der Einrichtung aktiviert und zeigt auf den
  Zielpfad, nicht auf den Startort. Abschaltbar in den Einstellungen.

### Geändert
- Das Detailfenster erscheint mittig auf dem Bildschirm und trägt einen
  schmalen Rahmen in der Farbe des Symbols statt des Systemrahmens.

### Hinzugefügt
- Ein Test prüft, dass der Rahmen seine Farbe tatsächlich bekommt. Ein nicht
  auflösbares `DynamicResource` bliebe sonst stillschweigend leer.

## [0.3.3] – 2026-08-19

### Geändert
- Die veröffentlichte Datei heißt in jeder Version gleich:
  `ClaudeUsageChecker.exe` statt `ClaudeUsageChecker-0.3.2-win-x64.exe`.
  Der Selbstaustausch schreibt die neue Fassung an den Pfad der laufenden
  Datei – ein versionierter Name behauptete danach eine falsche Version. Und
  Windows merkt sich die Anheftung im Infobereich pro Pfad: Bliebe der Name
  nicht gleich, landete das Symbol nach jedem Update wieder im
  Überlaufbereich.

## [0.3.2] – 2026-08-19

### Behoben
- Die Schaltflächen des Update-Hinweises ragten aus dem Fenster. Nebeneinander
  brauchten sie rund 420 Pixel, das Fenster ist 380 breit – „Release-Seite
  öffnen" war nur halb lesbar. Sie stehen jetzt untereinander.

### Hinzugefügt
- Tests, die Überlauf im Detailfenster aufdecken. Sie vermessen die
  tatsächliche Platzierung nach einem vollständigen Layout-Durchlauf und
  vergleichen den rechten Rand jedes Elements mit der Fensterbreite. Weder die
  gewünschte Größe der Steuerelemente noch die des Fensters taugen dafür:
  Avalonia begrenzt beide auf die Vorgabe, sodass ein Überlauf darin gar nicht
  auftauchen kann.

## [0.3.1] – 2026-08-19

### Geändert
- Die Oberfläche schreibt Umlaute als Umlaute. Bisher stand dort „Auf
  Aktualisierungen pruefen", „Gueltig bis" oder „Der Browser liess sich nicht
  oeffnen" – die Umschreibungen stammten aus der Entwicklung und hatten in der
  Anzeige nichts verloren. 36 Zeichenketten betroffen.
- Die Meldung bei fehlendem Zugriffsrecht verweist auch dort auf die
  Einstellungen, wo sie bisher ein Token zum Hinterlegen verlangte.

### Hinzugefügt
- Ein Test prüft die Zeichenkodierung von der Quelldatei bis in die
  Oberfläche. Ein Kodierungsfehler fällt damit im Testlauf auf statt beim
  Nutzer.

## [0.3.0] – 2026-08-19

Die erste Fassung, die sich selbst aktualisieren kann. Ab hier genügt ein Klick
– das Herunterladen von Hand entfällt.

### Behoben
- Versionen werden dreistellig angezeigt. Die vierte Stelle stammt aus der
  Assembly-Version und sagt nichts aus – „Version 0.2.0.0 ist aktuell"
  verwirrte nur.

### Hinzugefügt
- **Aktualisierung auf Knopfdruck.** „Jetzt einspielen und neu starten" lädt
  die neue Fassung, prüft ihre SHA-256-Summe gegen die veröffentlichte,
  ersetzt die laufende Datei und startet neu. Ein Hinweis, den man von Hand
  abarbeiten muss, bleibt in der Praxis liegen.
  - Stimmt die Prüfsumme nicht oder fehlt sie, wird nichts eingespielt und
    nichts ausgeführt.
  - Die Adresse stammt aus der GitHub-Antwort zu diesem Repository; Adressen
    ohne HTTPS werden verworfen.
  - Nur nach ausdrücklichem Klick, nie still im Hintergrund.
  - Der Austausch nutzt, dass Windows eine laufende Datei umbenennen lässt.
    Scheitert das Einsetzen, wird das Umbenennen zurückgenommen.

### Geändert
- „Details anzeigen" ist aus dem Kontextmenü entfernt. Der Linksklick auf das
  Symbol öffnet die Detailansicht, und die Zahlen stehen ohnehin in den
  Statuszeilen darüber – der Eintrag bot nur denselben Weg ein zweites Mal.
- Der Hinweis bei fehlendem Zugriffsrecht nennt zuerst die eigene Anmeldung.
  Bisher stand dort „Melde dich in Claude Code an" – ein Rat, dem auf einem
  Rechner ohne Claude Code niemand folgen konnte.

## [0.2.0] – 2026-08-19

Erste Veröffentlichung. Eigenständige Einzeldatei für Windows x64, 21 MB,
kein .NET-Runtime nötig.

### Anzeige

- 5-Stunden-Sitzungslimit und Wochenlimits (gesamt, Opus, Sonnet) aus
  `GET /api/oauth/usage` – autoritative Werte, keine Schätzung.
- Tooltip mit Auslastung, Reset-Uhrzeit und Restzeit. Bei einem Reset an einem
  anderen Tag steht der Wochentag davor, ab einer Woche Abstand das Datum –
  eine bloße Uhrzeit wäre für das Wochenlimit mehrdeutig.
- Kontextmenü mit **allen** gemeldeten Limits.
- Detailfenster mit Fortschrittsbalken, Reset-Zeiten, Zusatzkontingent
  (`extra_usage`) und der tatsächlich verwendeten Tokenquelle.
- Farbcodiertes Infobereich-Symbol: normal, angespannt, kritisch.

### Anmeldung

- **Eigene Anmeldung per OAuth mit PKCE** (RFC 7636, S256) – macht die
  Anwendung unabhängig von einer laufenden Claude-Code-Installation.
  Angefordert wird ausschließlich `user:profile`; ausdrücklich **nicht**
  `user:inference` und **nicht** `org:create_api_key`.
- Ohne lokalen Webserver: Der Code wird von Hand eingefügt statt über eine
  Rückleitung auf `localhost` entgegengenommen. Kein offener Port.
- Das eigene Token wird selbsttätig erneuert. Beim mitgelesenen Token von
  Claude Code unterbleibt das bewusst – ein rotierender Refresh-Token würde
  dessen Anmeldung entwerten. Getrennte Einträge im Secret-Store.
- Läuft die eigene Anmeldung ab, wird sie entfernt und gemeldet, statt still
  auf Claude Code zurückzufallen. Eine bloße Störung (Netzwerk, 5xx,
  Drosselung) lässt sie dagegen unangetastet.
- Fallback-Kette: eigene Anmeldung → hinterlegtes Token → Umgebungsvariable →
  Claude Code. Lehnt die API eine Quelle ab, rückt der Abruf zur nächsten vor.

### Betrieb

- Abrufintervall mindestens 180 Sekunden, exponentieller Backoff nach
  Fehlschlägen, `Retry-After` des Servers hat Vorrang.
- Nur eine Instanz je Anmeldesitzung.
- Autostart mit Windows, abschaltbar.
- Aktualisierungsprüfung über GitHub-Releases. Es wird nichts heruntergeladen
  oder ausgeführt – nur gemeldet und auf Wunsch die Release-Seite geöffnet.
- Fehler in Aktionen des Infobereichs beenden die Anwendung nicht mehr, sondern
  landen mit Kontext in `crash.log`.

### Erkenntnisse, die den Entwurf geprägt haben

- **`claude setup-token` taugt für diesen Zweck nicht.** Solche Tokens sind
  gültig und arbeiten gegen `/v1/messages`, tragen aber `user:profile` nicht.
  Der Nutzungsendpunkt weist sie mit HTTP 403 ab. Das war die ursprüngliche
  Annahme des Projekts und ist widerlegt.
- **Der Tokenendpunkt liegt auf `platform.claude.com`**, nicht mehr auf
  `console.anthropic.com` – dort antwortet er mit HTTP 404.
- **Der `User-Agent` ist Pflicht.** Ohne einen Claude-Code-User-Agent drosselt
  der Nutzungsendpunkt dauerhaft mit HTTP 429.
- Getrimmt und komprimiert gebaut: 21 MB statt 93 MB, Start in 2,3 statt
  7,2 Sekunden, 87 statt 136 MB Arbeitsspeicher. Trimming gewinnt auf allen
  drei Achsen – entfernter Code muss auch nicht geladen und übersetzt werden.

### Bekannte Einschränkungen

- Das Paket ist **nicht signiert**. Windows SmartScreen meldet beim ersten
  Start einen unbekannten Herausgeber.
- Wie lange die eigene Anmeldung eine längere Pause übersteht, ist unbekannt –
  Anthropic dokumentiert die Lebensdauer des Refresh-Tokens nicht.
- Der Anmeldevorgang nutzt die öffentlich bekannte OAuth-Client-ID von Claude
  Code, da Anthropic keine Registrierung eigener Anwendungen anbietet. Kein
  offiziell unterstützter Weg; er kann sich jederzeit ändern.
- macOS ist vorbereitet, aber nicht umgesetzt.
