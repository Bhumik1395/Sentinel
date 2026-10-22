-- Phase 5: incident handling and replay-safe agent commands.
-- Safe upgrade: additive columns, indexes, and a metadata table only.
-- Rollback: remove the indexes/tables and the four agent_commands columns.
SET search_path TO sentinel, public;

CREATE TABLE IF NOT EXISTS command_signing_keys (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  key_id varchar(80) NOT NULL UNIQUE,
  secret_hash varchar(128) NOT NULL,
  active boolean NOT NULL DEFAULT true,
  created_at timestamptz NOT NULL DEFAULT now(),
  retired_at timestamptz
);

ALTER TABLE agent_commands ADD COLUMN IF NOT EXISTS nonce varchar(80);
ALTER TABLE agent_commands ADD COLUMN IF NOT EXISTS expires_at timestamptz;
ALTER TABLE agent_commands ADD COLUMN IF NOT EXISTS signature varchar(128);
ALTER TABLE agent_commands ADD COLUMN IF NOT EXISTS result_reported_at timestamptz;
UPDATE agent_commands SET nonce = id::text WHERE nonce IS NULL;
UPDATE agent_commands SET expires_at = created_at + interval '5 minutes' WHERE expires_at IS NULL;
ALTER TABLE agent_commands ALTER COLUMN nonce SET NOT NULL;
ALTER TABLE agent_commands ALTER COLUMN expires_at SET NOT NULL;
CREATE UNIQUE INDEX IF NOT EXISTS uq_agent_commands_endpoint_nonce ON agent_commands(endpoint_id, nonce);
CREATE INDEX IF NOT EXISTS idx_agent_commands_poll ON agent_commands(endpoint_id, status, expires_at, created_at);
