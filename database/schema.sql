CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE SCHEMA IF NOT EXISTS sentinel;
SET search_path TO sentinel, public;

CREATE TYPE user_role AS ENUM (
  'OWNER',
  'SUPPORT_TEAM',
  'CSO',
  'SECURITY_ADMINISTRATOR',
  'SECURITY_ANALYST'
);

CREATE TYPE user_status AS ENUM ('ACTIVE', 'DEACTIVATED');
CREATE TYPE endpoint_status AS ENUM ('HEALTHY', 'DEGRADED', 'OFFLINE');
CREATE TYPE severity_level AS ENUM ('INFORMATIONAL', 'LOW', 'MEDIUM', 'HIGH', 'CRITICAL');
CREATE TYPE sues_event_type AS ENUM (
  'PROCESS_START',
  'PROCESS_STOP',
  'NETWORK_CONNECTION',
  'DNS_QUERY',
  'FILE_ACCESS',
  'FILE_MODIFICATION',
  'REGISTRY_MODIFICATION',
  'POWERSHELL_EXECUTION',
  'ALERT',
  'INCIDENT'
);
CREATE TYPE telemetry_source AS ENUM ('ETW', 'AMSI', 'SYSMON', 'WINDOWS_EVENT_LOG', 'SENTINEL');
CREATE TYPE alert_status AS ENUM ('OPEN', 'ACKNOWLEDGED', 'IN_INVESTIGATION', 'RESOLVED', 'FALSE_POSITIVE');
CREATE TYPE incident_status AS ENUM ('OPEN', 'INVESTIGATING', 'CONTAINED', 'RESOLVED', 'CLOSED');
CREATE TYPE ioc_type AS ENUM ('IP_ADDRESS', 'DOMAIN', 'URL', 'FILE_HASH');
CREATE TYPE application_status AS ENUM ('PENDING', 'APPROVED', 'REJECTED');
CREATE TYPE report_type AS ENUM ('INCIDENT', 'SECURITY_OPERATIONS', 'EXECUTIVE_SUMMARY');
CREATE TYPE report_status AS ENUM ('PENDING', 'GENERATING', 'COMPLETED', 'FAILED');
CREATE TYPE report_format AS ENUM ('CSV', 'JSON');
CREATE TYPE soar_action_type AS ENUM ('KILL_PROCESS', 'ENDPOINT_ISOLATION', 'CREATE_INCIDENT');
CREATE TYPE soar_action_status AS ENUM ('PENDING_APPROVAL', 'APPROVED', 'DENIED', 'DISPATCHED', 'EXECUTED', 'FAILED', 'CANCELLED');
CREATE TYPE deception_asset_type AS ENUM ('HONEY_FILE', 'CANARY_TOKEN');
CREATE TYPE deception_asset_status AS ENUM ('ACTIVE', 'DISABLED', 'TRIGGERED');

CREATE TABLE organizations (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  name varchar(200) NOT NULL,
  slug varchar(120) NOT NULL UNIQUE,
  company_scope boolean NOT NULL DEFAULT false,
  retention_days integer NOT NULL DEFAULT 90 CHECK (retention_days IN (30, 60, 90, 365)),
  log_retention_days integer NOT NULL DEFAULT 90 CHECK (log_retention_days IN (30, 90, 180, 365)),
  created_at timestamptz NOT NULL DEFAULT now(),
  updated_at timestamptz NOT NULL DEFAULT now(),
  deleted_at timestamptz
);

CREATE TABLE users (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  organization_id uuid REFERENCES organizations(id) ON DELETE RESTRICT,
  keycloak_user_id varchar(120) NOT NULL UNIQUE,
  email varchar(320) NOT NULL UNIQUE,
  display_name varchar(200) NOT NULL,
  role user_role NOT NULL,
  status user_status NOT NULL DEFAULT 'ACTIVE',
  last_login_at timestamptz,
  created_at timestamptz NOT NULL DEFAULT now(),
  updated_at timestamptz NOT NULL DEFAULT now(),
  CHECK (
    (role IN ('OWNER', 'SUPPORT_TEAM') AND organization_id IS NULL)
    OR
    (role IN ('CSO', 'SECURITY_ADMINISTRATOR', 'SECURITY_ANALYST') AND organization_id IS NOT NULL)
  )
);

CREATE TABLE support_engagements (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  organization_id uuid NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
  support_user_id uuid NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
  reason text NOT NULL,
  starts_at timestamptz NOT NULL DEFAULT now(),
  ends_at timestamptz NOT NULL,
  created_by uuid REFERENCES users(id) ON DELETE SET NULL,
  created_at timestamptz NOT NULL DEFAULT now(),
  CHECK (ends_at > starts_at)
);

CREATE TABLE licenses (
  organization_id uuid PRIMARY KEY REFERENCES organizations(id) ON DELETE CASCADE,
  endpoint_cap integer NOT NULL CHECK (endpoint_cap >= 0),
  current_endpoint_count integer NOT NULL DEFAULT 0 CHECK (current_endpoint_count >= 0),
  starts_at timestamptz NOT NULL DEFAULT now(),
  expires_at timestamptz,
  updated_at timestamptz NOT NULL DEFAULT now(),
  CHECK (current_endpoint_count <= endpoint_cap)
);

CREATE TABLE applications (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  company_name varchar(200) NOT NULL,
  contact_name varchar(200) NOT NULL,
  contact_email varchar(320) NOT NULL,
  contact_phone varchar(50),
  requested_endpoint_cap integer CHECK (requested_endpoint_cap IS NULL OR requested_endpoint_cap > 0),
  notes text,
  status application_status NOT NULL DEFAULT 'PENDING',
  reviewed_by uuid REFERENCES users(id) ON DELETE SET NULL,
  reviewed_at timestamptz,
  rejection_reason text,
  created_organization_id uuid REFERENCES organizations(id) ON DELETE SET NULL,
  created_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE policies (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  organization_id uuid NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
  version integer NOT NULL,
  name varchar(160) NOT NULL,
  collect_process_events boolean NOT NULL DEFAULT true,
  collect_network_events boolean NOT NULL DEFAULT true,
  collect_registry_events boolean NOT NULL DEFAULT true,
  collect_file_events boolean NOT NULL DEFAULT true,
  collect_script_events boolean NOT NULL DEFAULT true,
  config jsonb NOT NULL DEFAULT '{}'::jsonb,
  created_by uuid REFERENCES users(id) ON DELETE SET NULL,
  created_at timestamptz NOT NULL DEFAULT now(),
  UNIQUE (organization_id, version)
);

CREATE TABLE endpoints (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  organization_id uuid NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
  policy_id uuid REFERENCES policies(id) ON DELETE SET NULL,
  hostname varchar(255) NOT NULL,
  agent_version varchar(50) NOT NULL,
  policy_version integer NOT NULL DEFAULT 1,
  status endpoint_status NOT NULL DEFAULT 'HEALTHY',
  last_heartbeat_at timestamptz,
  registered_at timestamptz NOT NULL DEFAULT now(),
  certificate_fingerprint varchar(128),
  metadata jsonb NOT NULL DEFAULT '{}'::jsonb,
  UNIQUE (organization_id, hostname)
);

CREATE TABLE endpoint_heartbeats (
  id bigserial PRIMARY KEY,
  endpoint_id uuid NOT NULL REFERENCES endpoints(id) ON DELETE CASCADE,
  organization_id uuid NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
  agent_version varchar(50) NOT NULL,
  policy_version integer NOT NULL,
  status endpoint_status NOT NULL,
  details jsonb NOT NULL DEFAULT '{}'::jsonb,
  received_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE events (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  event_id varchar(120) NOT NULL,
  organization_id uuid NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
  endpoint_id uuid NOT NULL REFERENCES endpoints(id) ON DELETE CASCADE,
  user_id uuid REFERENCES users(id) ON DELETE SET NULL,
  schema_version varchar(20) NOT NULL DEFAULT '1.0',
  event_type sues_event_type NOT NULL,
  source telemetry_source NOT NULL,
  hostname varchar(255) NOT NULL,
  severity severity_level NOT NULL,
  occurred_at timestamptz NOT NULL,
  ingested_at timestamptz NOT NULL DEFAULT now(),
  process_name text,
  process_id integer,
  parent_process_name text,
  parent_process_id integer,
  command_line text,
  domain text,
  ip_address inet,
  destination_port integer CHECK (destination_port IS NULL OR destination_port BETWEEN 0 AND 65535),
  protocol varchar(20),
  file_path text,
  registry_key text,
  alert_id uuid,
  incident_id uuid,
  raw jsonb NOT NULL DEFAULT '{}'::jsonb,
  UNIQUE (organization_id, event_id)
);

CREATE TABLE detection_rules (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  rule_key varchar(120) NOT NULL UNIQUE,
  name varchar(240) NOT NULL,
  source varchar(40) NOT NULL CHECK (source IN ('BEHAVIORAL', 'SIGMA', 'IOC', 'ML', 'DECEPTION', 'OPERATIONAL')),
  severity severity_level NOT NULL,
  mitre_technique varchar(40),
  enabled boolean NOT NULL DEFAULT true,
  definition jsonb NOT NULL DEFAULT '{}'::jsonb,
  created_at timestamptz NOT NULL DEFAULT now(),
  updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE iocs (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  organization_id uuid NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
  type ioc_type NOT NULL,
  value text NOT NULL,
  description text,
  expires_at timestamptz NOT NULL,
  created_by uuid REFERENCES users(id) ON DELETE SET NULL,
  created_at timestamptz NOT NULL DEFAULT now(),
  UNIQUE (organization_id, type, value)
);

CREATE TABLE alerts (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  organization_id uuid NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
  endpoint_id uuid NOT NULL REFERENCES endpoints(id) ON DELETE CASCADE,
  user_id uuid REFERENCES users(id) ON DELETE SET NULL,
  event_id uuid REFERENCES events(id) ON DELETE SET NULL,
  rule_id uuid REFERENCES detection_rules(id) ON DELETE SET NULL,
  title varchar(240) NOT NULL,
  severity severity_level NOT NULL,
  detection_source varchar(40) NOT NULL,
  mitre_technique varchar(40),
  status alert_status NOT NULL DEFAULT 'OPEN',
  occurrence_count integer NOT NULL DEFAULT 1 CHECK (occurrence_count > 0),
  first_seen_at timestamptz NOT NULL,
  last_seen_at timestamptz NOT NULL,
  evidence jsonb NOT NULL DEFAULT '{}'::jsonb,
  created_at timestamptz NOT NULL DEFAULT now()
);

ALTER TABLE events
  ADD CONSTRAINT fk_events_alert
  FOREIGN KEY (alert_id) REFERENCES alerts(id) ON DELETE SET NULL;

CREATE TABLE incidents (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  organization_id uuid NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
  title varchar(240) NOT NULL,
  severity severity_level NOT NULL,
  status incident_status NOT NULL DEFAULT 'OPEN',
  assigned_to uuid REFERENCES users(id) ON DELETE SET NULL,
  root_cause text,
  summary text,
  opened_at timestamptz NOT NULL DEFAULT now(),
  resolved_at timestamptz,
  closed_at timestamptz,
  metadata jsonb NOT NULL DEFAULT '{}'::jsonb
);

ALTER TABLE events
  ADD CONSTRAINT fk_events_incident
  FOREIGN KEY (incident_id) REFERENCES incidents(id) ON DELETE SET NULL;

CREATE TABLE incident_alerts (
  incident_id uuid NOT NULL REFERENCES incidents(id) ON DELETE CASCADE,
  alert_id uuid NOT NULL REFERENCES alerts(id) ON DELETE CASCADE,
  linked_at timestamptz NOT NULL DEFAULT now(),
  PRIMARY KEY (incident_id, alert_id)
);

CREATE TABLE incident_notes (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  incident_id uuid NOT NULL REFERENCES incidents(id) ON DELETE CASCADE,
  author_id uuid REFERENCES users(id) ON DELETE SET NULL,
  note text NOT NULL,
  created_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE soar_actions (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  organization_id uuid NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
  requested_by uuid NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
  approved_by uuid REFERENCES users(id) ON DELETE RESTRICT,
  target_endpoint_id uuid NOT NULL REFERENCES endpoints(id) ON DELETE CASCADE,
  alert_id uuid REFERENCES alerts(id) ON DELETE SET NULL,
  incident_id uuid REFERENCES incidents(id) ON DELETE SET NULL,
  action_type soar_action_type NOT NULL,
  status soar_action_status NOT NULL DEFAULT 'PENDING_APPROVAL',
  justification text NOT NULL,
  request_payload jsonb NOT NULL DEFAULT '{}'::jsonb,
  result jsonb,
  requested_at timestamptz NOT NULL DEFAULT now(),
  decided_at timestamptz,
  dispatched_at timestamptz,
  executed_at timestamptz,
  CHECK (approved_by IS NULL OR approved_by <> requested_by),
  CHECK (
    (status IN ('PENDING_APPROVAL', 'CANCELLED') AND approved_by IS NULL)
    OR
    (status IN ('APPROVED', 'DISPATCHED', 'EXECUTED', 'FAILED') AND approved_by IS NOT NULL)
    OR
    (status = 'DENIED')
  )
);

CREATE TABLE agent_commands (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  organization_id uuid NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
  endpoint_id uuid NOT NULL REFERENCES endpoints(id) ON DELETE CASCADE,
  soar_action_id uuid REFERENCES soar_actions(id) ON DELETE SET NULL,
  command_type soar_action_type NOT NULL,
  payload jsonb NOT NULL DEFAULT '{}'::jsonb,
  status varchar(40) NOT NULL DEFAULT 'PENDING',
  created_at timestamptz NOT NULL DEFAULT now(),
  fetched_at timestamptz,
  acknowledged_at timestamptz,
  result jsonb
);

CREATE TABLE deception_assets (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  organization_id uuid NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
  endpoint_id uuid REFERENCES endpoints(id) ON DELETE CASCADE,
  type deception_asset_type NOT NULL,
  name varchar(200) NOT NULL,
  value text NOT NULL,
  status deception_asset_status NOT NULL DEFAULT 'ACTIVE',
  created_by uuid REFERENCES users(id) ON DELETE SET NULL,
  created_at timestamptz NOT NULL DEFAULT now(),
  triggered_at timestamptz,
  trigger_event_id uuid REFERENCES events(id) ON DELETE SET NULL
);

CREATE TABLE ml_baselines (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  organization_id uuid NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
  endpoint_id uuid NOT NULL REFERENCES endpoints(id) ON DELETE CASCADE,
  model_version varchar(50) NOT NULL,
  learning_started_at timestamptz NOT NULL DEFAULT now(),
  learning_completed_at timestamptz,
  feature_state jsonb NOT NULL DEFAULT '{}'::jsonb,
  created_at timestamptz NOT NULL DEFAULT now(),
  updated_at timestamptz NOT NULL DEFAULT now(),
  UNIQUE (endpoint_id, model_version)
);

CREATE TABLE reports (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  organization_id uuid REFERENCES organizations(id) ON DELETE CASCADE,
  requested_by uuid REFERENCES users(id) ON DELETE SET NULL,
  type report_type NOT NULL,
  status report_status NOT NULL DEFAULT 'PENDING',
  filters jsonb NOT NULL DEFAULT '{}'::jsonb,
  artifact_uri text,
  export_format report_format,
  generated_at timestamptz,
  created_at timestamptz NOT NULL DEFAULT now(),
  error_message text
);

CREATE TABLE audit_logs (
  id bigserial PRIMARY KEY,
  organization_id uuid REFERENCES organizations(id) ON DELETE SET NULL,
  actor_user_id uuid REFERENCES users(id) ON DELETE SET NULL,
  actor_role user_role,
  action varchar(120) NOT NULL,
  entity_type varchar(80),
  entity_id uuid,
  ip_address inet,
  user_agent text,
  details jsonb NOT NULL DEFAULT '{}'::jsonb,
  created_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE platform_settings (
  key varchar(120) PRIMARY KEY,
  value jsonb NOT NULL,
  updated_by uuid REFERENCES users(id) ON DELETE SET NULL,
  updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX idx_users_org_role ON users (organization_id, role);
CREATE INDEX idx_support_engagements_active ON support_engagements (organization_id, support_user_id, starts_at, ends_at);
CREATE INDEX idx_endpoints_org_status ON endpoints (organization_id, status);
CREATE INDEX idx_heartbeats_endpoint_received ON endpoint_heartbeats (endpoint_id, received_at DESC);
CREATE INDEX idx_events_org_time ON events (organization_id, occurred_at DESC);
CREATE INDEX idx_events_endpoint_time ON events (endpoint_id, occurred_at DESC);
CREATE INDEX idx_events_type_time ON events (event_type, occurred_at DESC);
CREATE INDEX idx_events_raw_gin ON events USING gin (raw);
CREATE INDEX idx_iocs_active ON iocs (organization_id, type, expires_at);
CREATE INDEX idx_alerts_org_status_severity ON alerts (organization_id, status, severity, last_seen_at DESC);
CREATE INDEX idx_alerts_dedupe ON alerts (organization_id, rule_id, endpoint_id, user_id, last_seen_at DESC);
CREATE INDEX idx_incidents_org_status ON incidents (organization_id, status, opened_at DESC);
CREATE INDEX idx_soar_actions_org_status ON soar_actions (organization_id, status, requested_at DESC);
CREATE INDEX idx_agent_commands_pending ON agent_commands (endpoint_id, status, created_at);
CREATE INDEX idx_deception_assets_org_status ON deception_assets (organization_id, status);
CREATE INDEX idx_reports_org_type ON reports (organization_id, type, created_at DESC);
CREATE INDEX idx_audit_logs_org_time ON audit_logs (organization_id, created_at DESC);
CREATE INDEX idx_audit_logs_action_time ON audit_logs (action, created_at DESC);

CREATE OR REPLACE FUNCTION set_updated_at()
RETURNS trigger AS $$
BEGIN
  NEW.updated_at = now();
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_organizations_updated_at
BEFORE UPDATE ON organizations
FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_users_updated_at
BEFORE UPDATE ON users
FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_detection_rules_updated_at
BEFORE UPDATE ON detection_rules
FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_ml_baselines_updated_at
BEFORE UPDATE ON ml_baselines
FOR EACH ROW EXECUTE FUNCTION set_updated_at();
