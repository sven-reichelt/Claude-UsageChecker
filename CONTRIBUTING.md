# Contributing

Claude UsageChecker is a hobby project with a narrow purpose: show the usage
limits of a Claude subscription in the Windows system tray, read-only. Reports
and contributions are welcome within that scope.

## Reporting a problem

Use the [issue forms](../../issues/new/choose). They ask for the version, your
operating system and how the application authenticates, because those three
decide most cases.

> **Never paste an access token.** Tokens look like `sk-ant-oat01-â€¦` and grant
> full access to a Claude subscription. `%USERPROFILE%\.claude\.credentials.json`
> holds one â€” do not attach it. `crash.log` and `settings.json` contain no
> tokens and are safe to share.

Found a **security vulnerability**? Do not open a public issue â€” use
[Security Advisories](https://github.com/sven-reichelt/Claude-UsageChecker/security/advisories/new).
The reasoning is in [SECURITY.md](SECURITY.md).

## Language conventions

This trips people up, so it comes first:

| What | Language |
| --- | --- |
| Everything in the repository | **English** |
| Interface strings | **English** is the source; eight translations ship with it |
| Test method names | `Method_ExpectedBehaviour` |
| `docs/de/` and `docs/changelog/de.md` | **German**, kept in parallel |

The repository is public, so anyone who finds it should be able to read it. The
German documentation under `docs/de/` is maintained alongside the English source;
where the two disagree, the English one counts.

The commit messages up to August 2026 are in German and stay that way.

The history itself was rewritten once, on 2026-08-20, to carry the licence
retroactively. Every hash changed with it and the tags were rebuilt. That was a
deliberate one-off while nobody had forked the repository; it is not a habit,
and a second one would break every clone that exists by then.

### Adding or changing interface text

Interface text never belongs in the source. It lives in
`src/ClaudeUsageChecker.Core/Localization/Texts/<language>.json` and is reached
through a named property on `T`. XAML files therefore carry no `Text=` or
`Content=` attributes â€” each window sets its labels in an `ApplyTexts()` method,
so that a language change can refresh them.

Adding one string means touching nine files: English first, then the eight
translations. `LanguageFileTests` reports every key still missing, and checks
that the placeholders (`{0}`, `{1}`) match across languages â€” a `{2}` in a text
that only receives two values would otherwise throw at runtime, and only for
whoever happens to use that language.

The same applies to `CHANGELOG.md`: an entry there needs its counterpart in the
eight files under `docs/changelog/`, because the application shows the changelog
in the interface language.

If you do not speak a language, say so in the pull request and leave that file
alone â€” a wrong translation is worse than a missing one, which at least falls
back to English visibly.

## Building

```powershell
dotnet build                                      # whole solution
dotnet test                                       # 545 tests
dotnet run --project src/ClaudeUsageChecker.App   # run it
```

Output goes to `artifacts/`, not to per-project `bin/obj` folders.

Requires the [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) and
Windows. macOS is prepared for but not implemented.

## Project layout

* **`ClaudeUsageChecker.Core`** â€” platform independent, no UI dependency. API
  access, token retrieval, state logic and text formatting live here. Everything
  in it is testable, and everything in it is tested.
* **`ClaudeUsageChecker.App`** â€” Avalonia. The composition root is
  `App.axaml.cs`. Deliberately no DI container and no MVVM framework, to keep
  the dependency list short.

Anything that can live in Core should live in Core.

## Rules that are not up for discussion

These are load-bearing. Each one exists because of a concrete failure; the
reasoning is in [SECURITY.md](SECURITY.md) and [CLAUDE.md](CLAUDE.md).

1. **Never refresh or write back foreign tokens.** Access to Claude Code's
   `.credentials.json` is strictly read-only. Its refresh tokens rotate â€”
   refreshing one would invalidate Claude Code's own sign-in.
2. **Never log tokens.** `AccessToken.ToString()` masks; a test enforces it.
3. **Never poll faster than 180 seconds.** The endpoint throttles permanently
   otherwise. `MonitorOptions.PollInterval` raises smaller values by itself.
4. **`User-Agent: claude-code/<version>` is mandatory** on every call to
   `/api/oauth/usage`. Without it the endpoint answers HTTP 429 forever.
5. **The OAuth scope stays `user:profile`.** Not `user:inference`, not
   `org:create_api_key`. The application needs nothing more, and asking for more
   would claim rights over an account that it never needed.
6. **No personal data in the repository.** Not in test data, screenshots or
   sample output either.
7. **The self-update verifies its SHA-256 checksum.** Nothing is executed
   without it. It is the only safeguard that path has.

## Testing

`TreatWarningsAsErrors` is on. Analyzer exceptions belong in `.editorconfig`
with a stated reason, not as `#pragma` scattered through the source.

Two habits this project relies on:

**Counter-check every new test.** Reintroduce the fault on purpose and confirm
the test actually fails. More than once a test here turned out not to catch what
it claimed â€” including one written to guard version comparison that passed just
as happily with the guard removed.

**Do not trust green tests for user interface or file system work.** Several
defects surfaced only when actually running the application: windows that would
not open, buttons overflowing their window, a cleanup routine that dismantled
the running application. Green tests said nothing about any of them.

## Pull requests

Small and self-contained, please. For anything larger, open an issue first â€”
this project has opinions about scope, and it is better to hear them before you
write the code, not after.

Explain **why**, not only what. The code base documents its decisions, and a
change that arrives without its reasoning loses that.

Add your entry to `CHANGELOG.md` under *Unreleased*, following the style already
there - and to the eight translations under `docs/changelog/`.

## License

Contributions are made under the [GNU General Public License v3.0 or
later](LICENSE), like the rest of the project. By opening a pull request you
agree to that.
