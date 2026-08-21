# Änderungsverlauf

Übersetzung von [CHANGELOG.md](../../CHANGELOG.md). Englisch ist die Quelle;
weichen beide voneinander ab, gilt der englische Text.

Das Format folgt [Keep a Changelog](https://keepachangelog.com/de/1.1.0/),
die Versionierung [Semantic Versioning](https://semver.org/lang/de/).

## [Unveröffentlicht]

## [0.9.0] – 2026-08-21

### Hinzugefügt
- **Das macOS-Paket ist signiert und von Apple beglaubigt.** Bisher trug es
  eine Ad-hoc-Signatur, die für alle außer dem Rechner, der sie erzeugt hat,
  wertlos ist: macOS verweigerte den Start, und ihn doch zu erreichen hieß,
  dem System den Einwand von Hand auszureden. Nun trägt es eine Developer-ID
  und Apples Beglaubigung, fest ins Paket geheftet, damit sie auch ohne
  Netzverbindung gilt. Es startet per Doppelklick wie alles andere.
- **Selbstaustausch unter macOS.** Die Schaltfläche, die seit 0.4.0 die
  Windows-Fassung erneuert, tut hier nun dasselbe. Der Unterschied liegt darin,
  was ersetzt wird – ein ganzes Paket statt einer Datei – und was zuvor geprüft
  wird: neben der veröffentlichten SHA-256-Prüfsumme auch die Signatur der
  geladenen Fassung, mit derselben Frage, die auch macOS stellen würde. Was
  macOS nicht ausführen würde, wird auch nicht eingespielt.

  Beides zusammen, weil keines das andere ersetzt. Eine Prüfsumme belegt, dass
  die Datei die vom Server gesendete ist; eine Signatur belegt, wer sie gebaut
  hat.
- **macOS wird als Disk-Image ausgeliefert.** Öffnen, die Anwendung auf den
  danebenliegenden Programme-Ordner ziehen, fertig – kein Terminal, kein
  Auspacken.

  Die Bequemlichkeit ist dabei der kleinere Teil des Grundes. Der größte Teil
  dieser Anwendung besteht aus verwalteten .NET-Dateien, und die sind keine
  Mach-O-Dateien, weshalb ihre Signatur nicht in ihnen selbst liegen kann:
  macOS legt sie in erweiterten Attributen daneben ab. Ein Zip kann die nur als
  Beiwagen tragen, und ob daraus wieder Attribute werden, entscheidet das
  Werkzeug, das es auspackt. Wo das schiefgeht, stehen 202 der 221 Dateien im
  Paket unsigniert da, die Beglaubigung passt nicht mehr zu einer Signatur, die
  nicht mehr gilt, und macOS meldet, es könne die Anwendung nicht auf
  Schadsoftware prüfen – was wie ein Vorwurf klingt und ein verlorenes
  Dateiattribut ist.

  Ein Disk-Image wird eingehängt statt ausgepackt. Zwischen der Signatur und
  dem prüfenden System steht nichts. Das Zip bleibt der Veröffentlichung
  beigelegt, für die Update-Prüfung, die es mit `ditto` auspackt und dabei
  behält, worauf es ankommt.

## [0.8.0] – 2026-08-20

### Hinzugefügt
- **macOS.** Die Anwendung wohnt jetzt auch in der Menüleiste: ein Symbol mit
  den gemeldeten Limits, dieselben Fenster, dieselben neun Sprachen. Die eigene
  Anmeldung wandert in den Schlüsselbund, der Autostart läuft über einen
  Launch-Agent des Benutzers, und das Token einer Claude-Code-Installation wird
  wie bisher aus dem Schlüsselbund mitgelesen.

  Das Menü ist dort ein natives – die umgekehrte Entscheidung wie unter Windows,
  aus demselben Grund: Ein Menüleisten-Symbol öffnet ein Systemmenü, und ein
  nachgebautes Fenster wäre genau das, was auffiele.

  Ausgeliefert als Programmbündel für Apple Silicon, ad hoc signiert statt von
  einem eingetragenen Entwickler. Der Selbstaustausch bleibt auf macOS vorerst
  aus – eine neue Fassung wird von Hand geholt.

- **Hell oder dunkel, nach Wahl.** Die Anwendung folgt seit jeher dem
  Erscheinungsbild des Systems; das passt meistens und bleibt die Vorgabe. Unter
  „Erscheinungsbild" lässt sie sich jetzt stattdessen auf hell oder dunkel
  festlegen. Die Wahl wirkt schon beim Auswählen – Farbe ist die eine
  Einstellung, deren Wirkung sich nicht in einem Satz beschreiben lässt.
- **Beide Anmeldungen auf einen Blick.** Die Einstellungen sagen jetzt ganz
  oben, ob eine Claude-Code-Installation angemeldet ist und ob die eigene
  Anmeldung trägt – beides, immer, gleich welche gerade genutzt wird. Welcher
  Weg genommen wird, sagt nämlich nichts darüber, ob der andere funktionieren
  würde – und genau das ist die Frage, wenn keine Zahlen mehr ankommen.

### Behoben
- **Testfassungen jenseits der neunten wurden nicht angeboten.** Die Kennung
  hinter dem Bindestrich wurde als Text verglichen, damit stand „beta.10" unter
  „beta.9" – wer testete, bekam „ist aktuell" zu sehen. Beim Bau des Vergleichs
  als Gedankenspiel abgetan; die zehnte Testfassung eines einzigen Tages kam
  eine Woche später. Kennungen werden jetzt gezählt, Teil für Teil, wie es die
  semantische Versionierung vorsieht.
## [0.7.2] – 2026-08-20

### Behoben
- **Die Übersicht der Neuerungen blieb beim Verlassen einer Testfassung aus.**
  Was zuletzt lief, wurde mit drei Zahlen und ohne Kennung notiert – 0.7.1-beta.5
  und die fertige 0.7.1 hinterließen damit dieselbe Spur, der Schritt dazwischen
  war unsichtbar und die Übersicht kam nie. Die Kennung wird jetzt mit
  aufgeschrieben, und das Ankommen bei der fertigen Fassung zählt als Schritt
  nach vorn, obwohl die Nummer stehen bleibt. Zwischen zwei Testfassungen
  derselben Nummer bleibt es still: Der Changelog hat dort nichts Neues zu sagen.
- **Die Übersicht sagt jetzt, wenn eine Testfassung läuft.** Die Überschrift
  nennt die Version des Changelog-Eintrags, und der Changelog kennt keine
  Testfassungen – 0.7.2-beta.1 las sich daher als „Neu in Version 0.7.2", ohne
  dass irgendwo stand, dass die fertige Fassung noch gar nicht erreicht ist.
## [0.7.1] – 2026-08-20

### Hinzugefügt
- **Der Aktualisieren-Knopf sieht unterwegs nach einer neuen Fassung.** Ein
  Druck statt zwei. In den Einstellungen abwählbar, für wen das Netz nicht
  öfter angefasst werden soll als nötig.
- **Die Einstellungen stehen in zwei Spalten.** In einer Spalte wurde das
  Fenster hoch und schmal – auf einem Laptop-Bildschirm hieß das scrollen für
  Einstellungen, die bequem nebeneinander passen.

### Geändert
- Vorarbeit für Testfassungen. Davon ist im Alltag nichts zu sehen.

### Behoben
- **Das Menü im Infobereich wächst mit seinem Inhalt.** Es stand auf einer
  festen Breite, und die Zeile für das Zusatzkontingent passte nicht hinein:
  Sie trägt zwei Beträge und eine Währung und brach deshalb auf eine zweite
  Zeile um. In einem Menü, dessen übrige Zeilen je ein Limit sind, liest sich
  eine umgebrochene Zeile wie zwei.
- **Übersetzungskorrekturen.** Sechs Sprachen nannten das Zusatzkontingent
  noch „Credits", obwohl die Zahl Geld ist, und alle Sprachen behaupteten,
  ein fehlender Changelog erscheine auf Deutsch – er erscheint auf Englisch.
  Fehlermeldungen des Windows-Anmeldespeichers waren fest auf Deutsch; sie
  folgen jetzt der Oberflächensprache.

### Sicherheit
- Die Release-Seite aus der GitHub-Antwort wird an derselben Latte gemessen
  wie die Download-Adressen: nur https.
- Die Actions der Abläufe sind auf Commit-Hashes festgenagelt statt auf
  wandernde Marken. Eine Marke kann umhängen, wer das Repository der Action
  verwaltet, ein Hash nicht. Das zählt vor allem bei der fremden
  Release-Action, die mit Schreibrecht in dem Ablauf läuft, der die
  veröffentlichte Programmdatei baut.

## [0.7.0] – 2026-08-20

### Geändert
- **Das Menü im Infobereich zeichnet die Anwendung jetzt selbst.** Windows
  zeichnet ein Kontextmenü in der Systemschrift, mit haarfeinen Trennlinien
  und ohne eigenen Rahmen – neben den Fenstern dieser Anwendung sah das aus
  wie ein fremdes Programm. Jetzt trägt es denselben Rahmen, dieselbe Schrift
  und dieselben Abstände wie alles andere.

  Dafür musste das Symbol direkt bei Windows angemeldet werden. Avalonias
  Infobereich-Symbol bietet nur ein natives Menü, das sich aus dem Prozess
  heraus nicht gestalten lässt, und kein Rechtsklick-Ereignis, an das man ein
  eigenes Fenster hängen könnte. Was jetzt erscheint, ist ein gewöhnliches
  Fenster – und wird damit von denselben Tests gemessen und gezeichnet wie
  die übrigen, in allen neun Sprachen.
- **Das Menü nennt die Version.** Der Eintrag heißt jetzt „Über Claude
  UsageChecker 0.7.0 …“. Danach wird als Erstes gefragt, wer ein Problem
  meldet, und bisher stand sie nur in einem Fenster.

## [0.6.4] – 2026-08-20

### Behoben
- **Für ein Fenster, dessen Reset fällig war, gab es gar keinen richtigen
  Satz.** Der Baustein für eine Dauer wurde in eine Lücke gesetzt, die eine
  Dauer erwartet – heraus kam „Sitzung: 39 % – noch gleich". Die vier Stellen,
  die etwas über eine Restzeit sagen, haben für diesen Fall jetzt einen
  eigenen Satz: „Reset fällig", mit dem Zeitpunkt dahinter, wo er hilft.

  Zu sehen ist das nur zwischen dem Ablauf eines Fensters und dem nächsten
  Abruf – deshalb hat es seit den Anfangstagen überlebt, es hat schlicht nie
  jemand in dieser Minute hingesehen.

## [0.6.3] – 2026-08-20

### Behoben
- **Das Detailfenster saß unterhalb der Bildschirmmitte, sobald ein Update
  bereitstand.** Es wird einmal erzeugt und wiederverwendet, `CenterScreen`
  wirkte also nur beim allerersten Öffnen – die Update-Meldung trifft aber
  Sekunden später aus dem Netzabruf ein und macht das Fenster gut hundert
  Pixel höher. Ein Fenster, das sich nach seinem Inhalt bemisst, wächst nach
  unten, von einer Oberkante aus, die für die kleinere Höhe berechnet wurde –
  seine Mitte lag damit um die halbe Meldung zu tief. Es wird jetzt bei jeder
  Größenänderung neu zentriert.

## [0.6.2] – 2026-08-20

### Geändert
- **Das Symbol im Infobereich sagt, in welchem Zustand es ist.** Abgemeldet
  bleibt es schlicht grau. Angemeldet und alles im Rahmen, trägt es einen
  grünen Haken; ab der Warnschwelle ein gelbes Fragezeichen, ab der kritischen
  ein rotes Ausrufezeichen. Bisher tat das die Farbe allein – was niemandem
  hilft, der Gelb und Rot nicht auseinanderhält.

  Ein Zeichen je Zustand, nicht mehr: Bei sechzehn Pixeln – der üblichen Größe
  in der Taskleiste – ist das Abzeichen keine sieben Pixel breit, und zwei
  Zeichen nebeneinander sind ein Fleck statt einer Lesung. Das Anwendungssymbol
  selbst bleibt ohne Abzeichen; es meldet keinen Zustand.

### Behoben
- **Das Zusatzkontingent wurde hundertfach zu groß und in der falschen Einheit
  angezeigt.** Die API meldet `used_credits: 2276`, und das sind keine 2276
  Credits, sondern 22,76 € – ein Geldbetrag in der kleinsten Einheit seiner
  Währung. Die Anwendung nahm die Zahl für bare Münze und behauptete „2276,00
  von 5000,00 Credits". Es ist nichts fehlgeschlagen, es war schlicht falsch –
  und unsichtbar, solange niemand das Kontingent aktiviert hatte.

  Das neuere Feld `spend` sagt, was seine Zahlen bedeuten – Betrag, Währung und
  Exponent nebeneinander – und wird jetzt bevorzugt gelesen; `extra_usage`
  bleibt Rückfallebene und führt inzwischen selbst eine Währung. **Die Währung
  stammt vom Konto**: Wer in Dollar abrechnet, sieht USD, wer in Brasilien
  wohnt, BRL – und die Zahl der Nachkommastellen kommt mit, denn nicht jede
  Währung hat zwei. Der Betrag wird so geschrieben, wie die Oberflächensprache
  Zahlen schreibt.

## [0.6.1] – 2026-08-20

### Geändert
- Service-Release.

## [0.6.0] – 2026-08-20

### Behoben
- **Modellbezogene Wochenlimits fehlten in der Anzeige.** Wer ein Fable-Limit
  hat, sah es nirgends – weder im Tooltip noch im Kontextmenü noch in der
  Detailansicht –, obwohl Claude selbst es ausweist. Grund: Die Anwendung las
  die Felder `seven_day_opus` und `seven_day_sonnet`, die den Modellnamen im
  Bezeichner tragen. Beide sind inzwischen leer, und ein Feld
  `seven_day_fable` gibt es nicht.

  Die API liefert dieselben Werte zusätzlich als Liste `limits`, die das Modell
  im Inhalt benennt (`scope.model.display_name`). Diese Liste wird jetzt
  bevorzugt gelesen; die alten Felder bleiben als Rückfall. **Jedes künftige
  Modell erscheint dadurch von selbst**, ohne dass hier etwas geändert werden
  muss. Einzelheiten in [docs/api-research.md](docs/api-research.md).

  Das Symbol im Infobereich bezieht diese Limits ebenfalls ein – bisher wäre es
  grün geblieben, während ein Modellkontingent schon erschöpft war.

### Hinzugefügt
- **Neun Sprachen.** Deutsch, Englisch, Spanisch, Französisch, Italienisch,
  Portugiesisch (Brasilien und Portugal getrennt), Russisch und vereinfachtes
  Chinesisch. Beim ersten Start richtet sich die Anwendung nach der Sprache des
  Systems; ändern lässt sie sich im Einrichtungsfenster – dort wirkt die Wahl
  sofort und wird von **beiden** Schaltflächen übernommen – und später jederzeit
  in den Einstellungen.

  Mit der Sprache wechselt auch die Kultur für Zahlen, Datum und Uhrzeit: Wer
  die Oberfläche auf Französisch stellt, erwartet dort keine deutschen
  Datumsangaben.

  **Der Änderungsverlauf ist mitübersetzt.** Die Übersicht nach einer
  Aktualisierung erscheint also in derselben Sprache wie die Oberfläche.
  Englisch ist die Quelle und steht in [CHANGELOG.md](../../CHANGELOG.md); die
  Übersetzungen, darunter die deutsche, liegen unter [docs/changelog/](.).

  Nicht übersetzt werden Produkt- und Modellnamen: „Claude UsageChecker“,
  „Claude Code“ und der Name des Modells aus der API – „Fable“ heißt in jeder
  Sprache Fable.
- **Warn- und Kritischschwelle sind einstellbar.** Ab welcher Auslastung das
  Symbol im Infobereich gelb und ab welcher es rot wird, steht jetzt in den
  Einstellungen statt fest im Code (Vorgabe unverändert 75 % und 90 %). Eine
  Warnschwelle oberhalb der kritischen wird abgelehnt statt stillschweigend
  zurechtgerückt – sie fände nie statt.
- **Übersicht der Neuerungen nach einer Aktualisierung.** Beim ersten Start
  einer neuen Fassung zeigt die Anwendung, was sich seit der zuvor gelaufenen
  geändert hat. Übersprungene Zwischenfassungen kommen mit. Die Quelle ist der
  mitgelieferte Änderungsverlauf, kein Netzzugriff – die Übersicht steht auch
  ohne Verbindung bereit und zeigt zwangsläufig den Stand, der zur laufenden
  Fassung gehört. Beim allerersten Start entfällt sie.
- **„Über Claude UsageChecker“ im Kontextmenü.** Zeigt Symbol, Fassung, eine
  kurze Beschreibung und führt zur Projektseite. Von dort ist auch der
  vollständige Änderungsverlauf erreichbar.

### Geändert
- **Die Projektsprache ist Englisch.** Dokumentation, Kommentare, Bezeichner und
  Testnamen – alles im Repository außer den deutschen Oberflächentexten und der
  bisherigen Commit-Historie. Der Grund ist schlicht: Es ist ein öffentliches
  Repository, und wer es findet, soll es lesen können. Die deutsche
  Dokumentation wird unter [docs/de/](../de/) weitergeführt.
- Die zuletzt gelaufene Fassung wird in der Einstellungsdatei festgehalten
  (`lastRunVersion`). Sie ist die einzige Angabe, an der die Anwendung eine
  Aktualisierung erkennen kann – die ausführbare Datei selbst weiß nicht, was
  vor ihr lief.

  Ältere Fassungen kannten das Feld nicht. Wer von einer solchen aktualisiert,
  hat also nichts Gemerktes stehen – dann entscheidet das Vorhandensein der
  Einstellungsdatei: Sie belegt, dass die Anwendung schon lief, und es werden
  die Neuerungen der laufenden Fassung gezeigt. Ohne diesen Zweig zeigte
  ausgerechnet die Fassung, welche die Übersicht einführt, keine an.
- `MonitorOptions` trägt die Schwellen nicht mehr. Der Monitor hat sie nie
  gelesen – er beschafft Werte und beurteilt sie nicht. Beurteilt wird an genau
  einer Stelle, im `TrayIconSeverityResolver`, aus den Benutzereinstellungen.
  Zwei Orte für dieselbe Angabe wären eine Einladung, später am falschen zu
  drehen.
- Das errechnete `PollInterval` steht nicht mehr mit in der Einstellungsdatei.
  Gelesen wurde es dort nie; es sah nur aus wie eine zweite Angabe zum
  Abrufintervall, die der ersten widersprechen könnte.
- **Das Einstellungsfenster bleibt auf dem Bildschirm.** Es wächst mit seinem
  Inhalt und lässt sich nicht verkleinern; auf einem niedrigen Bildschirm ragte
  es unten heraus und nahm die Schaltfläche „Speichern“ mit. Jetzt sichern das
  zwei Dinge ab: Die Schaltflächenzeile sitzt unterhalb des Scrollbereichs und
  bleibt sichtbar, wie niedrig der Bildschirm auch ist, und das Fenster wird
  nach dem Aufbau gemessen und nach oben geschoben, falls es noch übersteht.
  Eine Höhenbegrenzung allein genügte nicht – Avalonia zentriert ein Fenster
  anhand der Höhe, die es beim Öffnen hat, und der Inhalt wächst danach.

### Entfernt
- **Die Eingabe eines Tokens von Hand** ist aus den Einstellungen verschwunden.
  Nutzen konnte sie niemandem: Das einzige Token zum Einfügen stammt aus
  `claude setup-token`, und dem fehlt der Geltungsbereich `user:profile`, den
  der Endpunkt verlangt. Die Token, die funktionieren – das der
  Claude-Code-Installation und das der eigenen Anmeldung – tippt niemand von
  Hand ein. Ein von einer früheren Fassung hinterlegtes Token wird weiterhin
  gelesen; nur der Weg, eines hinzuzufügen, ist fort. Begründung in
  [docs/api-research.md](../api-research.md).

### Dokumentation
- **Vorlagen für Fehlermeldungen und Wünsche** unter `.github/ISSUE_TEMPLATE/`,
  dazu eine Vorlage für Pull Requests und [CONTRIBUTING.md](CONTRIBUTING.md) –
  auf Englisch, damit auch außerhalb des deutschsprachigen Raums etwas gemeldet
  werden kann. Die Formulare fragen Fassung, Betriebssystem, Abonnement und
  Tokenquelle ab und warnen ausdrücklich davor, ein Token einzufügen.
- Die Recherche zur API ([docs/api-research.md](docs/api-research.md)) hält
  das neue Antwortformat fest – einschließlich der Felder, die ungenutzt
  bleiben, und warum.

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
