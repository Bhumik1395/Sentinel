-- Safe upgrade: adds the enrollment boundary and makes endpoint hostnames reusable after retirement.
-- Rollback: drop enrollment_tokens and its index, then drop the partial index before restoring the prior unique constraint.
SET search_path TO sentinel, public;
ALTER TYPE endpoint_status ADD VALUE IF NOT EXISTS 'RETIRED';
ALTER TABLE endpoints DROP CONSTRAINT IF EXISTS endpoints_organization_id_hostname_key;
CREATE UNIQUE INDEX IF NOT EXISTS uq_endpoints_active_hostname ON endpoints (organization_id, hostname) WHERE status <> 'RETIRED';
CREATE TABLE IF NOT EXISTS enrollment_tokens (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
  token_hash varchar(64) NOT NULL UNIQUE, expires_at timestamptz NOT NULL, redeemed_at timestamptz,
  created_at timestamptz NOT NULL DEFAULT now(), CHECK (expires_at > created_at)
);
CREATE INDEX IF NOT EXISTS idx_enrollment_tokens_redeem ON enrollment_tokens (token_hash) WHERE redeemed_at IS NULL;
