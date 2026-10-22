-- Phase 0 migration foundation. Apply before later versioned migrations.
-- Safe upgrade: this statement is idempotent. Rollback: DROP TABLE sentinel.schema_migrations;
CREATE SCHEMA IF NOT EXISTS sentinel;

CREATE TABLE IF NOT EXISTS sentinel.schema_migrations (
  version varchar(64) PRIMARY KEY,
  applied_at timestamptz NOT NULL DEFAULT now(),
  checksum varchar(128) NOT NULL
);
