-- Phase 4: durable ingestion batches, explainable alert deduplication, and retention support.
-- Safe upgrade: additive only.  Run after 0001-0003.
-- Rollback: DROP INDEX uq_alerts_organization_dedupe_key; ALTER TABLE alerts DROP COLUMN dedupe_key;
--           DROP TABLE telemetry_rejections, telemetry_ingestion_batches.
SET search_path TO sentinel, public;

CREATE TABLE IF NOT EXISTS telemetry_ingestion_batches (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  endpoint_id uuid NOT NULL REFERENCES endpoints(id) ON DELETE CASCADE,
  batch_id uuid NOT NULL,
  payload_hash varchar(64) NOT NULL,
  accepted_count integer NOT NULL DEFAULT 0,
  rejected_count integer NOT NULL DEFAULT 0,
  received_at timestamptz NOT NULL DEFAULT now(),
  UNIQUE (endpoint_id, batch_id)
);

ALTER TABLE events ADD COLUMN IF NOT EXISTS file_hash varchar(128);

CREATE TABLE IF NOT EXISTS telemetry_rejections (
  id bigserial PRIMARY KEY,
  endpoint_id uuid NOT NULL REFERENCES endpoints(id) ON DELETE CASCADE,
  batch_id uuid NOT NULL,
  event_id varchar(120),
  reason varchar(200) NOT NULL,
  received_at timestamptz NOT NULL DEFAULT now()
);

ALTER TABLE alerts ADD COLUMN IF NOT EXISTS dedupe_key varchar(128);
UPDATE alerts SET dedupe_key = 'legacy:' || id::text WHERE dedupe_key IS NULL;
ALTER TABLE alerts ALTER COLUMN dedupe_key SET NOT NULL;
CREATE UNIQUE INDEX IF NOT EXISTS uq_alerts_organization_dedupe_key
  ON alerts (organization_id, dedupe_key);
CREATE INDEX IF NOT EXISTS idx_telemetry_batches_endpoint_received
  ON telemetry_ingestion_batches (endpoint_id, received_at DESC);
