# Production readiness gate

Do not onboard a production customer until each item has an owner, dated evidence, and a passing result.

- Run a clean CI build, API test suite, Electron package smoke test, and agent install/upgrade/uninstall smoke test.
- Restore a production-like PostgreSQL backup into an isolated environment and verify tenant-scoped alert, incident, and audit data.
- Load test ingestion, event hunting, alert deduplication, and 15-second command polling against agreed throughput targets.
- Confirm `/operations/readiness` is healthy and telemetry ingestion health is monitored.
- Review Keycloak password, reset, token lifetime, brute-force, SMTP, and TLS settings.
- Rotate the command-signing secret and endpoint CA material in a controlled rehearsal.
- Complete a multi-tenant, enrollment, command-channel, Electron-boundary, and secret-management security review.

The deployment owner records results in the release ticket; a failed item blocks release.
