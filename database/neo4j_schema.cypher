CREATE CONSTRAINT organization_id IF NOT EXISTS
FOR (n:Organization) REQUIRE n.id IS UNIQUE;

CREATE CONSTRAINT endpoint_id IF NOT EXISTS
FOR (n:Endpoint) REQUIRE n.id IS UNIQUE;

CREATE CONSTRAINT user_id IF NOT EXISTS
FOR (n:User) REQUIRE n.id IS UNIQUE;

CREATE CONSTRAINT process_id IF NOT EXISTS
FOR (n:Process) REQUIRE n.id IS UNIQUE;

CREATE CONSTRAINT file_id IF NOT EXISTS
FOR (n:File) REQUIRE n.id IS UNIQUE;

CREATE CONSTRAINT domain_id IF NOT EXISTS
FOR (n:Domain) REQUIRE n.id IS UNIQUE;

CREATE CONSTRAINT ip_address_id IF NOT EXISTS
FOR (n:IPAddress) REQUIRE n.id IS UNIQUE;

CREATE CONSTRAINT alert_id IF NOT EXISTS
FOR (n:Alert) REQUIRE n.id IS UNIQUE;

CREATE CONSTRAINT incident_id IF NOT EXISTS
FOR (n:Incident) REQUIRE n.id IS UNIQUE;

CREATE INDEX endpoint_org IF NOT EXISTS
FOR (n:Endpoint) ON (n.organizationId);

CREATE INDEX user_org IF NOT EXISTS
FOR (n:User) ON (n.organizationId);

CREATE INDEX alert_org_time IF NOT EXISTS
FOR (n:Alert) ON (n.organizationId, n.lastSeenAt);

CREATE INDEX incident_org_time IF NOT EXISTS
FOR (n:Incident) ON (n.organizationId, n.openedAt);

CREATE INDEX process_org_name IF NOT EXISTS
FOR (n:Process) ON (n.organizationId, n.name);

CREATE INDEX domain_value IF NOT EXISTS
FOR (n:Domain) ON (n.value);

CREATE INDEX ip_value IF NOT EXISTS
FOR (n:IPAddress) ON (n.value);
