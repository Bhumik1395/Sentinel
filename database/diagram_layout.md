# Sentinel Database Diagram Layout

Supabase auto-arranges foreign-key diagrams and does not provide SQL-level control over table positions. Use this layout as the manual arrangement reference.

## Recommended Groups

Place the groups left-to-right in the order data normally flows through Sentinel.

```text
┌────────────────────────────┐
│ Core / Tenant              │
│ organizations              │
│ users                      │
│ licenses                   │
│ applications               │
│ support_engagements        │
└──────────────┬─────────────┘
               │
┌──────────────▼─────────────┐
│ Agent / Collection         │
│ policies                   │
│ endpoints                  │
│ endpoint_heartbeats        │
│ agent_commands             │
└──────────────┬─────────────┘
               │
┌──────────────▼─────────────┐
│ Telemetry / Detection      │
│ events                     │
│ detection_rules            │
│ iocs                       │
│ alerts                     │
└──────────────┬─────────────┘
               │
┌──────────────▼─────────────┐
│ Investigation              │
│ incidents                  │
│ incident_alerts            │
│ incident_notes             │
└──────────────┬─────────────┘
               │
┌──────────────▼─────────────┐
│ Response / Extra Modules   │
│ soar_actions               │
│ deception_assets           │
│ ml_baselines               │
│ reports                    │
│ audit_logs                 │
│ platform_settings          │
└────────────────────────────┘
```

## Suggested Supabase Placement

Top row:

- `organizations` in the top center.
- `users` to the right of `organizations`.
- `licenses`, `applications`, and `support_engagements` under `organizations`.

Middle-left:

- `policies`
- `endpoints`
- `endpoint_heartbeats`
- `agent_commands`

Center:

- `events` as the main hub.
- `detection_rules` and `iocs` near `alerts`.
- `alerts` to the right of `events`.

Middle-right:

- `incidents`
- `incident_alerts` between `alerts` and `incidents`.
- `incident_notes` under `incidents`.

Bottom row:

- `soar_actions` below `alerts` and `incidents`.
- `deception_assets` near `events`.
- `ml_baselines` near `endpoints`.
- `reports`, `audit_logs`, and `platform_settings` at the bottom or far right.

## Relationship Hotspots

These tables have the most lines and should sit near the center:

- `organizations`
- `users`
- `endpoints`
- `events`
- `alerts`
- `incidents`

If those are centered, the rest of the diagram becomes much easier to read.
