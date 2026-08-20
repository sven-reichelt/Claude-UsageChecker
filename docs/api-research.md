# Research: where does the usage data come from?

As of 2026-08-19. This note records which routes were examined and why the choice
fell on the OAuth usage endpoint.

## Chosen route: `GET /api/oauth/usage`

The same endpoint `/usage` in Claude Code takes its figures from. It supplies
authoritative values - not estimates - and covers every window required.

```http
GET https://api.anthropic.com/api/oauth/usage
Authorization:  Bearer <oauth_access_token>
anthropic-beta: oauth-2025-04-20
User-Agent:     claude-code/<version>
Content-Type:   application/json
```

```json
{
  "five_hour":        { "utilization": 33.0, "resets_at": "2026-04-11T07:00:00.528743+00:00" },
  "seven_day":        { "utilization": 13.0, "resets_at": "2026-04-17T00:59:59.951713+00:00" },
  "seven_day_opus":   null,
  "seven_day_sonnet": { "utilization":  1.0, "resets_at": "2026-04-16T03:00:00.951719+00:00" },
  "extra_usage": {
    "is_enabled": false, "monthly_limit": null, "used_credits": null, "utilization": null
  }
}
```

* `utilization` – consumed share in percent (0–100)
* `resets_at` – ISO 8601 timestamp in UTC
* windows the subscription does not know are `null`

### Addendum 2026-08-19: the `limits` list supersedes the individual fields

The endpoint now delivers **both side by side**. Next to the fields above sits a
list expressing the same thing model-independently:

```json
{
  "limits": [
    { "kind": "session",       "group": "session", "percent":  6, "resets_at": "…", "scope": null },
    { "kind": "weekly_all",    "group": "weekly",  "percent": 18, "resets_at": "…", "scope": null },
    { "kind": "weekly_scoped", "group": "weekly",  "percent":  2, "resets_at": "…",
      "scope": { "model": { "id": null, "display_name": "Fable" }, "surface": null } }
  ]
}
```

**Why this became important:** the individual fields carry the model name in
their *identifier* - `seven_day_opus`, `seven_day_sonnet`. When Anthropic moved
the weekly limit to Fable, both were `null`, and a field `seven_day_fable` does
not exist. The limit was therefore missing from the display entirely, without
anything failing. In the list, by contrast, the name sits in the *content*; every
future model appears without a code change.

The list therefore takes precedence, and the individual fields remain a fallback.
Covered by `ScopedLimitMappingTests`.

### Addendum 2026-08-20: `spend` supersedes `extra_usage`

Measured against an account with the extra usage quota switched on - until then
the field could not be checked at all, which is why it stayed a note here for a
day.

```json
{
  "extra_usage": {
    "is_enabled": true, "monthly_limit": 5000, "used_credits": 2276.0,
    "utilization": 45.52, "currency": "EUR", "decimal_places": 2
  },
  "spend": {
    "used":  { "amount_minor": 2276, "currency": "EUR", "exponent": 2 },
    "limit": { "amount_minor": 5000, "currency": "EUR", "exponent": 2 },
    "percent": 46, "enabled": true
  }
}
```

**`used_credits` is not a count of credits.** It is an amount of money in the
smallest unit of its currency: 2276 means 22.76 EUR. The name is misleading, and
the application believed it - it showed "2276.00 of 5000.00 credits", wrong by a
factor of a hundred and in the wrong unit. Nothing failed, which is precisely why
it survived: without the quota switched on, `extra_usage` is empty and the
mistake invisible.

`spend` says what it means, with the amount, the currency and the exponent side
by side. It is therefore read in preference, exactly as `limits` is preferred
over the fixed window fields; `extra_usage` remains a fallback and has since
gained `currency` and `decimal_places` of its own.

**The currency belongs to the account.** EUR here, USD for an account billed in
the United States, BRL in Brazil - and the number of decimal places comes with
it, because not every currency has two. Both are read from the response. Covered
by `ExtraUsageMappingTests`.

Not acted on:

* **Code-name fields with no discernible meaning:** `seven_day_oauth_apps`,
  `seven_day_cowork`, `seven_day_omelette`, `tangelo`, `iguana_necktie`,
  `omelette_promotional`, `nimbus_quill`, `cinder_cove`, `amber_ladder`. All
  `null` except `nimbus_quill` (`utilization: 0.0`, without `resets_at`).
  Evidently internal identifiers; nothing to build on.
* `limits[].is_active` distinguishes the entries (`true` for `weekly_all`, `false`
  otherwise); the meaning is unclear - hence unused.
* `limits[].severity` is supplied by the API itself ("normal"). The application
  still classifies for itself, because the thresholds are configurable.

### Pitfalls

| Observation | Consequence in the design |
| --- | --- |
| Without `User-Agent: claude-code/<version>` immediate, permanent 429s follow | The header is fixed in `UsageApiOptions` |
| With the correct user agent, 180 seconds counts as a safe interval | `MonitorOptions.MinimumInterval` = 180 s, cannot go below |
| Throttling applies per token, not per account | The polling interval stays high, so that Claude Code is not slowed down |
| Access tokens expire after about 60 minutes | Without a running Claude Code sign-in the application says so openly |
| Setup tokens lack the `user:profile` scope | On HTTP 401/403 the call moves on to the next token source |

Sources:
[Issue #202 (Claude-Code-Usage-Monitor)](https://github.com/Maciek-roboblog/Claude-Code-Usage-Monitor/issues/202),
[Issue #30930 (claude-code)](https://github.com/anthropics/claude-code/issues/30930)

## Token sources

| Source | Lifetime | Assessment |
| --- | --- | --- |
| `%USERPROFILE%\.claude\.credentials.json` | approx. 60 min | **Chosen.** Field `claudeAiOauth.accessToken`, `expiresAt` as Unix milliseconds |
| macOS keychain, service `Claude Code-credentials` | approx. 60 min | The same on macOS, read through `/usr/bin/security` |
| Environment variable `CLAUDE_CODE_OAUTH_TOKEN` | – | Handy for development and tests |
| `claude setup-token` → `sk-ant-oat01-…` | approx. 1 year | **Unsuitable**, see below |

### Withdrawn: entering a token by hand

Until version 0.5 the settings carried a section "Single token (special case)":
a text box, a check against the endpoint before storing, and the value in the
Windows Credential Manager. It was removed on 2026-08-20 without ever having
been of use to anyone.

The reason lies in the section above. What a user would have to hand is a token
from `claude setup-token` - and that one lacks the `user:profile` scope, so the
endpoint turns it down. The only tokens that do work are the one belonging to the
Claude Code installation, which the application reads by itself, and the one from
its own sign-in, which it manages by itself. Neither is pasted in by hand. What
remained was an input field for a case that does not arise, on the most sensitive
data the application touches.

The reading side stays: `SecretStoreTokenProvider` continues to look in the
secret store, so a token stored by an earlier version keeps working. Only the way
to put one there is gone. `TokenValidator` was deleted along with it - it existed
solely to check what was entered. Both are in the history, should the case ever
arise after all.

### Refuted: `claude setup-token` as an independent primary source

That was this project's original assumption - and it is wrong. Measured on
2026-08-19 with a freshly created token (108 characters, prefix
`sk-ant-oat01-`):

| Request | Result |
| --- | --- |
| `GET /api/oauth/usage` with the `anthropic-beta` header | **HTTP 403** |
| `GET /api/oauth/usage` without the `anthropic-beta` header | **HTTP 403** |
| `POST /v1/messages` (minimal request) | **HTTP 200** |

```json
{"type":"error","error":{"type":"permission_error",
 "message":"OAuth token does not meet scope requirement user:profile"}}
```

The token is therefore valid and capable - it simply does not carry the
`user:profile` scope the usage endpoint demands. `setup-token` evidently requests
inference rights alone. The token of the interactive sign-in
(`.credentials.json`) has both and works.

**Consequences for the design:**

1. The project goal "independent of Claude Code" is not reachable this way - but
   it is through an OAuth flow of our own, see below.
2. A stored `setup-token` initially paralysed the application completely, because
   the token chain only moved on for an *empty* source, not for a *rejected* one.
   The call now tries the next source on HTTP 401/403. Covered by
   `TokenFallbackTests`.
3. The settings check an entered token against the endpoint before storing it and
   turn it down with the reason, rather than filing it away in silence.

## Chosen: an OAuth flow of our own, with PKCE

The route to independence. Played through completely and confirmed on
2026-08-19.

```
1. GET  https://claude.ai/oauth/authorize
        ?code=true&client_id=…&response_type=code
        &redirect_uri=https://console.anthropic.com/oauth/code/callback
        &scope=user:profile
        &code_challenge=…&code_challenge_method=S256&state=…

2. The user grants access, the page shows CODE#STATE

3. POST https://platform.claude.com/v1/oauth/token
        {"grant_type":"authorization_code","code":…,"state":…,
         "client_id":…,"redirect_uri":…,"code_verifier":…}
     → {"access_token":…,"refresh_token":…,"expires_in":28800,"scope":"user:profile"}
```

**`user:profile` alone suffices.** Neither `user:inference` nor
`org:create_api_key` is needed - the approval page accepts the single scope and
the usage endpoint accepts the token.

### Measured peculiarities

| Observation | Consequence |
| --- | --- |
| `console.anthropic.com/v1/oauth/token` answers **HTTP 404** | The exchange goes through `platform.claude.com`. Recorded in `OAuthEndpointTests` |
| Without `state` in the body: `Invalid request format` | `state` is always sent, on the exchange as well |
| The access token is valid for roughly **8 hours** | Refresh five minutes before expiry |
| Refreshing returns a **new** refresh token | It rotates; the application stores it immediately |

The refresh was verified against the real server and returns a fresh token pair
with an unchanged scope.

### Independence confirmed in the field

Played through on 2026-08-19 on a machine **without a Claude Code installation**:
a grey icon and the notice about a missing token before signing in, the complete
usage status after. Since no foreign token was to be found there, the display can
only have run through the application's own sign-in.

That closes the chain: the route through `claude setup-token` was unsuitable, the
OAuth flow of our own carries.

### Residual risk

The flow uses the publicly known client id of Claude Code, because Anthropic
offers no registration for third-party applications. To the authorization server
the application therefore presents itself as Claude Code. Not an officially
supported route; endpoints and behaviour may change - the 404 on the old token
endpoint is evidence of exactly that.

## Discarded: refreshing **foreign** tokens ourselves

What is meant is the refresh token from `.credentials.json`, that is, Claude
Code's. The application's own token from the flow above very much is refreshed -
the difference is whose credentials they are. Discarded for three reasons:

1. **Rotating refresh tokens.** If this application refreshes a token and does
   not write the result back, Claude Code loses its sign-in. If it does write
   back, it interferes with foreign credentials - both unacceptable for a
   side-along tool.
2. **Cloudflare blocks it.** Refresh requests from unusual environments are
   classified as bot traffic and turned away with 403.
   ([Issue #47754](https://github.com/anthropics/claude-code/issues/47754))
3. **Risk of a ban.** There are reports of permanent 429s after automated refresh
   attempts.
   ([Issue #38248](https://github.com/anthropics/claude-code/issues/38248))

Instead: read only, and show an expired token openly.

## Discarded: evaluating local transcripts

Tools such as `ccusage` read the JSONL files under `~/.claude/projects/**` and
extrapolate consumption from token counters.

Drawbacks: those are estimates, not limits. The route captures Claude Code only -
usage through claude.ai or other clients is missing. And Anthropic's weighting of
tokens against the quota is not public, so the calculation drifts.

Conceivable as an additional display, unsuitable as the sole source.

## Discarded: the admin usage API

`https://api.anthropic.com/v1/organizations/usage_report/messages` supplies
consumption data per organisation. It relates to API quotas billed by use,
though, not to the session and weekly limits of a Pro or Max subscription. The
wrong data source for this undertaking.

## Open questions

* The `User-Agent` currently carries a hard-wired version. Should Anthropic start
  filtering against outdated versions, it has to be kept current.
* How long the refresh token remains valid is unknown. As long as the application
  runs regularly, it refreshes in good time. After a very long break it may have
  lapsed - then signing in once more is needed.
* Rotating refresh tokens carry a narrow window: if storing fails between a
  successful refresh and filing it away, the old token is spent and the new one
  lost. The consequence would be signing in again, not data loss.
