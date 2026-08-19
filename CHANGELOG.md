# Changelog

The source of record. Translations live in [docs/changelog/](docs/changelog/);
if one of them disagrees with this file, this file is right.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
the versioning [Semantic Versioning](https://semver.org/).

## [Unreleased]

## [0.6.0] – 2026-08-20

### Fixed
- **Model-specific weekly limits were missing from the display.** Anyone with a
  Fable limit saw it nowhere – neither in the tooltip nor the context menu nor
  the details window – although Claude itself reports it. The cause: the
  application read the fields `seven_day_opus` and `seven_day_sonnet`, which
  carry the model name in the identifier. Both are empty now, and there is no
  field `seven_day_fable`.

  The API also reports the same values in a list called `limits`, which names
  the model in its content (`scope.model.display_name`). That list is now read
  in preference; the old fields remain as a fallback. **Every future model
  appears by itself**, without a change here. Details in
  [docs/api-research.md](docs/api-research.md).

  The tray icon takes these limits into account as well – previously it stayed
  green while a model quota was already exhausted.

### Added
- **Nine languages.** German, English, Spanish, French, Italian, Portuguese
  (Brazil and Portugal separately), Russian and Simplified Chinese. On first
  start the application follows the system language; it can be changed in the
  setup window – where the choice takes effect immediately and is applied by
  **both** buttons – and later at any time in the settings.

  The culture for numbers, dates and times changes along with the language:
  anyone switching the interface to French does not expect German dates there.

  **The changelog is translated too.** The summary shown after an update
  therefore appears in the same language as the interface. English is the source
  and lives in this file; the translations, German among them, are under
  [docs/changelog/](docs/changelog/).

  Product and model names are not translated: "Claude UsageChecker", "Claude
  Code" and the model name from the API – "Fable" is Fable in every language.

### Changed
- **The project language is English.** Documentation, comments, identifiers and
  test names – everything in the repository except the German interface strings
  and the commit history up to this point. The reasoning is plain: this is a
  public repository, and anyone who finds it should be able to read it. The
  German documentation is kept in parallel under [docs/de/](docs/de/).
- **The warning and critical thresholds are configurable.** The usage level at
  which the tray icon turns yellow, and the one at which it turns red, now live
  in the settings instead of the code (defaults unchanged at 75 % and 90 %). A
  warning threshold above the critical one is rejected rather than quietly
  corrected – it would never take effect.
- **A summary of what changed after an update.** On the first start of a new
  version, the application shows what has changed since the version that ran
  before. Skipped intermediate versions are included. The source is the bundled
  changelog, not a network request – the summary is therefore available offline
  and necessarily shows the state belonging to the running version. It is
  skipped on the very first start.
- **"About Claude UsageChecker" in the context menu.** Shows the icon, the
  version, a short description and leads to the project page. The full
  changelog is reachable from there too.

### Changed
- The version that ran last is recorded in the settings file
  (`lastRunVersion`). It is the only thing by which the application can
  recognise an update – the executable itself does not know what ran before it.

  Older versions did not know that field. Anyone updating from one of them has
  nothing recorded – in that case the presence of the settings file decides: it
  proves the application has run before, and the changes of the running version
  are shown. Without that branch, the very version introducing the summary
  would show none.
- `MonitorOptions` no longer carries the thresholds. The monitor never read
  them – it fetches values, it does not judge them. Judging happens in exactly
  one place, in `TrayIconSeverityResolver`, from the user settings. Two places
  for the same setting would be an invitation to later turn the wrong one.
- The computed `PollInterval` is no longer written to the settings file. It was
  never read from there; it merely looked like a second statement about the
  polling interval that could contradict the first.
- **The settings window stays on the screen.** It grows with its content and
  cannot be resized; on a low screen it reached past the bottom edge and took
  the "Save" button with it. Two safeguards now: the button row is docked below
  the scroll area and stays visible however low the screen, and the window is
  measured once laid out and moved back up if it still overhangs. Capping the
  height alone was not enough - Avalonia centres a window on the height it has
  at the moment it opens, and the content grows afterwards.

### Removed
- **Entering a token by hand** is gone from the settings. It could be of use to
  nobody: the only token to paste in comes from `claude setup-token`, and that
  one lacks the `user:profile` scope the usage endpoint requires. The tokens
  that do work - the one belonging to the Claude Code installation and the one
  from the application's own sign-in - are never typed in by hand. A token
  stored by an earlier version keeps being read; only the way to add one is
  gone. Reasoning in [docs/api-research.md](docs/api-research.md).

### Documentation
- **Templates for bug reports and feature requests** under
  `.github/ISSUE_TEMPLATE/`, plus a pull request template and
  [CONTRIBUTING.md](CONTRIBUTING.md) – in English, so that reports can
  come from outside the German-speaking world. The forms ask for version,
  operating system, subscription and token source, and explicitly warn against
  pasting a token.
- The API research ([docs/api-research.md](docs/api-research.md)) records the
  new response format – including the fields that remain unused, and why.

## [0.5.0] – 2026-08-19

### Changed
- The installation target is now `%LOCALAPPDATA%\Programs\ClaudeUsageChecker`
  instead of `%USERPROFILE%\ClaudeUsageChecker`. That is the location Windows
  intends for applications without administrator rights – VS Code and Signal
  live there too. It leaves the root of the user profile clear, where nobody
  expects programs next to documents and downloads.

  **Already installed copies do not move by themselves.** They keep running
  from the old location. To move: open the settings and save – with the
  autostart box ticked, the application is copied to the new location. The old
  directory can then be deleted by hand.

## [0.4.2] – 2026-08-19

### Fixed
- Anyone who skipped the setup on first start and later only ticked "Start with
  Windows" got an autostart entry pointing at the downloads folder – worthless
  as soon as that folder was cleaned out. The tick now triggers the move as
  well, with a prior notice about the target path and the restart.
- **Unticking** it, by contrast, leaves the application where it is. Only the
  autostart entry is removed; once installed stays installed.

## [0.4.1] – 2026-08-19

### Fixed
- The extraction folders of earlier versions were left behind in the temporary
  directory. A compressed single file cannot load its native libraries from the
  bundle – the .NET runtime extracts them to
  `%TEMP%\.net\ClaudeUsageChecker\<id>`, and since the id depends on the
  content, every version got its own folder. Around 16 MB per update,
  accumulating without limit. The application now clears them itself.

### Documentation
- [SECURITY.md](SECURITY.md) lists in full what the application stores
  where, and what would remain after an uninstall.

## [0.4.0] – 2026-08-19

### Added
- **Permanent installation.** If the application runs outside its target
  location, it offers once on first start to copy itself to
  `%USERPROFILE%\ClaudeUsageChecker`, set up autostart and restart from there.
  The reason is not tidiness: autostart, the pinned tray icon and the
  self-update all depend on the path of the executable – if it sits in the
  downloads folder, all three break as soon as that folder is cleaned out.
- Autostart is enabled together with the installation and points at the target
  path, not at the starting location. Can be switched off in the settings.

### Changed
- The details window appears centred on the screen and carries a thin border in
  the colour of the icon instead of the system frame.

### Added
- A test checks that the border actually gets its colour. An unresolvable
  `DynamicResource` would otherwise stay silently empty.

## [0.3.3] – 2026-08-19

### Changed
- The published file has the same name in every version:
  `ClaudeUsageChecker.exe` instead of `ClaudeUsageChecker-0.3.2-win-x64.exe`.
  The self-update writes the new version to the path of the running file – a
  versioned name would afterwards claim the wrong version. And Windows
  remembers the tray pinning per path: if the name did not stay the same, the
  icon would land in the overflow area after every update.

## [0.3.2] – 2026-08-19

### Fixed
- The buttons of the update notice extended past the window. Side by side they
  needed about 420 pixels, the window is 380 wide – "Open release page" was
  only half readable. They are now stacked.

### Added
- Tests that expose overflow in the details window. They measure the actual
  placement after a full layout pass and compare the right edge of every
  element with the window width. Neither the desired size of the controls nor
  that of the window is any use for this: Avalonia clamps both to the specified
  value, so an overflow cannot appear there at all.

## [0.3.1] – 2026-08-19

### Changed
- The interface writes umlauts as umlauts. Previously it said "Auf
  Aktualisierungen pruefen", "Gueltig bis" or "Der Browser liess sich nicht
  oeffnen" – the transliterations came from development and had no business
  being on screen. 36 strings affected.
- The message about missing access rights now points to the settings in the
  place where it previously demanded a token.

### Added
- A test checks the character encoding from the source file through to the
  interface. An encoding error now surfaces in the test run instead of at the
  user.

## [0.3.0] – 2026-08-19

The first version that can update itself. From here on a single click is
enough – downloading by hand is no longer necessary.

### Fixed
- Versions are shown with three components. The fourth comes from the assembly
  version and says nothing – "Version 0.2.0.0 is up to date" was merely
  confusing.

### Added
- **Update at the push of a button.** "Install now and restart" downloads the
  new version, verifies its SHA-256 checksum against the published one,
  replaces the running file and restarts. A notice that has to be acted on by
  hand tends to be left lying around.
  - If the checksum does not match, or is missing, nothing is installed and
    nothing is executed.
  - The address comes from the GitHub response for this repository; addresses
    without HTTPS are discarded.
  - Only after an explicit click, never silently in the background.
  - The replacement relies on Windows allowing a running file to be renamed. If
    putting the new file in place fails, the rename is undone.

### Changed
- "Show details" has been removed from the context menu. A left click on the
  icon opens the details window, and the figures are in the status lines above
  it anyway – the entry merely offered the same route a second time.
- The message about missing access rights names your own sign-in first.
  Previously it said "Sign in to Claude Code" – advice nobody could follow on a
  machine without Claude Code.

## [0.2.0] – 2026-08-19

First release. Self-contained single file for Windows x64, 21 MB, no .NET
runtime required.

### Display

- 5-hour session limit and weekly limits (total, Opus, Sonnet) from
  `GET /api/oauth/usage` – authoritative values, not estimates.
- Tooltip with usage, reset time and remaining time. If the reset falls on
  another day, the weekday precedes it; from a week away, the date – a bare
  time of day would be ambiguous for the weekly limit.
- Context menu with **all** reported limits.
- Details window with progress bars, reset times, extra usage (`extra_usage`)
  and the token source actually used.
- Colour-coded tray icon: normal, strained, critical.

### Sign-in

- **Own sign-in via OAuth with PKCE** (RFC 7636, S256) – makes the application
  independent of a running Claude Code installation. The only scope requested
  is `user:profile`; explicitly **not** `user:inference` and **not**
  `org:create_api_key`.
- Without a local web server: the code is pasted by hand rather than received
  through a redirect to `localhost`. No open port.
- The application's own token is refreshed automatically. For the token read
  from Claude Code this is deliberately omitted – a rotating refresh token
  would invalidate its sign-in. Separate entries in the secret store.
- If your own sign-in expires, it is removed and reported rather than quietly
  falling back to Claude Code. A mere disturbance (network, 5xx, throttling)
  leaves it untouched.
- Fallback chain: own sign-in → stored token → environment variable → Claude
  Code. If the API rejects one source, the request moves on to the next.

### Operation

- Polling interval at least 180 seconds, exponential backoff after failures,
  the server's `Retry-After` takes precedence.
- Only one instance per login session.
- Autostart with Windows, can be switched off.
- Update check via GitHub releases. Nothing is downloaded or executed – only
  reported, and the release page opened on request.
- Errors in tray actions no longer terminate the application but end up with
  context in `crash.log`.

### Findings that shaped the design

- **`claude setup-token` is not suitable for this purpose.** Such tokens are
  valid and work against `/v1/messages`, but do not carry `user:profile`. The
  usage endpoint rejects them with HTTP 403. That was the project's original
  assumption, and it is refuted.
- **The token endpoint lives on `platform.claude.com`**, no longer on
  `console.anthropic.com` – there it answers HTTP 404.
- **The `User-Agent` is mandatory.** Without a Claude Code user agent the usage
  endpoint throttles permanently with HTTP 429.
- Built trimmed and compressed: 21 MB instead of 93 MB, startup in 2.3 instead
  of 7.2 seconds, 87 instead of 136 MB of memory. Trimming wins on all three
  axes – code that has been removed does not need to be loaded and compiled
  either.

### Known limitations

- The package is **not signed**. Windows SmartScreen reports an unknown
  publisher on first start.
- How long your own sign-in survives a longer break is unknown – Anthropic does
  not document the lifetime of the refresh token.
- The sign-in flow uses the publicly known OAuth client ID of Claude Code,
  since Anthropic offers no registration for third-party applications. Not an
  officially supported route; it may change at any time.
- macOS is prepared for but not implemented.
