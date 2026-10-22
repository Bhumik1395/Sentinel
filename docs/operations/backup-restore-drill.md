# Backup and restore drill

1. Record the source database version and backup timestamp.
2. Restore into an isolated PostgreSQL instance with no production credentials.
3. Run schema migrations, then verify organization counts, license counts, endpoint counts, alerts, incidents, and audit logs.
4. Authenticate with a non-production Keycloak realm and verify that one organization cannot query another organization’s data.
5. Record recovery time, recovery-point age, discrepancies, and the responsible engineer.

Never perform a restore drill over the production database.
