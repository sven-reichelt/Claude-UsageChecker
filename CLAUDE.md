# Claude UsageChecker â€“ notes for Claude Code

Tray application for Windows (macOS planned) that shows the session and weekly
limits of a Claude subscription in the notification area.

## Language

**Talk to the user in German. Write everything in the repository in English.**

That is deliberate: the repository is public, so anyone who finds it should be
able to read it - but the conversation stays in the language its author thinks
in. The one exception is the interface strings, which exist in nine languages;
English is the source there as well.

The commit messages up to August 2026 are in German and stay that way.

The history itself was rewritten once, on 2026-08-20, to carry the licence
retroactively. Every hash changed with it and the tags were rebuilt. That was a
deliberate one-off while nobody had forked the repository; it is not a habit,
and a second one would break every clone that exists by then.

## Commands

```powershell
dotnet build                                      # the whole solution
dotnet test                                       # 566 tests (Core.Tests + App.Tests)
dotnet run --project src/ClaudeUsageChecker.App   # run the application
node build/generate-icons.mjs                     # regenerate the icons
```

Builds into `artifacts/` (centrally through `ArtifactsPath` in
`Directory.Build.props`), not into per-project `bin/obj` folders.

## Layout

* **`ClaudeUsageChecker.Core`** â€“ platform independent, no UI dependency. API
  access, token retrieval, state logic and text formatting belong here.
  Everything in it is testable, and everything in it is tested.
* **`ClaudeUsageChecker.App`** â€“ Avalonia. The composition root is
  `App.axaml.cs`; there is deliberately no DI container and no MVVM framework,
  to keep the dependency list short.

## Rules that are not up for discussion

1. **Never refresh a foreign token, never write one back.** Access to
   `.credentials.json` is strictly read-only. Reasoning in
   [SECURITY.md](SECURITY.md).
2. **Never log tokens.** `AccessToken.ToString()` masks; a test enforces it.
3. **Never poll faster than 180 seconds.** The endpoint throttles permanently
   otherwise. `MonitorOptions.PollInterval` raises smaller values by itself.
4. **`User-Agent: claude-code/<version>` is mandatory** on every call to
   `/api/oauth/usage`.
5. **No personal data in the repository.** Not in test data, screenshots or
   sample output either.

## Conventions

* `TreatWarningsAsErrors` is on. Analyzer exceptions are recorded in
  `.editorconfig` with their reasoning - not scattered as `#pragma` through the
  source.
* Package versions centrally in `Directory.Packages.props`.
* **Interface text never belongs in the source.** It lives in
  `src/ClaudeUsageChecker.Core/Localization/Texts/<language>.json` and is fetched
  through `T.Name`; English is the source language. XAML therefore carries no
  `Text=` or `Content=` attributes any more - each window sets its labels in its
  `ApplyTexts()` method, so that a language change can refresh them.
  `LanguageFileTests` and `LabellingTests` catch what gets forgotten.
* Test methods: `Method_ExpectedBehaviour`.

## Status

Version 0.7.1 released, the repository is public and written in English.
Finished among other things: the application's own sign-in through OAuth with
PKCE including refresh, update at the push of a button with checksum
verification, permanent setup with autostart, configurable thresholds, the
summary of changes after an update, the about window, model-specific weekly
limits read from the `limits` list, and nine languages.

Open: **the macOS menu bar** - the only larger item. The core is platform
independent and `MacOsKeychainCredentialReader` exists; what is missing is the
connection to the menu bar, a counterpart to `WindowsCredentialStore`, and
equivalents for autostart and self-installation. It is also unverified how long
the sign-in survives a longer break, and whether the figures on the Pro
subscription look the way the README describes. Details in
[CHANGELOG.md](CHANGELOG.md).

## Pitfalls that have bitten before

**Never write your own `InitializeComponent()` in a window's code-behind.**
Avalonia generates a version `InitializeComponent(bool loadXaml = true, â€¦)` which
writes the controls named with `x:Name` into their fields after loading. A
hand-written parameterless variant wins overload resolution, loads only the XAML
and leaves every field null - the constructor then fails with a
`NullReferenceException`. That compiles without error.
`WindowConstructionTests` catches it.

**Failures in tray actions otherwise end the application.** Without a window an
exception travels all the way to the message loop and the process disappears
without a word. New handlers therefore always go through `ErrorGuard.Run` or
`ErrorGuard.Forget`.

**The Windows tooltip is truncated hard at 127 characters.** It therefore shows
only session and weekly limit; every further limit lives in the context menu.
Whoever extends the tooltip texts checks `ToTooltip_StaysWithinTheWindowsLimit`
along with it - the test deliberately assumes the worst case.

**Keep foreign and own credentials strictly apart.** The token of Claude Code is
only ever read and never refreshed (rotating refresh tokens would invalidate its
sign-in). The application's own OAuth token, by contrast, it manages fully
including refresh. Separate entries in the secret store, never mixed.

**The OAuth flow stays at `user:profile`.** The application needs nothing more,
and asking for more would mean claiming rights over an account it never needed.
`TheRequestedScopeStaysAtTheNecessaryMinimum` enforces it.

**One new interface string means nine files.** English is the source;
`LanguageFileTests` reports every key missing from one of the eight
translations, and additionally checks that the placeholders (`{0}`, `{1}`) are
the same in every language. A `{2}` in a text that only receives two values
otherwise throws a `FormatException` - and only once somebody using that
language opens that particular window.

**The same applies to the changelog.** An entry in `CHANGELOG.md` has to make it
into the eight versions under `docs/changelog/`. The test checks that all of them
know the same versions - a missing entry under *Unreleased* escapes it, though.

**`Assembly.GetName().Version` always has four parts, the changelog three.**
`Version` counts a missing part as âˆ’1, so `0.6.0` counts as **smaller** than
`0.6.0.0`. Comparing the two unguarded makes the application consider the
running version out of date - the summary of changes would come back on every
start. Use `ReleaseHistory.ThreePart` before every comparison;
`ReleaseHistoryTests` pins it down.

**The CI runs in English, this machine in German.** Anything a test compares
against a formatted date, time or number therefore behaves differently in the
two places. `ToTooltip_ContainsTheResetTime` built its expectation with the
invariant culture while the code formats with the current one - "07:14" in both
on a German machine, "7:14 AM" on the runner. Green here, red on the first
push. Since a language change now sets the culture of the process, this kind of
fault has become easy to write: never compare against a formatted literal
without fixing the culture, and derive times rather than writing them out - the
time zone differs between the two as well.

**Restoring a file from a backup does not trigger a rebuild.** `Copy-Item` and
`cp` carry the source's timestamp along, so a restored file looks older than the
build output and MSBuild leaves it alone - the next run still uses what was
there before. It struck twice in one session: once with a language file, and
once more expensively with a counter-check whose deliberately broken code stayed
in the build and was then measured as if it were the fix. Touch the file
afterwards, or build with `--no-incremental`.

**Editing language files from the shell mangles the encoding.** `perl -i -CSD`
decodes the file as UTF-8 but leaves the replacement text from the command line
as raw bytes - and writes them out encoded a second time. Out of "PrÃ¼fsumme"
comes "PrÃƒÂ¼fsumme". It struck 93 lines across eight languages, and nothing
noticed: English is pure ASCII and stayed clean, so every test was green and the
damage would have shown only to whoever ran the application in German. Use
`-CSDA` (the `A` decodes the arguments) or a dedicated tool.
`NoTextIsDoublyEncoded` catches it now.

**Tests that press "save" in the settings window need an injected autostart
switch.** The real route writes to the Run key of the registry - a test without
`applyAutostart` deletes the autostart entry of the user on whose machine it
happens to run.

**The self-update is the most delicate path in the program.** It downloads an
executable from the network and starts it. Three conditions secure that - a
verified SHA-256 sum, an address from GitHub's response, an explicit click.
Changing anything there loosens the only safeguard that exists. Reasoning in
[SECURITY.md](SECURITY.md), tests in `UpdateInstallerTests`.
