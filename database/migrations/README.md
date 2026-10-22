# Database migrations

`database/schema.sql` is the clean-install reference. Production upgrades are applied once,
in lexical order, from this directory and recorded in `sentinel.schema_migrations` with a
SHA-256 checksum. Never edit an applied migration; add the next numbered file instead.

Each migration must state its safe rollback procedure in a leading SQL comment. Back up the
database and test both upgrade and rollback against a production-like copy before release.
