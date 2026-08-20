# Claude UsageChecker

[![Latest release](https://img.shields.io/github/v/release/sven-reichelt/Claude-UsageChecker?label=release)](https://github.com/sven-reichelt/Claude-UsageChecker/releases/latest)
[![Pre-release](https://img.shields.io/github/v/release/sven-reichelt/Claude-UsageChecker?include_prereleases&label=pre-release)](https://github.com/sven-reichelt/Claude-UsageChecker/releases)

*Deutsche Fassung: [docs/de/README.md](docs/de/README.md)*

Shows the session and weekly limits of a Claude subscription permanently in the
Windows notification area - independent of a running Claude Code session. A
pointer on the icon is enough: session and weekly limit appear with their usage,
reset time and remaining time in the tooltip. The context menu lists **every**
reported limit, and a click opens the details window with progress bars.

With its own sign-in it runs independently of Claude Code - confirmed on a
machine without a Claude Code installation. macOS (menu bar) is prepared for but
not yet implemented - see the [roadmap](#roadmap).

## Features

| Area | Status |
| --- | --- |
| Five-hour session limit with remaining time | ✅ |
| Weekly limit, total and per model (name from the API) | ✅ |
| Colour-coded tray icon (normal / strained / critical) | ✅ |
| Details window with progress bars and reset times | ✅ |
| Extra usage, where enabled on the subscription | ✅ |
| Every limit in the context menu | ✅ |
| Configurable thresholds for yellow and red | ✅ |
| Nine languages, changelog included | ✅ |
| Summary of changes after an update | ✅ |
| Token encrypted in the Windows Credential Manager | ✅ |
| Permanent setup with autostart, on request | ✅ |
| Only one instance per login session | ✅ |
| Own sign-in through OAuth with PKCE - independent of Claude Code | ✅ |
| Automatic refresh of the application's own token | ✅ |
| Update at the push of a button, with checksum verification | ✅ |
| macOS menu bar | 🚧 planned |

## Data source

The application queries the OAuth usage endpoint of the Anthropic API - the same
source `/usage` in Claude Code takes its figures from:

```http
GET https://api.anthropic.com/api/oauth/usage
Authorization: Bearer <oauth_access_token>
anthropic-beta: oauth-2025-04-20
User-Agent:     claude-code/<version>
```

Response format (abridged):

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

What counts is the `limits` list: only it names the limited model in its content.
The older individual fields (`seven_day_opus`, `seven_day_sonnet`) carry the name
in their identifier and stay empty as soon as a different model is limited - they
serve only as a fallback now.

Two peculiarities of the endpoint shape the design:

1. **The `User-Agent` is mandatory.** Without a Claude Code user agent the
   service answers HTTP 429 permanently.
2. **It throttles sharply.** The polling interval is therefore at least 180
   seconds and cannot be set below that.

Details and the alternatives that were discarded are in
[`docs/api-research.md`](docs/api-research.md).

### Differences between Pro and Max

There is no plan detection. Every window the API reports as `null` is simply
left out - the display follows nothing but what comes back.

| Window | Pro | Max |
| --- | --- | --- |
| Session (`kind: session`) | yes | yes |
| Weekly total (`kind: weekly_all`) | yes | yes |
| Weekly limit per model (`kind: weekly_scoped`) | depending on use | depending on use |
| Extra usage (`extra_usage`) | probably not | when enabled |

Observation from practice: the model-specific weekly windows only appear once
that model has been used during the current week. The Pro column is researched,
not measured.

**Model names are not hard-wired.** The application reads them from the response
(`scope.model.display_name`) and labels the row with them - "Weekly Fable" today.
If Anthropic changes the limited model, the new one appears without a change to
the application. The earlier version read fields with the model name in the
identifier (`seven_day_opus`, `seven_day_sonnet`); when the weekly limit moved to
Fable, it was therefore missing from the display entirely.

## Languages

The interface is available in nine languages:

| | | |
| --- | --- | --- |
| English | Deutsch | Español |
| Français | Italiano | Português (Brasil) |
| Português (Portugal) | Русский | 简体中文 |

On the first start the application follows the language of the system. It can be
changed in the setup window - where the choice takes effect at once and is
adopted by **both** buttons - and later at any time under **Settings →
Language**.

The culture for numbers, dates and times changes along with the language:
someone switching the interface to French does not expect German dates there.

**The changelog is translated too.** The summary of changes shown after an update
therefore appears in the same language as the interface. English is the source
and lives in [CHANGELOG.md](CHANGELOG.md); the translations are under
[docs/changelog/](docs/changelog/). Where one is missing, the window shows the
English version and says so.

Product and model names are not translated: "Claude UsageChecker", "Claude Code"
and the model name from the API - "Fable" is Fable in every language.

The texts live as JSON per language in
`src/ClaudeUsageChecker.Core/Localization/Texts/` - deliberately not satellite
assemblies from `.resx`, because the release is a trimmed single file. To add a
language, create a file and register it in `Language.All`; a test then reports
every key still missing.

## Setup

### Requirements

* Windows 10/11
* [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) to build
* An active Claude subscription (Pro or Max)

### Build and run

```powershell
git clone https://github.com/sven-reichelt/Claude-UsageChecker.git
cd Claude-UsageChecker
dotnet build
dotnet run --project src/ClaudeUsageChecker.App
```

### Signing in (recommended)

**Settings → Sign in …** starts an OAuth flow of its own with PKCE. That gives
the application its own access rights, so it no longer needs a running Claude
Code installation:

1. Click **Open sign-in page in browser** - the approval happens on claude.ai.
2. Paste the code shown there into the field and click **Complete sign-in**.

The only scope requested is `user:profile` - the right to read the usage status.
Explicitly **not** the right to make requests on behalf of the account or to
create API keys. The token is stored encrypted and keeps itself alive; signing in
again is not necessary.

Deliberately without a local web server: the code is pasted by hand rather than
received through a redirect to `localhost`. That way the application opens no
port.

The access token is valid for about eight hours. The application refreshes it
five minutes before expiry through the refresh token, which rotates in the
process - signing in again is not needed while it keeps running.

**How long the sign-in survives a break is unknown.** Anthropic does not document
the lifetime of the refresh token and so far does not include it in the response.
The application evaluates the field `refresh_token_expires_in` in case it ever
arrives.

Should the sign-in have expired, it is removed and the details window says so.
The display then carries on - where available - through the token of Claude Code;
there is no silent fallback without notice. A mere disturbance (network, server
error, throttling) leaves the sign-in untouched.

### Without signing in

The application also works without a sign-in of its own, as long as Claude Code
is signed in. The sources are tried in order:

| Order | Source | Note |
| --- | --- | --- |
| 1 | Own sign-in (`ClaudeUsageChecker:OAuth`) | recommended, refreshes itself |
| 2 | Manually stored token | special case, has to carry `user:profile` |
| 3 | Environment variable `CLAUDE_CODE_OAUTH_TOKEN` | mainly for development |
| 4 | `%USERPROFILE%\.claude\.credentials.json` | token of Claude Code |

If the API rejects a token, the application moves on to the next source. An
unusable source therefore does not paralyse it.

> **`claude setup-token` does not work here.**
> Such tokens (`sk-ant-oat01-…`) are valid and work perfectly against
> `/v1/messages`, but do not carry the `user:profile` scope. The usage endpoint
> rejects them with HTTP 403:
> `OAuth token does not meet scope requirement user:profile`.
> The settings therefore check an entered token before storing it and turn it
> down with that reason. Tested on 2026-08-19.

Source 4 is **only read**. The application never refreshes that token and writes
nothing back into the credentials of Claude Code. Its own token, by contrast, it
manages fully - see [SECURITY.md](SECURITY.md).

> **A note on the client id.** The sign-in flow uses the publicly known OAuth
> client id of Claude Code, since Anthropic offers no registration for
> third-party applications. Only your own account data is retrieved, with your
> own approval, but the application identifies itself to the authorization server
> as Claude Code. This is not an officially supported route and may change at any
> time.

## Project layout

```
Claude-UsageChecker/
├── src/
│   ├── ClaudeUsageChecker.Core/     Platform-independent logic
│   │   ├── Api/                     HTTP client for /api/oauth/usage
│   │   ├── Authentication/          Token sources, with OAuth/ for the own flow
│   │   ├── Configuration/           Options and JSON context
│   │   ├── Formatting/              Tooltip and detail text
│   │   ├── Localization/            Language files and text access
│   │   ├── Models/                  Domain model and API DTOs
│   │   ├── Platform/                Secret store, credential reader
│   │   ├── Release/                 Changelog parser
│   │   └── Services/                Polling loop and state model
│   └── ClaudeUsageChecker.App/      Avalonia interface
│       ├── Services/                Updates, autostart
│       ├── Settings/                User settings
│       ├── Tray/                    Tray icon and menu
│       └── Views/                   Details, settings, sign-in and about windows
├── tests/
│   ├── ClaudeUsageChecker.Core.Tests/   Logic, formatting, token chain
│   └── ClaudeUsageChecker.App.Tests/    Headless UI tests (Avalonia.Headless)
├── build/                           Tooling (icon generator)
├── assets/icons/                    Generated icons
└── docs/                            Research, changelog translations, German docs
```

Icons are generated from code rather than committed as binaries:

```powershell
node build/generate-icons.mjs
```

## Releasing

Pushing a tag is enough:

```powershell
git tag v0.2.0
git push origin v0.2.0
```

The workflow `.github/workflows/release.yml` tests, builds a self-contained
single file for Windows x64, checks its size and whether it starts, computes the
SHA-256 sum and creates a **draft** release. Only publishing it by hand makes it
visible to the update check - that way nothing goes out unchecked.

The package is trimmed and compressed. Measured against the unmodified build:

| Variant | Size | Startup | Memory |
| --- | --- | --- | --- |
| unmodified | 93 MB | – | – |
| compressed only | 45 MB | 7.2 s | 136 MB |
| **trimmed + compressed** | **21 MB** | **2.3 s** | **87 MB** |

Trimming wins on all three axes - code that has been removed does not need to be
loaded and compiled either. The settings live in the project file, so a local
`dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true`
produces the same result.

The package is not signed - deliberately, because this is a hobby project.
Windows SmartScreen therefore reports an unknown publisher on the first start;
confirm through **More info → Run anyway**.

## Updates

The check runs against the GitHub releases of this repository
(`GitHubReleaseUpdateService`, behind the interchangeable interface
`IUpdateService`). It happens at startup - where enabled in the settings - and at
any time through **Check for updates …** in the context menu.

The result appears in the details window. For a newer version two buttons show
there:

* **Install now and restart** – downloads the new version, verifies its SHA-256
  sum against the published one, replaces the running file and restarts. One
  click, no manual download.
* **Open release page** – for anyone who prefers to look for themselves.

If the checksum does not match, or is missing, nothing is installed and nothing
is executed. Installing happens only after an explicit click, never silently in
the background. The details and the limits of that safeguard are in
[SECURITY.md](SECURITY.md).

The self-update requires the published single file. In a development build dozens
of files sit side by side - the button is not even offered there.

**The file is named the same in every release: `ClaudeUsageChecker.exe`.** Two
reasons. The self-update writes the new version to the path of the running file -
a versioned name would afterwards claim the wrong version. And Windows remembers
the tray pinning per path: if the name did not stay the same, the icon would land
in the overflow area after every update.

As long as no release exists, the check says so openly rather than staying quiet.

### What's new?

After an update the application shows on the first start what has changed since
the version that ran before - across several skipped versions as well. The source
is the bundled [changelog](CHANGELOG.md), which sits as a resource inside the
program. That makes the summary available without network access, and it
necessarily shows the state belonging to the running version.

Where it comes from, the application knows only from the settings file: under
`lastRunVersion` it records which version ran last. On the very first start the
summary is skipped - someone just getting to know the application needs no list
of what they have never seen.

The complete changelog is reachable at any time through **About Claude
UsageChecker …** in the context menu. Version and project page are there too.

## Icon colour

The icon follows whichever limit is tightest: session, weekly total and the
model-specific weekly limits. The usage level at which it turns yellow, and the
one at which it turns red, live in the settings - preset to 75 % and 90 %. The
warning threshold has to be below the critical one; otherwise it would never take
effect, and the window says so instead of quietly correcting the input.

## Roadmap

| Version | Content |
| --- | --- |
| 0.1 | Windows tray, session and weekly limit, token management ✅ |
| 0.2 | Own OAuth sign-in, first release as a single file ✅ |
| 0.3 | Update at the push of a button with checksum verification ✅ |
| 0.4 | Permanent setup with autostart ✅ |
| 0.5 | Installed into %LOCALAPPDATA%\Programs, where Windows wants it ✅ |
| 0.6 | Nine languages, model-specific limits, configurable thresholds, summary of changes after an update ✅ |
| 0.7 | Its own menu in the notification area, in the style of the windows ✅ |
| 0.8 | macOS menu bar ✅ |
| 0.9 | Self-replacement on macOS, and a signed bundle |

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md). In short: the repository is in English,
the interface strings exist in nine languages, and comments explain **why**
rather than what.

## License

[GNU General Public License v3.0 or later](LICENSE).

Free software: use it, study it, pass it on, change it. Whoever passes on a
changed version has to pass on its source under the same terms - that is the
whole point of the choice.
