# Secret exposure disposition

**Decision:** Treat every credential ever committed in `backend/.env` as permanently
compromised. Do not rewrite Git history from this working tree.

## Required operator actions

1. Rotate the Supabase database password/connection credentials.
2. Rotate the Neo4j password.
3. Rotate the Keycloak administrator password and any confidential-client secret.
4. Replace local and production `.env` values from the secret manager, then verify no
   active value matches a historic commit.

The file is removed from the Git index in this change and remains ignored locally.
`backend/.env.example` contains placeholders only. Future secret rotation must occur in
the service provider or secret manager before any deployment uses the new values.
