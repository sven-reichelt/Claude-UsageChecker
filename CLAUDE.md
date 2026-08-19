# Claude UsageChecker – Hinweise für Claude Code

Tray-Anwendung für Windows (macOS geplant), die Sitzungs- und Wochenlimit des
Claude-Abonnements im Infobereich anzeigt.

## Kommandos

```powershell
dotnet build                                      # gesamte Solution
dotnet test                                       # 32 Tests in Core.Tests
dotnet run --project src/ClaudeUsageChecker.App   # Anwendung starten
node build/generate-icons.mjs                     # Symbole neu erzeugen
```

Baut nach `artifacts/` (zentral über `ArtifactsPath` in `Directory.Build.props`),
nicht in projektlokale `bin/obj`-Ordner.

## Aufbau

* **`ClaudeUsageChecker.Core`** – plattformunabhängig, keine UI-Abhängigkeit.
  Hier gehören API-Zugriff, Tokenbeschaffung, Zustandslogik und Textaufbereitung
  hinein. Alles, was hier liegt, ist testbar und wird auch getestet.
* **`ClaudeUsageChecker.App`** – Avalonia. Composition Root ist `App.axaml.cs`;
  es gibt bewusst kein DI-Container-Framework und kein MVVM-Framework, um die
  Abhängigkeiten schlank zu halten.

## Regeln, die nicht verhandelbar sind

1. **Tokens nie erneuern, nie zurückschreiben.** Der Zugriff auf
   `.credentials.json` ist strikt lesend. Begründung in [SECURITY.md](SECURITY.md).
2. **Tokens nie protokollieren.** `AccessToken.ToString()` maskiert; ein Test
   sichert das ab.
3. **Abrufintervall nie unter 180 Sekunden.** Der Endpunkt drosselt sonst
   dauerhaft. `MonitorOptions.PollInterval` hebt kleinere Werte automatisch an.
4. **`User-Agent: claude-code/<version>` ist Pflicht** bei jedem Aufruf von
   `/api/oauth/usage`.
5. **Keine personenbezogenen Daten ins Repository.** Auch nicht in Testdaten,
   Screenshots oder Beispielausgaben.

## Konventionen

* `TreatWarningsAsErrors` ist aktiv. Analyzer-Ausnahmen werden in `.editorconfig`
  eingetragen und dort begründet – nicht per `#pragma` im Quelltext verstreut.
* Paketversionen zentral in `Directory.Packages.props`.
* Kommentare und Oberflächentexte auf Deutsch, Bezeichner auf Englisch.
* Testmethoden: `Methode_ErwartetesVerhalten`, Beschreibung auf Deutsch.

## Stand

Version 0.1 in Arbeit. Offen: Aktualisierungsprüfung aktivieren (hängt an der
Entscheidung, ob das Repository öffentlich wird), Installationspaket,
macOS-Menüleiste. Details in [CHANGELOG.md](CHANGELOG.md).
