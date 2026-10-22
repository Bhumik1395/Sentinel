# Phase 4 telemetry E2E verification

This runbook verifies the documented simulated attack from an agent-shaped SUES payload to a persisted, deduplicated alert. Run it only against a disposable local database: it writes telemetry and an alert.

## Preconditions

1. Apply `database/schema.sql` for a clean database, or migrations `0001` through `0004` to an existing test database.
2. Start the API with that database and register a disposable endpoint. Save its client certificate and private key in PEM or PFX form.
3. Set `SENTINEL_API` to the HTTPS API address and use the certificate for the registered endpoint. Mutual TLS is mandatory; bearer authentication is not accepted for ingestion.

## Simulate encoded PowerShell

Copy `backend/tests/Sentinel.Api.Tests/Fixtures/encoded-powershell-batch.json` to a temporary file and send it using the registered endpoint certificate. With PEM files, for example:

```powershell
$api = $env:SENTINEL_API
curl.exe --fail-with-body --cert endpoint-cert.pem --key endpoint-key.pem `
  -H "Content-Type: application/json" `
  --data-binary "@backend/tests/Sentinel.Api.Tests/Fixtures/encoded-powershell-batch.json" `
  "$api/telemetry/ingest"
```

The response must report `accepted: 1`.

To test event idempotency rather than batch replay, submit the same event again with a **new** `batchId`. The response must report `accepted: 0` and `duplicate: 1`.

## Verify the result

For the disposable endpoint's organization, query:

```sql
SELECT event_id, event_type, source, command_line
FROM sentinel.events
WHERE event_id = 'simulation:encoded-powershell:001';

SELECT title, severity, detection_source, mitre_technique, occurrence_count
FROM sentinel.alerts
WHERE dedupe_key = 'behavior:encoded-powershell';
```

Expected result: one `POWERSHELL_EXECUTION` event and one `Encoded PowerShell execution` alert (`HIGH`, `BEHAVIORAL`, `T1059.001`) with `occurrence_count = 1`. The alert is tenant-bound and is visible through `GET /alerts` only to users in that organization.
