-- Phase 7 operational state. Safe upgrade: additive tables only.
SET search_path TO sentinel, public;

CREATE TABLE IF NOT EXISTS graph_projection_runs (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  organization_id uuid NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
  status varchar(24) NOT NULL CHECK (status IN ('PENDING','RUNNING','COMPLETED','FAILED')),
  started_at timestamptz,
  completed_at timestamptz,
  error_message text,
  created_at timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS idx_graph_projection_runs_org_created ON graph_projection_runs (organization_id, created_at DESC);

CREATE TABLE IF NOT EXISTS operational_checks (
  check_name varchar(120) PRIMARY KEY,
  status varchar(24) NOT NULL CHECK (status IN ('HEALTHY','DEGRADED','FAILED')),
  detail text,
  observed_at timestamptz NOT NULL DEFAULT now()
);
