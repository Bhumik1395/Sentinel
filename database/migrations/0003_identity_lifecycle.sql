-- Phase 2 identity lifecycle. Safe upgrade is additive; rollback drops invitations and audit additions only.
SET search_path TO sentinel, public;
CREATE TABLE IF NOT EXISTS invitations (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES organizations(id) ON DELETE CASCADE,
  email varchar(320) NOT NULL, role user_role NOT NULL, invited_by uuid REFERENCES users(id) ON DELETE SET NULL,
  keycloak_user_id varchar(120), status varchar(20) NOT NULL DEFAULT 'PENDING' CHECK (status IN ('PENDING','ACCEPTED','CANCELLED','EXPIRED')),
  expires_at timestamptz NOT NULL, accepted_at timestamptz, created_at timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS idx_invitations_org_email ON invitations (organization_id, email) WHERE status = 'PENDING';
