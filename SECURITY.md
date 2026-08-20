# Security

*Deutsche Fassung: [docs/de/SECURITY.md](docs/de/SECURITY.md)*

Claude UsageChecker handles an OAuth token that grants full access to a Claude
subscription. Dealing with it follows a few fixed rules.

## Principles

### 1. No personal data in the repository

Neither tokens nor account data, usage figures or logs belong in version
control. `.gitignore` blocks the relevant patterns right at the top - among them
`*.credentials.json`, `*.token`, `.env`, `*.pfx`, `*.pem` and
`settings.local.json`.

Check before every commit:

```powershell
git diff --cached | Select-String -Pattern "sk-ant-", "Bearer ", "oat01"
```

### 2. Tokens never in plaintext on disk

The token is only ever stored through the secret store of the operating system:

| Platform | Storage | Protection |
| --- | --- | --- |
| Windows | Credential Manager (`CredWriteW`) | DPAPI, bound to the user account |
| macOS | Keychain (`SecItemAdd` through the Security framework) | Keychain Services, bound to the login keychain |

The framework and not `/usr/bin/security`: that tool takes the password as a
command line argument, and the arguments of a running process are readable by
every account on the machine. Reading is a different matter - the value comes
back on standard output, where nobody else can see it - so the reader for the
Claude Code credentials still uses the tool.

Two separate entries:

| Entry | Content |
| --- | --- |
| `ClaudeUsageChecker:OAuth` | The application's own sign-in (access and refresh token) |
| `ClaudeUsageChecker:OAuthToken` | A manually stored single token (special case) |

The settings file `%LOCALAPPDATA%\ClaudeUsageChecker\settings.json` holds
behaviour settings only, and never secrets.

### 2a. What the application stores where - in full

| Location | Content | Remains after uninstall |
| --- | --- | --- |
| Credential Manager, `ClaudeUsageChecker:OAuth` | the application's own sign-in (access and refresh token) | yes |
| Credential Manager, `ClaudeUsageChecker:OAuthToken` | manually stored single token | yes |
| `%LOCALAPPDATA%\ClaudeUsageChecker\settings.json` | behaviour settings, no secrets | yes |
| `%LOCALAPPDATA%\ClaudeUsageChecker\crash.log` | local crash reports | yes |
| `%LOCALAPPDATA%\Programs\ClaudeUsageChecker\` | the application itself, after setup | yes |
| `HKCU\…\CurrentVersion\Run`, value `ClaudeUsageChecker` | autostart entry | yes |
| `%TEMP%\.net\ClaudeUsageChecker\<id>\` | libraries extracted by the .NET runtime | cleaned up at startup |
| `%TEMP%\ClaudeUsageChecker-<id>.exe` | staging file while updating | deleted immediately |
| Next to the exe: `ClaudeUsageChecker.exe.alt` | the replaced version after an update | deleted on the next start |

The extraction folder is the only location the application does not create
itself: a compressed single file cannot load its native libraries from the
bundle, so the runtime extracts them. Since the id depends on the content, every
version would get a folder of its own - some 16 MB accumulating with every
update. The application therefore clears away the folders of earlier versions
itself.

To remove everything, the rows of the table suffice; there are no further
stores, no database and no traces in other profiles.

**None of it leaves the machine.** There is no telemetry, no usage statistics
and no transmission of crash reports.

### 3. Foreign credentials are only read, own ones fully managed

A strict distinction applies here:

**Credentials of Claude Code** (`%USERPROFILE%\.claude\.credentials.json` or the
macOS keychain) are only ever **read**. The application writes nothing back
there and does not refresh those tokens. The reason: Anthropic rotates refresh
tokens - a refresh by this application would invalidate the sign-in of the
Claude Code installation. The `refreshToken` is therefore not even read into a
model (see `ClaudeCliCredentials`).

**The application's own credentials** from its OAuth flow belong to it alone.
They very much are refreshed as they expire - a rotating refresh token
invalidates nothing foreign here. That is precisely what makes the application
independent of a running Claude Code installation.

Both live in separate entries of the secret store and are never mixed.

### 3a. The application's own sign-in flow

* **PKCE with S256** (RFC 7636) ties the code exchange to the flow that
  requested it. Verifier and `state` are generated afresh per flow from
  `RandomNumberGenerator`.
* **Least privilege:** the only scope requested is `user:profile` - the right to
  read the usage status. Explicitly **not** `user:inference` (making requests on
  behalf of the account) and **not** `org:create_api_key`.
* **No local web server.** The code is pasted by hand rather than received
  through a redirect to `localhost`. That saves an open port and a listening
  service on the user's machine.
* A code from another flow is recognised by its `state` and is not even sent.

### 4. Token values never reach logs

`AccessToken.ToString()` prints origin and expiry only. A test
(`ToString_DoesNotGiveAwayTheTokenValue`) enforces it.

### 5. Frugal network communication

Exactly these counterparts are contacted:

| Target | Purpose | Data transmitted |
| --- | --- | --- |
| `api.anthropic.com/api/oauth/usage` | fetch the usage status | the bearer token only |
| `claude.ai/oauth/authorize` | sign-in page, only in the user's browser | – |
| `platform.claude.com/v1/oauth/token` | exchange the code, refresh tokens | code, PKCE verifier or refresh token |
| `api.github.com` (optional) | version check | none, just a GET |

There is no telemetry, no crash reporting to third parties and no analytics.
Crash reports are written locally to
`%LOCALAPPDATA%\ClaudeUsageChecker\crash.log` and stay there.

### 6. Updates: downloaded code only with verified provenance

The application can replace itself at the push of a button. In doing so it
downloads an executable from the network and starts it - the most delicate
operation in the whole program. Originally that was deliberately ruled out; the
decision was reversed because a notice that has to be acted on by hand tends to
be left lying around, and the application then runs out of date.

Three conditions secure it. If one is missing, nothing is installed:

1. **A verified checksum.** Every release comes with a SHA-256 sum. The
   downloaded file is hashed and compared. On a mismatch it is discarded and
   **not** executed. Without a checksum file nothing is even started.
2. **The address from GitHub's response.** The download address comes from the
   API response for exactly this repository and is not pieced together from file
   names or guessed. Addresses without HTTPS are discarded.
3. **An explicit act by the user.** Installing happens only after a click on
   **Install now and restart**. There is no silent update in the background.

**What the checksum does not achieve.** It is no substitute for a signature:
whoever can create a release can create the matching checksum too. It protects
against corrupted downloads and downloads altered in transit - not against a
compromised account.

That is a deliberate decision: releases are created solely by the repository
owner, and the threat model is the faulty download, not the attacker with write
access. It does mean, though, that securing the GitHub account is part of the
security chain - without two-factor authentication there, the protection here is
moot.

Anyone with stricter requirements signs the packages with a code-signing
certificate and verifies the signature instead of the sum. For this hobby
project the effort is out of all proportion.

The replacement itself exploits the fact that Windows does not allow a running
file to be overwritten but does allow it to be renamed: rename, put the new file
in the old place, start the new version, end this one. If the second step fails,
the first is undone - a working program always remains.

### 7. Consideration for the API

The endpoint throttles aggressively. `MonitorOptions.MinimumInterval` enforces at
least 180 seconds between two calls; a `Retry-After` from the server always takes
precedence, and after failures an exponentially growing backoff up to 30 minutes
applies.

## Reporting a vulnerability

Please do **not** open a public issue. Reports go through
[GitHub Security Advisories](https://github.com/sven-reichelt/Claude-UsageChecker/security/advisories/new)
or directly to the repository owner.

## Checklist before a release

- [ ] `git log -p` searched for token patterns (`sk-ant-`, `oat01`, `Bearer `)
- [ ] No file from `%USERPROFILE%\.claude\` in the repository
- [ ] Screenshots contain no account data
- [ ] `settings.json` and `crash.log` not committed
- [ ] Dependencies checked (`dotnet list package --vulnerable`)
