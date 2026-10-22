# **Contents** 

|**Sentinel — Phase 2: Tenant Model Completion & Licensing**|**2**|
|---|---|
|0. What Phase 2 Actually Covers . . . . . . . . . . . . . . . . . . . . .|. . .<br>3|
|1. Add the Missing Folders . . . . . . . . . . . . . . . . . . . . . . . .|. . .<br>3|
|2. Add the Package You’ll Need<br>. . . . . . . . . . . . . . . . . . . . .|. . .<br>4|
|3. Migration — Already Covered by Phase 1’s Corrected Schema<br>. . .|. . .<br>4|
|4. A Real Database Access Layer . . . . . . . . . . . . . . . . . . . . .|. . .<br>5|
|5. Organization CRUD (Owner-Only) . . . . . . . . . . . . . . . . . . .|. . .<br>5|
|6. Support Team — Time-Boxed, Engagement-Scoped Access<br>. . . . .|. . .<br>11|
|7. Licensing & Endpoint Registration<br>. . . . . . . . . . . . . . . . . .|. . .<br>14|
|8. Harden the Isolation Middleware for Support Team . . . . . . . . .|. . .<br>19|
|9. Wire It All Into Program.cs . . . . . . . . . . . . . . . . . . . . . . .|. . .<br>22|
|10. Update the CI Workfow<br>. . . . . . . . . . . . . . . . . . . . . . .|. . .<br>22|
|11. Verifcation Checklist for Phase 2<br>. . . . . . . . . . . . . . . . . .|. . .<br>23|
|12. Deliberately Not in Phase 2<br>. . . . . . . . . . . . . . . . . . . . .|. . .<br>23|



1 

# **— Sentinel Phase 2: Tenant Model Completion & Licensing** 

**Patched.** This revision fixes real bugs found by tracing the actual call graph in Phase 3, not by re-reading this document’s prose: OrganizationsService.CreateOrganizationAsync referenced an organizations.type column and a keycloak_id column that don’t exist in the project’s canonical schema, and never populated the required slug column — meaning this method, as originally written, could not have succeeded against the real database even once. LicenseService.RegisterEndpointAsync only rejected SUSPENDED licenses, silently treating EXPIRED ones as active. Both fixed below. See the Sentinel Project Consistency Report for the full list this was drawn from. 

### **(Fitted to your actual project structure: frontend/, backend/, docs/)** 

Before anything else, two corrections to carry forward from Phase 1. 

First: the Phase 1 guide’s Keycloak Bootstrap section said the Owner “assigns its first Security Administrator” to a new organization. That line is a leftover from an earlier draft of the role model, before CSO existed as a role. The PRD/TRD/Implementation Plan you’re actually building against are unambiguous: the Owner assigns the first CSO, and the CSO — not the Owner — is who subsequently manages Security Administrator accounts. Phase 2’s Organization creation flow implements the corrected hierarchy. If you already manually created a test org in Keycloak with a securityadministrator as the “first” account, that account is fine to keep for testing, but don’t treat it as the pattern going forward. 

Second: Phase 1 never actually wired the backend to Supabase — UserManagementService.CreateUserAsync just returned Guid.NewGuid() without touching a database, and OrganizationContext only read JWT claims. That was fine for a scaffold. Phase 2 can’t get away with that anymore: Organization CRUD and Licensing both need real persisted state, so this phase’s first real piece of work is adding an actual database access layer. 

2 

## **0. What Phase 2 Actually Covers** 

Per the Implementation Plan’s build order (Section 2), Phase 1 covered item 1 (Infrastructure & Identity Foundation) and made a first pass at pieces of item 2 (RBAC authorization logic, without persistence). Phase 2 finishes item 2 and completes item 3: 

**In scope:** - Organization CRUD, Owner-only, backed by real Postgres persistence. - Correcting the onboarding-adjacent flow so Organization creation provisions the first CSO, not a Security Administrator. - Support Team’s time-boxed, engagement-scoped access — no SOAR authority, no unrestricted cross-org access, access scoped to an active, time-boxed engagement. - Hardening OrganizationIsolationMiddleware so it actually checks engagement state for Support Team requests, instead of only checking IsPlatformTier. - Licensing: licenses table, endpoint cap enforcement at registration time. 

**Explicitly deferred (do not build these yet):** - Manual Onboarding & Public Site (Implementation Plan item 4) — Organization creation in this phase is a direct Owner action via API, not the Apply →Review →Discussion →Approval pipeline. That pipeline is Phase 3’s job and calls into the same OrganizationsService this phase builds. - Real Keycloak user provisioning via the Admin REST API — still stubbed this phase, same pattern as Phase 1’s UserManagementService. Real HTTP calls land in Phase 3, bundled with onboarding. - Endpoint agent, telemetry ingestion, heartbeats — item 5 in the build order. This phase adds a minimal endpoints table and a registration endpoint purely so license-cap enforcement has something to enforce against — it is not the real Agent’s registration flow. - Renewal/Suspension enforcement behavior beyond the stated working assumption: suspension blocks new registrations only; existing endpoints keep reporting; dashboard remains read access. Don’t wire up telemetry-rejection logic now — there’s no telemetry pipeline yet to wire it into. 

## **1. Add the Missing Folders** 

- $ cd path/to/your/project/backend 

- $ mkdir -p src/Sentinel.Identity/Data 

- $ mkdir -p src/Sentinel.Identity/Organizations 

- $ mkdir -p src/Sentinel.Identity/SupportEngagements 

- $ mkdir -p src/Sentinel.Licensing 

- $ mkdir -p src/Sentinel.Api/Controllers 

Updated structure after this phase: 

backend/ ├──docker-compose.yml ├──.env ├──keycloak/ │ └──realm-export.json ├──migrations/ 

3 

│ └──001_full_schema_corrected.sql ←the one canonical schema (see Phase 1, Step 4) └──src/ ├──Sentinel.Api/ │ ├──Controllers/ ←new │ │ ├──OrganizationsController.cs │ │ ├──SupportEngagementsController.cs │ │ ├──LicensingController.cs │ │ └──EndpointsController.cs │ ├──OrganizationIsolationMiddleware.cs ←modified, Step 8 │ └──Program.cs ←modified, Step 9 ├──Sentinel.Identity/ │ ├──Data/ │ │ └──SentinelDataSource.cs ←new, Step 4 │ ├──Organizations/ ←new, Step 5 │ ├──SupportEngagements/ ←new, Step 6 │ └──(existing Phase 1 files, unchanged) └──Sentinel.Licensing/ ←new project, Step 7 

## **2. Add the Package You’ll Need** 

$ cd path/to/your/project/backend/src/Sentinel.Identity 

$ dotnet add package Npgsql --version 8.0.3 

$ cd path/to/your/project/backend/src/Sentinel.Api 

$ dotnet add package Npgsql --version 8.0.3 

Why Npgsql directly and not EF Core? Nothing in the PRD/TRD mandates an ORM, and introducing one now is a bigger architectural commitment than Phase 2 needs for a handful of simple tables. [Guessing] — if your team already has an EF Core preference, that’s a reasonable substitution, but make it consistently rather than mixing raw SQL and EF Core across phases. 

## **— 3. Migration Already Covered by Phase 1’s Corrected Schema** 

**This step is now a no-op.** Earlier drafts of this document had you write and run a separate 002_organizations_licensing.sql here, creating licenses, endpoints, and support_engagements. Don’t — Phase 1’s Step 4 already runs the corrected full schema, which includes all three tables with their actual column names (licenses.id as its own primary key, support_engagements.status/started_at/expires_at/ended_at, not a two-column starts_at/ends_at-only shape). If you’re picking up this document without having done Phase 1’s Step 4 yet, go do that first — there is nothing left for this phase to migrate. 

**How to know it worked:** Supabase Table Editor already shows licenses, endpoints, 

4 

and support_engagements from Phase 1’s setup — confirm they’re there before continuing, rather than assuming. 

## **4. A Real Database Access Layer** 

**Where:** backend/src/Sentinel.Identity/Data/SentinelDataSource.cs 

_// backend/src/Sentinel.Identity/Data/SentinelDataSource.cs_ **using** Npgsql **;** 

**namespace** Sentinel **.Identity.Data;** 

**public interface** ISentinelDataSource **{** NpgsqlConnection **CreateConnection(); } public class** SentinelDataSource **:** ISentinelDataSource **{ private readonly** string _connectionString **; public SentinelDataSource(** IConfiguration configuration **) {** _connectionString **=** configuration **.GetConnectionString(** "Supabase" **) ?? throw new InvalidOperationException(** "ConnectionStrings:Supabase is missing. Check ↪ appsettings.json / environment variables." **); }** 

**public** NpgsqlConnection **CreateConnection() => new(** _connectionString **); }** 

This is deliberately thin — one method, no connection pooling logic of its own, because Npgsql’s connection string already points at Supabase’s pooled connection (port 6543, per Phase 1 Step 2). Adding a second pooling layer on top of Supabase’s pooler would be redundant at best and would mask connection-cap issues at worst. 

## **5. Organization CRUD (Owner-Only)** 

**Where:** backend/src/Sentinel.Identity/Organizations/OrganizationModels.cs 

_// backend/src/Sentinel.Identity/Organizations/OrganizationModels.cs_ **namespace** Sentinel **.Identity.Organizations;** 

5 

_// FIXED: no more `Type` field — organizations.type doesn't exist in the // canonical schema. company_scope is a boolean, checked at the query level // (see ListOrganizationsAsync below), not surfaced as a string on this_ ↪ _record._ 

**public** record **Organization(** Guid Id **,** string Name **,** string Slug **,** 

- ↪ DateTimeOffset CreatedAt **);** 

**public** record **CreateOrganizationRequest(** string Name **,** string CsoEmail **,** int ↪ EndpointCap **);** 

**public** record **CreateOrganizationResult(** Guid OrganizationId **,** Guid CsoUserId **,** ↪ Guid LicenseId **,** string CsoTemporaryPassword **);** 

CsoTemporaryPassword is new this revision — it didn’t exist in the original Phase 2 design because nothing yet needed it. Phase 3’s real Keycloak provisioning generates one; this field is here now so Phase 3 doesn’t have to change this record’s shape later. If you’re implementing Phase 2 in isolation before Phase 3 exists, the stub provisioning service below can return a placeholder string for it. 

_// back-_ 

↪ _end/src/Sentinel.Identity/Organizations/IKeycloakUserProvisioningService.cs_ **namespace** Sentinel **.Identity.Organizations;** 

_///_ **<summary>** _/// Stubbed the same way Phase 1's UserManagementService was stubbed. Real /// Keycloak Admin REST API calls land in Phase 3 alongside onboarding. ///_ **</summary> public interface** IKeycloakUserProvisioningService **{** Task **<** Guid **> ProvisionUserAsync(** string email **,** string role **,** Guid ↪ organizationId **); }** 

**public class** StubKeycloakUserProvisioningService **:** ↪ IKeycloakUserProvisioningService **{ public** Task **<** Guid **> ProvisionUserAsync(** string email **,** string role **,** Guid ↪ organizationId **) =>** Task **.FromResult(** Guid **.NewGuid()); }** 

**Where:** backend/src/Sentinel.Identity/Organizations/OrganizationsService.cs — **this is the corrected version; the original had this method referencing columns that don’t exist and could never have succeeded.** 

_// backend/src/Sentinel.Identity/Organizations/OrganizationsService.cs_ **using** Npgsql **; using** Sentinel **.Identity.Data;** 

6 

#### **namespace** Sentinel **.Identity.Organizations;** 

**public interface** IOrganizationsService 

#### **{** 

Task **<** CreateOrganizationResult **>** 

↪ **CreateOrganizationAsync(** CreateOrganizationRequest request **);** Task **<** IReadOnlyList **<** Organization **>> ListOrganizationsAsync();** Task **<** Organization **?> GetOrganizationAsync(** Guid id **);** 

#### **}** 

**public class** OrganizationsService **:** IOrganizationsService **{** 

**private readonly** ISentinelDataSource _dataSource **; private readonly** IKeycloakUserProvisioningService _provisioning **;** 

**public OrganizationsService(** ISentinelDataSource dataSource **,** ↪ IKeycloakUserProvisioningService provisioning **) {** _dataSource **=** dataSource **;** _provisioning **=** provisioning **;** 

#### **}** 

**public** async Task **<** CreateOrganizationResult **>** 

↪ **CreateOrganizationAsync(** CreateOrganizationRequest request **) {** 

**if (** request **.EndpointCap <=** 0 **)** 

**throw new ArgumentException(** "endpointCap must be a positive ↪ integer." **);** 

await **using** var conn **=** _dataSource **.CreateConnection();** await conn **.OpenAsync();** await **using** var tx **=** await conn **.BeginTransactionAsync();** 

_// 1. Create the organization. FIXED: no `type` column — the_ ↪ _canonical // schema distinguishes Sentinel Company from customer orgs via the // company_scope boolean (defaults false, correct here), and_ ↪ _requires // a unique, NOT NULL slug this method must generate, not skip._ var orgId **=** Guid **.NewGuid();** var slug **= Slugify(** request **.Name);** await **using (** var cmd **= new NpgsqlCommand(** "INSERT INTO organizations (id, name, slug) VALUES (@id, @name, ↪ @slug)" **,** conn **,** tx **)) {** cmd **.Parameters.AddWithValue(** "id" **,** orgId **);** 

7 

cmd **.Parameters.AddWithValue(** "name" **,** request **.Name);** cmd **.Parameters.AddWithValue(** "slug" **,** slug **);** await cmd **.ExecuteNonQueryAsync(); }** 

_// 2. Provision the first CSO (Owner assigns first CSO, not a_ ↪ _Security // Administrator — see the correction note at the top of this doc). // FIXED: keycloak_user_id, not keycloak_id; display_name is NOT_ ↪ _NULL // in the canonical schema and was previously never populated._ var provisioned **=** await ↪ _provisioning **.ProvisionUserAsync(** request **.CsoEmail,** "cso" **,** ↪ orgId **);** var csoUserId **=** Guid **.NewGuid();** await **using (** var cmd **= new NpgsqlCommand(** 

@"INSERT INTO users (id, organization_id, keycloak_user_id, ↪ email, display_name, role) **VALUES (** @id **,** @orgId **,** @kcId **,** @email **,** @displayName **,** 'CSO' **)** ", ↪ conn, tx)) 

**{** cmd **.Parameters.AddWithValue(** "id" **,** csoUserId **);** cmd **.Parameters.AddWithValue(** "orgId" **,** orgId **);** cmd **.Parameters.AddWithValue(** "kcId" **,** provisioned **.ToString());** cmd **.Parameters.AddWithValue(** "email" **,** request **.CsoEmail);** _// Placeholder — the onboarding form should collect a real_ ↪ _contact // name and pass it through here. Not fixed in this phase;_ ↪ _flagged // so it doesn't look like an oversight nobody noticed._ cmd **.Parameters.AddWithValue(** "displayName" **,** ↪ request **.CsoEmail.Split(** '@' **)[** 0 **]);** await cmd **.ExecuteNonQueryAsync();** 

**}** 

_// 3. Assign the license._ var licenseId **=** Guid **.NewGuid();** await **using (** var cmd **= new NpgsqlCommand(** @"INSERT INTO licenses (id, organization_id, endpoint_cap) **VALUES (** @id **,** @orgId **,** @cap **)** ", conn, tx)) 

**{** cmd **.Parameters.AddWithValue(** "id" **,** licenseId **);** cmd **.Parameters.AddWithValue(** "orgId" **,** orgId **);** cmd **.Parameters.AddWithValue(** "cap" **,** request **.EndpointCap);** await cmd **.ExecuteNonQueryAsync(); }** 

8 

#### await tx **.CommitAsync();** 

- _// Placeholder password when using the stub provisioning service — // Phase 3's real KeycloakUserProvisioningService returns an actual // generated temporary password instead of this string._ **return new CreateOrganizationResult(** orgId **,** csoUserId **,** licenseId **,** ↪ "set-by-phase-3-provisioning" **);** 

**}** 

**public** async Task **<** IReadOnlyList **<** Organization **>> ListOrganizationsAsync() {** await **using** var conn **=** _dataSource **.CreateConnection();** await conn **.OpenAsync();** 

_// FIXED: filters on company_scope = false, not a `type` column that // doesn't exist — customer organizations are the ones NOT flagged // as the platform-level Sentinel Company row._ await **using** var cmd **= new NpgsqlCommand(** 

"SELECT id, name, slug, created_at FROM organizations WHERE ↪ company_scope = false ORDER BY created_at DESC" **,** conn **);** 

await **using** var reader **=** await cmd **.ExecuteReaderAsync();** var results **= new** List **<** Organization **>(); while (** await reader **.ReadAsync()) {** 

results **.Add(new Organization(** 

reader **.GetGuid(** 0 **),** reader **.GetString(** 1 **),** reader **.GetString(** 2 **),** reader **.GetFieldValue<** DateTimeOffset **>(** 3 **)));** 

**}** 

**return** results **; }** 

**public** async Task **<** Organization **?> GetOrganizationAsync(** Guid id **) {** 

await **using** var conn **=** _dataSource **.CreateConnection();** await conn **.OpenAsync();** await **using** var cmd **= new NpgsqlCommand(** 

"SELECT id, name, slug, created_at FROM organizations WHERE id = ↪ @id" **,** conn **);** cmd **.Parameters.AddWithValue(** "id" **,** id **);** await **using** var reader **=** await cmd **.ExecuteReaderAsync(); if (!** await reader **.ReadAsync()) return null; return new Organization(** reader **.GetGuid(** 0 **),** reader **.GetString(** 1 **),** reader **.GetString(** 2 **),** reader **.GetFieldValue<** DateTimeOffset **>(** 3 **));** 

**}** 

_// ponytail: simple kebab-case + short random suffix, no external slug_ 

9 

_// library needed for four inserts a week._ **private static** string **Slugify(** string name **) =>** $"{new string(name.ToLowerInvariant().Where(c => ↪ char.IsLetterOrDigit(c) || c == ' ').ToArray()) **.Trim().Replace(** ' ' **,** '-' **)}-{** Guid **.NewGuid().ToString(** "N" **)[..6]}** "; 

**}** 

Note the transaction wrapping all three inserts: organization + first CSO + license are created atomically — an organization that exists in Postgres but has no license row is an invalid state this phase makes structurally hard to reach, not just avoided by convention. 

**Where:** backend/src/Sentinel.Api/Controllers/OrganizationsController.cs 

_// backend/src/Sentinel.Api/Controllers/OrganizationsController.cs_ **using** Microsoft **.AspNetCore.Authorization; using** Microsoft **.AspNetCore.Mvc; using** Sentinel **.Identity.Organizations;** 

**namespace** Sentinel **.Api.Controllers;** 

**[** ApiController **] [Route(** "organizations" **)] [Authorize(** Policy **=** "OwnerOnly" **)] public class** OrganizationsController **:** ControllerBase **{** 

**private readonly** IOrganizationsService _organizations **; public OrganizationsController(** IOrganizationsService organizations **) =>** ↪ _organizations **=** organizations **;** 

**[** HttpPost **]** 

**public** async Task **<** IActionResult **> Create([** FromBody **]** ↪ CreateOrganizationRequest request **) {** 

var result **=** await _organizations **.CreateOrganizationAsync(** request **); return CreatedAtAction(nameof(** Get **), new {** id **=** 

↪ result **.OrganizationId },** result **); }** 

**[** HttpGet **]** 

**public** async Task **<** IActionResult **> List() => Ok(** await ↪ _organizations **.ListOrganizationsAsync());** 

**[HttpGet(** "{id:guid}" **)] public** async Task **<** IActionResult **> Get(** Guid id **) {** 

var org **=** await _organizations **.GetOrganizationAsync(** id **); return** org **is null ? NotFound() : Ok(** org **);** 

10 

**}** 

**}** 

[Authorize(Policy = "OwnerOnly")] reuses the policy Phase 1 already registered — nothing new to wire there. 

## **— 6. Support Team Time-Boxed, Engagement-Scoped Access** 

**Where:** backend/src/Sentinel.Identity/SupportEngagements/SupportEngagementModels.cs 

_// back-_ 

↪ _end/src/Sentinel.Identity/SupportEngagements/SupportEngagementModels.cs_ **namespace** Sentinel **.Identity.SupportEngagements;** 

**public** record **StartEngagementRequest(** Guid OrganizationId **,** Guid ↪ SupportUserId **,** string Reason **,** int DurationHours **); public** record **SupportEngagement(** 

Guid Id **,** Guid OrganizationId **,** Guid SupportUserId **,** string Reason **,** string Status **,** DateTimeOffset StartedAt **,** DateTimeOffset ExpiresAt **);** 

**Where:** backend/src/Sentinel.Identity/SupportEngagements/SupportEngagementService.cs 

_// back-_ 

↪ _end/src/Sentinel.Identity/SupportEngagements/SupportEngagementService.cs_ **using** Npgsql **; using** Sentinel **.Identity.Data;** 

**namespace** Sentinel **.Identity.SupportEngagements;** 

**public interface** ISupportEngagementService 

**{** 

Task **<** Guid **> StartEngagementAsync(** StartEngagementRequest request **);** Task **EndEngagementAsync(** Guid engagementId **);** 

Task **<** bool **> HasActiveEngagementAsync(** Guid supportUserId **,** Guid ↪ organizationId **);** 

**}** 

**public class** SupportEngagementService **:** ISupportEngagementService **{** 

**private readonly** ISentinelDataSource _dataSource **; public SupportEngagementService(** ISentinelDataSource dataSource **) =>** ↪ _dataSource **=** dataSource **;** 

**public** async Task **<** Guid **> StartEngagementAsync(** StartEngagementRequest 

↪ request **)** 

11 

**{ if (** request **.DurationHours is <=** 0 or **>** 72 **) throw new ArgumentException(** "Engagement duration must be between 1 and 72 hours. PRD/TRD ↪ do not specify a " **+** "maximum — 72h is a TRD-unstated but reasonable default ↪ guard; revisit if product " **+** "specifies otherwise." **);** await **using** var conn **=** _dataSource **.CreateConnection();** await conn **.OpenAsync();** var id **=** Guid **.NewGuid();** await **using** var cmd **= new NpgsqlCommand(** @"INSERT INTO support_engagements **(** id **,** organization_id **,** support_user_id **,** reason **,** expires_at **) VALUES (** @id **,** @orgId **,** @userId **,** @reason **, now() + (** @hours **||** ' ↪ hours' **)::** interval **)** ", conn); cmd **.Parameters.AddWithValue(** "id" **,** id **);** cmd **.Parameters.AddWithValue(** "orgId" **,** request **.OrganizationId);** cmd **.Parameters.AddWithValue(** "userId" **,** request **.SupportUserId);** cmd **.Parameters.AddWithValue(** "reason" **,** request **.Reason);** cmd **.Parameters.AddWithValue(** "hours" **,** request **.DurationHours);** await cmd **.ExecuteNonQueryAsync(); return** id **;** 

**}** 

**public** async Task **EndEngagementAsync(** Guid engagementId **) {** await **using** var conn **=** _dataSource **.CreateConnection();** await conn **.OpenAsync();** await **using** var cmd **= new NpgsqlCommand(** "UPDATE support_engagements SET status = 'ENDED', ended_at = ↪ now() WHERE id = @id AND status = 'ACTIVE'" **,** conn **);** cmd **.Parameters.AddWithValue(** "id" **,** engagementId **);** await cmd **.ExecuteNonQueryAsync(); }** 

**public** async Task **<** bool **> HasActiveEngagementAsync(** Guid supportUserId **,** ↪ Guid organizationId **) {** await **using** var conn **=** _dataSource **.CreateConnection();** await conn **.OpenAsync();** await **using** var cmd **= new NpgsqlCommand(** @"SELECT 1 FROM support_engagements 

WHERE support_user_id **=** @userId AND organization_id **=** @orgId AND status **=** 'ACTIVE' AND expires_at **> now()** 

12 

LIMIT 1", conn); cmd **.Parameters.AddWithValue(** "userId" **,** supportUserId **);** cmd **.Parameters.AddWithValue(** "orgId" **,** organizationId **);** var result **=** await cmd **.ExecuteScalarAsync(); return** result **is** not **null;** 

**}** 

**}** 

Two things worth flagging, not burying: 

1. Expiry is checked by comparing expires_at > now() at read time, not by a background job that flips status to ENDED. A row can sit at status = 'ACTIVE' forever after it expires and still be correctly denied, because the time check is part of the same query. If you later build a Support Queue UI listing “active engagements,” it must apply the same expires_at > now() filter — a naive WHERE status = 'ACTIVE' query there would show expired-but-not-cleaned-up engagements as if they were live. 

2. Who is allowed to call StartEngagementAsync? Neither the PRD nor TRD specifies whether Support Team members self-initiate engagements or an Owner has to approve them. [Guessing] — this phase does not add an authorization policy restricting who can call this beyond requiring authentication as support-team or owner. Flag this to product before Support Queue UI work begins. 

**Where:** backend/src/Sentinel.Api/Controllers/SupportEngagementsController.cs 

_// backend/src/Sentinel.Api/Controllers/SupportEngagementsController.cs_ **using** Microsoft **.AspNetCore.Authorization; using** Microsoft **.AspNetCore.Mvc; using** Sentinel **.Identity.SupportEngagements;** 

**namespace** Sentinel **.Api.Controllers;** 

**[** ApiController **] [Route(** "support-engagements" **)] [Authorize(** Policy **=** "SentinelCompany" **)] public class** SupportEngagementsController **:** ControllerBase 

**{** 

**private readonly** ISupportEngagementService _engagements **; public SupportEngagementsController(** ISupportEngagementService ↪ engagements **) =>** _engagements **=** engagements **;** 

**[** HttpPost **] public** async Task **<** IActionResult **> Start([** FromBody **]** ↪ StartEngagementRequest request **)** 

**{** 

var id **=** await _engagements **.StartEngagementAsync(** request **); return Ok(new {** engagementId **=** id **}); }** 

13 

**[HttpPost(** "{id:guid}/end" **)] public** async Task **<** IActionResult **> End(** Guid id **) {** await _engagements **.EndEngagementAsync(** id **); return NoContent(); } }** 

SentinelCompany is the policy Phase 1 already defined (owner, support-team) — reused as-is. 

## **7. Licensing & Endpoint Registration** 

- $ cd path/to/your/project/backend 

- $ dotnet new classlib -n Sentinel.Licensing -o src/Sentinel.Licensing 

- $ dotnet add src/Sentinel.Licensing package Npgsql --version 8.0.3 

- $ dotnet add src/Sentinel.Licensing reference src/Sentinel.Identity 

- $ dotnet add src/Sentinel.Api reference src/Sentinel.Licensing 

- $ dotnet sln add src/Sentinel.Licensing 

That last command matters — skip it and dotnet build backend/Sentinel.sln (which CI calls) will silently not build this project at all. 

**Where:** backend/src/Sentinel.Licensing/LicenseModels.cs 

_// backend/src/Sentinel.Licensing/LicenseModels.cs_ **namespace** Sentinel **.Licensing;** 

**public** record **License(** 

Guid Id **,** Guid OrganizationId **,** int EndpointCap **,** int 

↪ CurrentEndpointCount **,** string Status **); public** record **RegisterEndpointRequest(** Guid OrganizationId **,** string 

- ↪ Hostname **);** 

**public enum** RegistrationOutcome **{** Registered **,** CapReached **,** LicenseSuspended ↪ **}** 

**public** record **RegisterEndpointResult(** RegistrationOutcome Outcome **,** Guid **?** ↪ EndpointId **);** 

**Where:** backend/src/Sentinel.Licensing/LicenseService.cs — **RegisterEndpointAsync’s suspension check is corrected in this revision; see the note inline.** 

_// backend/src/Sentinel.Licensing/LicenseService.cs_ **using** Npgsql **; using** Sentinel **.Identity.Data;** 

14 

**namespace** Sentinel **.Licensing;** 

**public interface** ILicenseService **{** 

Task **<** License **?> GetLicenseAsync(** Guid organizationId **);** Task **UpdateEndpointCapAsync(** Guid organizationId **,** int newCap **);** Task **<** RegisterEndpointResult **>** ↪ **RegisterEndpointAsync(** RegisterEndpointRequest request **);** 

**}** 

**public class** LicenseService **:** ILicenseService **{ private readonly** ISentinelDataSource _dataSource **; public LicenseService(** ISentinelDataSource dataSource **) =>** _dataSource **=** ↪ dataSource **; public** async Task **<** License **?> GetLicenseAsync(** Guid organizationId **) {** await **using** var conn **=** _dataSource **.CreateConnection();** await conn **.OpenAsync();** await **using** var cmd **= new NpgsqlCommand(** @"SELECT id, organization_id, endpoint_cap, ↪ current_endpoint_count, status::text FROM licenses WHERE organization_id **=** @orgId", conn); cmd **.Parameters.AddWithValue(** "orgId" **,** organizationId **);** await **using** var reader **=** await cmd **.ExecuteReaderAsync(); if (!** await reader **.ReadAsync()) return null; return new License(** reader **.GetGuid(** 0 **),** reader **.GetGuid(** 1 **),** reader **.GetInt32(** 2 **),** ↪ reader **.GetInt32(** 3 **),** reader **.GetString(** 4 **)); }** 

**public** async Task **UpdateEndpointCapAsync(** Guid organizationId **,** int ↪ newCap **) { if (** newCap **<=** 0 **) throw new ArgumentException(** "endpointCap must be a ↪ positive integer." **);** var existing **=** await **GetLicenseAsync(** organizationId **) ?? throw new InvalidOperationException(** "No license exists for ↪ this organization." **); if (** newCap **<** existing **.CurrentEndpointCount) throw new InvalidOperationException(** $"Cannot set endpoint cap to {newCap}: organization ↪ currently has " **+** $"{existing.CurrentEndpointCount} registered endpoints." **);** await **using** var conn **=** _dataSource **.CreateConnection();** 

15 

await conn **.OpenAsync();** await **using** var cmd **= new NpgsqlCommand(** 

"UPDATE licenses SET endpoint_cap = @cap WHERE organization_id = ↪ @orgId" **,** conn **);** cmd **.Parameters.AddWithValue(** "cap" **,** newCap **);** cmd **.Parameters.AddWithValue(** "orgId" **,** organizationId **);** await cmd **.ExecuteNonQueryAsync();** 

**}** 

**public** async Task **<** RegisterEndpointResult **>** ↪ **RegisterEndpointAsync(** RegisterEndpointRequest request **) {** 

await **using** var conn **=** _dataSource **.CreateConnection();** await conn **.OpenAsync();** await **using** var tx **=** await conn **.BeginTransactionAsync();** 

_// Row lock for the duration of the transaction so two concurrent // registrations against the same org can't both pass the cap check // before either increments current_endpoint_count._ await **using** var lockCmd **= new NpgsqlCommand(** 

@"SELECT endpoint_cap, current_endpoint_count, status::text FROM licenses WHERE organization_id **=** @orgId FOR UPDATE", ↪ conn, tx); lockCmd **.Parameters.AddWithValue(** "orgId" **,** request **.OrganizationId);** await **using** var reader **=** await lockCmd **.ExecuteReaderAsync(); if (!** await reader **.ReadAsync())** 

**throw new InvalidOperationException(** "No license exists for this ↪ organization." **);** var cap **=** reader **.GetInt32(** 0 **);** var current **=** reader **.GetInt32(** 1 **);** var status **=** reader **.GetString(** 2 **);** await reader **.CloseAsync();** 

_// FIXED: the canonical schema's license_status enum has three values // (ACTIVE / SUSPENDED / EXPIRED), not two. This check previously_ ↪ _only // matched "SUSPENDED" — an EXPIRED license was silently treated as // active and allowed to keep registering new endpoints._ **if (** status **is** "SUSPENDED" or "EXPIRED" **) {** 

await tx **.RollbackAsync(); return new RegisterEndpointResult(** RegistrationOutcome **.** ⌋ ↪ **LicenseSuspended,** ↪ **null);** 

**}** 

**if (** current **>=** cap **)** 

16 

**{** await tx **.RollbackAsync(); return new** ↪ **RegisterEndpointResult(** RegistrationOutcome **.CapReached,** ↪ **null); }** var endpointId **=** Guid **.NewGuid();** await **using (** var insertCmd **= new NpgsqlCommand(** "INSERT INTO endpoints (id, organization_id, hostname) VALUES ↪ (@id, @orgId, @hostname)" **,** conn **,** tx **)) {** insertCmd **.Parameters.AddWithValue(** "id" **,** endpointId **);** insertCmd **.Parameters.AddWithValue(** "orgId" **,** ↪ request **.OrganizationId);** insertCmd **.Parameters.AddWithValue(** "hostname" **,** request **.Hostname);** await insertCmd **.ExecuteNonQueryAsync(); }** await **using (** var updateCmd **= new NpgsqlCommand(** "UPDATE licenses SET current_endpoint_count = ↪ current_endpoint_count + 1 WHERE organization_id = @orgId" **,** conn **,** tx **)) {** updateCmd **.Parameters.AddWithValue(** "orgId" **,** ↪ request **.OrganizationId);** await updateCmd **.ExecuteNonQueryAsync(); }** await tx **.CommitAsync(); return new RegisterEndpointResult(** RegistrationOutcome **.Registered,** ↪ endpointId **); } }** 

**Where:** backend/src/Sentinel.Api/Controllers/LicensingController.cs 

_// backend/src/Sentinel.Api/Controllers/LicensingController.cs_ **using** Microsoft **.AspNetCore.Authorization; using** Microsoft **.AspNetCore.Mvc; using** Sentinel **.Licensing;** 

**namespace** Sentinel **.Api.Controllers;** 

**[** ApiController **] [Route(** "licensing" **)] [Authorize(** Policy **=** "OwnerOnly" **)]** 

17 

**public class** LicensingController **:** ControllerBase **{** 

**private readonly** ILicenseService _licenses **; public LicensingController(** ILicenseService licenses **) =>** _licenses **=** ↪ licenses **;** 

**[HttpGet(** "{orgId:guid}" **)] public** async Task **<** IActionResult **> Get(** Guid orgId **) {** 

var license **=** await _licenses **.GetLicenseAsync(** orgId **); return** license **is null ? NotFound() : Ok(** license **);** 

**}** 

**[HttpPut(** "{orgId:guid}" **)] public** async Task **<** IActionResult **> UpdateCap(** Guid orgId **, [** FromBody **]** int ↪ endpointCap **) {** await _licenses **.UpdateEndpointCapAsync(** orgId **,** endpointCap **); return NoContent();** 

**} }** 

**Where:** backend/src/Sentinel.Api/Controllers/EndpointsController.cs 

_// backend/src/Sentinel.Api/Controllers/EndpointsController.cs_ **using** Microsoft **.AspNetCore.Mvc; using** Sentinel **.Licensing;** 

**namespace** Sentinel **.Api.Controllers;** 

_// No [Authorize] here on purpose — real endpoint registration_ 

↪ _(Implementation // Plan item 5) will be agent-cert-authenticated, not JWT-authenticated._ ↪ _This // controller exists only to validate license-cap enforcement ahead of the_ ↪ _real // agent existing. Do not expose this route outside a controlled test // environment before Phase 5 replaces it with cert-based auth._ **[** ApiController **]** 

**[Route(** "endpoints" **)] public class** EndpointsController **:** ControllerBase **{** 

**private readonly** ILicenseService _licenses **; public EndpointsController(** ILicenseService licenses **) =>** _licenses **=** ↪ licenses **;** 

**[HttpPost(** "register" **)]** 

18 

**public** async Task **<** IActionResult **> Register([** FromBody **]** 

- ↪ RegisterEndpointRequest request **)** 

**{** 

var result **=** await _licenses **.RegisterEndpointAsync(** request **); return** result **.Outcome switch** 

**{** 

RegistrationOutcome **.Registered => Ok(new {** endpointId **=** ↪ result **.EndpointId }),** 

RegistrationOutcome **.CapReached => Conflict(new {** error **=** 

↪ "Endpoint cap reached for this organization." **}),** RegistrationOutcome **.LicenseSuspended => Conflict(new {** error **=** ↪ "Organization license is suspended or expired." **}),** _ **=> StatusCode(** 500 **),** 

**};** 

**}** 

**}** 

## **8. Harden the Isolation Middleware for Support Team** 

Phase 1’s middleware let any IsPlatformTier request through unconditionally — correct for Owner, always too permissive for Support Team. 

**Where:** backend/src/Sentinel.Api/OrganizationIsolationMiddleware.cs — replace the whole file: 

_// backend/src/Sentinel.Api/OrganizationIsolationMiddleware.cs_ **using** Sentinel **.Identity; using** Sentinel **.Identity.SupportEngagements;** 

**namespace** Sentinel **.Api;** 

**public class** OrganizationIsolationMiddleware **{** 

**private readonly** RequestDelegate _next **;** 

**public OrganizationIsolationMiddleware(** RequestDelegate next **) =>** _next **=** ↪ next **;** 

**public** async Task **InvokeAsync(** 

HttpContext context **,** IOrganizationContext orgContext **,** 

- ↪ ISupportEngagementService engagements **)** 

**{** 

**if (!** context **.User.Identity?.IsAuthenticated ?? true) {** 

await **_next(** context **); return;** 

19 

#### **}** 

**if (** orgContext **.Role ==** "owner" **) {** await **_next(** context **); return; }** 

var hasRouteOrgId **=** context **.Request.RouteValues.TryGetValue(** "orgId" **,** ↪ **out** var routeOrgIdRaw **) &&** Guid **.TryParse(** routeOrgIdRaw **?.ToString(), out** var routeOrgId **);** 

**if (** orgContext **.Role ==** "support-team" **) {** 

**if (!** hasRouteOrgId **) {** await **_next(** context **); return; }** 

_//_ NOTE _: this still assumes orgContext.UserId is directly usable. // Phase 3, Step 8 replaces this with a proper keycloak_user_id_ ↪ _-> // users.id lookup — see that document, not this one, for the_ ↪ _fix. // Left as-is here rather than silently duplicated across two_ ↪ _docs._ var supportUserId **=** orgContext **.UserId** 

**?? throw new UnauthorizedAccessException(** "Token contains no ↪ resolvable user identity." **);** var active **=** await 

↪ engagements **.HasActiveEngagementAsync(** supportUserId **,** ↪ routeOrgId **!.Value); if (!** active **) {** 

context **.Response.StatusCode =** StatusCodes **.Status403Forbidden;** await context **.Response.WriteAsync(** "No active support ↪ engagement for this organization." **); return; }** await **_next(** context **); return;** 

**}** 

_// CSO / Security Administrator / Security Analyst — unchanged Phase_ ↪ _1 behavior._ **if (** orgContext **.OrganizationId is null)** 

20 

**{** context **.Response.StatusCode =** StatusCodes **.Status403Forbidden;** await context **.Response.WriteAsync(** "No organization scope on ↪ token." **); return; }** 

**if (** hasRouteOrgId **&&** routeOrgId **!=** orgContext **.OrganizationId) {** context **.Response.StatusCode =** StatusCodes **.Status403Forbidden;** await context **.Response.WriteAsync(** "Cross-organization access ↪ denied." **); return; }** await **_next(** context **); } }** 

This introduces orgContext.UserId, which Phase 1’s IOrganizationContext doesn’t have. Add it: 

**Where:** backend/src/Sentinel.Identity/IOrganizationContext.cs — add one property and a few lines: 

_// Add to the interface:_ Guid **?** UserId **{ get; }** 

_// Add to OrganizationContext, alongside the existing OrganizationId/Role_ ↪ _parsing:_ **public** Guid **?** UserId **{ get; }** 

_// Inside the constructor, alongside the existing Role/OrganizationId_ ↪ _assignment:_ var subClaim **=** user **.FindFirst(** "sub" **)?.Value ??** user **.FindFirst(** System **.Security.Claims.ClaimTypes.NameIdentifier)?.Value;** UserId **=** string **.IsNullOrEmpty(** subClaim **) ? null :** Guid **.TryParse(** subClaim **,** ↪ **out** var parsed **) ?** parsed **: null;** 

[Guessing] — this assumes the Keycloak sub claim can be parsed directly as the internal users.id GUID. It can’t, in fact — sub is Keycloak’s own internal ID, stored as users.keycloak_user_id, a different column from users.id. This was flagged as an open guess here specifically so it wouldn’t get load-bearing before it was fixed; Phase 3, Step 8 is where it actually gets fixed. Don’t “fix” it here too — one canonical fix, in one place, is safer than the same fix drifting slightly differently in two documents. 

21 

## **9. Wire It All Into Program.cs** 

**Where:** backend/src/Sentinel.Api/Program.cs — additions on top of Phase 1’s existing lines: 

**using** Sentinel **.Identity.Data; using** Sentinel **.Identity.Organizations; using** Sentinel **.Identity.SupportEngagements; using** Sentinel **.Licensing;** 

_// ... existing builder setup from Phase 1 ..._ 

builder **.Services.AddSingleton<** ISentinelDataSource **,** SentinelDataSource **>();** builder **.Services.AddScoped<** IOrganizationsService **,** OrganizationsService **>();** builder **.Services.AddScoped<** IKeycloakUserProvisioningService **,** ↪ StubKeycloakUserProvisioningService **>();** builder **.Services.AddScoped<** ISupportEngagementService **,** ↪ SupportEngagementService **>();** builder **.Services.AddScoped<** ILicenseService **,** LicenseService **>();** 

_// ... existing AddControllers / app.Build() / middleware pipeline from_ ↪ _Phase 1 ..._ 

No changes to the middleware pipeline ordering — OrganizationIsolationMiddleware already runs after UseAuthentication() and before UseAuthorization(), which is required for it to have both a parsed JWT and a chance to reject before controllerlevel policies even evaluate. 

## **10. Update the CI Workflow** 

**Where:** .github/workflows/ci.yml — no path changes needed; dotnet build backend/Sentinel.sln picks up the new Sentinel.Licensing project automatically once you’ve run dotnet sln add in Step 7. Confirm by opening backend/Sentinel.sln in a text editor — it should list Sentinel.Api, Sentinel.Identity, and Sentinel.Licensing, not two projects. 

If backend/Sentinel.sln doesn’t exist yet at all: 

- $ cd path/to/your/project/backend 

- $ dotnet new sln -n Sentinel 

- $ dotnet sln add src/Sentinel.Api src/Sentinel.Identity src/Sentinel.Licensing 

This is worth checking explicitly — if it was silently missing since Phase 1, your CI workflow has been failing on every push, or has never actually built anything. 

22 

## **11. Verification Checklist for Phase 2** 

- licenses, endpoints, and support_engagements exist in Supabase (confirmed already present from Phase 1, Step 4 — not created by this phase). 

- dotnet build backend/Sentinel.sln succeeds, and Sentinel.Licensing appears in the build output. 

- As an owner token: POST /organizations with a name, CSO email, and endpoint cap returns 201 with an organizationId, csoUserId, and licenseId — and this now actually succeeds against the real schema, which it could not have done before this revision’s fix. 

- The new organization’s row in Postgres has a non-null, unique slug. 

- The same POST /organizations request, retried with a security-administrator or cso token instead, is rejected with 403. 

- GET /licensing/{orgId} (as Owner) shows currentEndpointCount: 0 and the cap you set at creation. 

- POST /endpoints/register against that org, repeated past the endpoint cap, returns 409 Conflict on the request that would exceed it. 

- Manually flip a license’s status to SUSPENDED in Supabase, retry POST /endpoints/register — rejected. **New:** manually flip it to EXPIRED instead and retry — also rejected, with the same error. Before this revision’s fix, EXPIRED would have silently succeeded. 

- Create a support_engagements row manually with expires_at in the future for a test support-team user and organization; a request from that user’s token to an org-scoped route for that organization succeeds. 

- Set that same row’s expires_at to a past timestamp and retry — rejected with 403, without needing to manually flip status to ENDED first. 

- A support-team token with no engagement row at all for a given organization is rejected on the first attempt. 

## **12. Deliberately Not in Phase 2** 

No Keycloak Admin REST API integration (still stubbed — real provisioning is Phase 3, alongside onboarding). No public Apply/Review/Discussion/Approval pipeline. No real Agent, no telemetry, no heartbeats — POST /endpoints/register exists solely to give licensing something to enforce against. No Renewal logic beyond “reject new registrations” on SUSPENDED/EXPIRED. No SOAR, no approval_policy column, no detection engine, no frontend work. 

23 

