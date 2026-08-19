# Recherche: Woher kommen die Nutzungsdaten?

Stand: 19.08.2026. Diese Notiz hält fest, welche Wege geprüft wurden und warum
die Wahl auf den OAuth-Nutzungsendpunkt fiel.

## Gewählter Weg: `GET /api/oauth/usage`

Derselbe Endpunkt, aus dem `/usage` in Claude Code seine Werte bezieht. Er
liefert autoritative Werte – nicht geschätzte – und deckt alle geforderten
Fenster ab.

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

* `utilization` – verbrauchter Anteil in Prozent (0–100)
* `resets_at` – ISO-8601-Zeitstempel in UTC
* Fenster, die das Abonnement nicht kennt, sind `null`

### Fallstricke

| Beobachtung | Konsequenz im Entwurf |
| --- | --- |
| Ohne `User-Agent: claude-code/<version>` folgen sofortige, dauerhafte 429er | Header ist in `UsageApiOptions` fest gesetzt |
| Mit korrektem User-Agent gelten 180 Sekunden als sicheres Intervall | `MonitorOptions.MinimumInterval` = 180 s, nicht unterschreitbar |
| Drosselung greift pro Token, nicht pro Konto | Ein eigenes Langzeit-Token entlastet die Claude-Code-Sitzung |
| Access-Tokens laufen nach ca. 60 Minuten ab | Langzeit-Token aus `claude setup-token` wird bevorzugt |

Quellen:
[Issue #202 (Claude-Code-Usage-Monitor)](https://github.com/Maciek-roboblog/Claude-Code-Usage-Monitor/issues/202),
[Issue #30930 (claude-code)](https://github.com/anthropics/claude-code/issues/30930)

## Tokenquellen

| Quelle | Laufzeit | Bewertung |
| --- | --- | --- |
| `claude setup-token` → `sk-ant-oat01-…` | ca. 1 Jahr | **Gewählt als Primärquelle.** Unabhängig von Claude Code, ein einmaliger Einrichtungsschritt |
| `%USERPROFILE%\.claude\.credentials.json` | ca. 60 Min | Als Rückfallebene. Feld `claudeAiOauth.accessToken`, `expiresAt` als Unix-Millisekunden |
| macOS-Schlüsselbund, Dienst `Claude Code-credentials` | ca. 60 Min | Rückfallebene unter macOS, gelesen über `/usr/bin/security` |
| Umgebungsvariable `CLAUDE_CODE_OAUTH_TOKEN` | – | Praktisch für Entwicklung und Tests |

## Verworfen: Token selbst erneuern

Technisch möglich über
`POST https://console.anthropic.com/v1/oauth/token` mit
`grant_type=refresh_token` und der Claude-Code-Client-ID. Aus drei Gründen
verworfen:

1. **Rotierende Refresh-Tokens.** Erneuert diese Anwendung ein Token und
   schreibt das Ergebnis nicht zurück, verliert Claude Code seine Anmeldung.
   Schreibt sie zurück, greift sie in fremde Anmeldedaten ein – beides
   inakzeptabel für ein Nebenläufer-Werkzeug.
2. **Cloudflare blockiert.** Refresh-Anfragen aus untypischen Umgebungen werden
   als Bot-Verkehr eingestuft und mit 403 abgewiesen.
   ([Issue #47754](https://github.com/anthropics/claude-code/issues/47754))
3. **Sperrgefahr.** Es gibt Berichte über dauerhafte 429er nach automatisierten
   Refresh-Versuchen.
   ([Issue #38248](https://github.com/anthropics/claude-code/issues/38248))

Stattdessen: nur lesen, und ein abgelaufenes Token offen anzeigen.

## Verworfen: lokale Transkripte auswerten

Werkzeuge wie `ccusage` lesen die JSONL-Dateien unter
`~/.claude/projects/**` und rechnen den Verbrauch aus Token-Zählern hoch.

Nachteile: Es sind Schätzungen, keine Limits. Der Weg erfasst nur Claude Code –
Nutzung über claude.ai oder andere Clients fehlt. Und Anthropics Gewichtung von
Tokens zu Kontingent ist nicht öffentlich, sodass die Rechnung driftet.

Als Zusatzanzeige denkbar, als alleinige Quelle ungeeignet.

## Verworfen: Admin-Nutzungs-API

`https://api.anthropic.com/v1/organizations/usage_report/messages` liefert
Verbrauchsdaten pro Organisation. Sie bezieht sich jedoch auf API-Kontingente
mit Abrechnung nach Verbrauch, nicht auf die Sitzungs- und Wochenlimits eines
Pro-/Max-Abonnements. Falsche Datenquelle für dieses Vorhaben.

## Offene Punkte

* Ob ein Token aus `claude setup-token` von `/api/oauth/usage` akzeptiert wird,
  ist noch nicht praktisch bestätigt – es ist ein OAuth-Access-Token derselben
  Familie, was dafür spricht. **Erster Test nach der Einrichtung.**
* Der `User-Agent` trägt derzeit eine fest verdrahtete Version. Falls Anthropic
  gegen veraltete Versionen filtert, muss sie nachgeführt werden.
