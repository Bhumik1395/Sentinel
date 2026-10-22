# **Contents** 

|**Sentinel — Phase 3: Manual Onboarding, Desktop Application & Public**|
|---|
|**Website**<br>**2**|
|0. What Phase 3 Actually Covers . . . . . . . . . . . . . . . . . . . . . . . .<br>3|
|1. Add the Missing Folders . . . . . . . . . . . . . . . . . . . . . . . . . . .<br>4|
|2. Migration 003 — Applications . . . . . . . . . . . . . . . . . . . . . . . .<br>5|
|3. Real Keycloak Admin API Client . . . . . . . . . . . . . . . . . . . . . . .<br>6|
|4. Email Delivery<br>. . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . .<br>9|
|5. Onboarding / Applications Service<br>. . . . . . . . . . . . . . . . . . . . .<br>12|
|6. Applications Controller & Rate Limiting<br>. . . . . . . . . . . . . . . . . .<br>14|
|7. User Invitation Flow<br>. . . . . . . . . . . . . . . . . . . . . . . . . . . . .<br>14|
|8. Fix the Phase 2 sub / users.id TODO . . . . . . . . . . . . . . . . . . . .<br>17|
|9. Wire It All Into Program.cs . . . . . . . . . . . . . . . . . . . . . . . . . .<br>18|
|10. Update the CI Workfow<br>. . . . . . . . . . . . . . . . . . . . . . . . . .<br>18|
|11. Public Website . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . .<br>19|
|12. Sentinel Desktop Application (Electron + React) . . . . . . . . . . . . .<br>20|
|13. End-to-End Flows (Documentation Only — Not Implemented This Phase)<br>25|
|14. Verifcation Checklist for Phase 3<br>. . . . . . . . . . . . . . . . . . . . .<br>26|
|15. Deliberately Not in Phase 3<br>. . . . . . . . . . . . . . . . . . . . . . . .<br>27|



1 

# **— Sentinel Phase 3: Manual Onboarding, Desktop Application & Public Website** 

**Patched.** Installer naming standardized to SentinelAgent.msi (Windows Service, protected endpoints) and SentinelConsole.msi (Electron desktop app, analyst/CSO/admin workstations) throughout — matches the naming used in the Sentinel Project Consistency Report. ApplicationsService’s column reference fixed to created_organization_id (Step 5). CanManageUser authorization policy is now correctly registered — see Phase 1’s patched Program.cs; the invite flow in Step 7 depends on that fix and would have thrown a 500 on every call before it. **Confirmed, not changed:** Security Administrator manages policies; CSO does not. The invite flow below only governs account-creation hierarchy (who can invite whom) and says nothing about policy management — flagged explicitly here since an earlier architecture draft briefly suggested otherwise before that was corrected. 

### **(Fitted to your actual project structure: backend/, frontend/, desktop/, docs/)** 

Before anything else — six things this document has to actually resolve, not just acknowledge. Three carried forward from earlier phases, three new from this revision’s architecture change. 

**One — Keycloak Groups vs. flat attribute (carried forward).** TRD §22 describes Organizations as Keycloak Groups; nothing built so far ever created one. This document keeps the flat organizationId user-attribute model from Phase 1. Still true, still a documented TRD-wording deviation, not a functional gap. 

**Two — the Phase 2 sub / users.id TODO (carried forward, fixed in Step 8).** The JWT sub claim is Keycloak’s internal user ID, stored as users.keycloak_user_id — a different column from users.id. Fixed properly this phase. 

**Three — the previous revision’s “merge public site and dashboard into one React web app” is superseded, and that’s a genuine improvement, not a reversal for its own sake.** This document’s prior version put the authenticated dashboard inside the same browser app as the marketing site, specifically to share one deploy. 

2 

That coupling risk goes away here for free: the dashboard is now a **desktop application** , and a website and an installer are different distribution artifacts by construction — you cannot accidentally ship a marketing typo fix through the same pipeline as a dashboard release when there is no shared pipeline. The public site (Step 11) goes back to being small, static, and auth-free. 

**Four — SMTP is no longer deferred; it is now load-bearing.** Earlier phases explicitly deferred SMTP as a stated, deliberate gap, with the temporary password returned directly to the Owner as the fallback delivery path. The new onboarding flow requires a CSO to receive a real email (credentials, activation, download link) before they can do anything — so this phase builds real email delivery (Step 4). The old fallback doesn’t disappear; it becomes the answer to “what happens when the email send fails” (see Step 5), not “what happens because we haven’t built email yet.” 

**Five — desktop OAuth must use the system browser + a loopback redirect, never an embedded browser/webview.** Nothing in the architecture note specified this, and it’s the one place a reasonable-looking implementation is actually a known anti-pattern: an embedded BrowserWindow showing Keycloak’s login form inside the Electron app itself cannot be verified by the user as genuinely Keycloak, and is explicitly called out as insecure practice for native apps (RFC 8252, “OAuth 2.0 for Native Apps”). Step 12 opens the user’s real default browser instead and catches the redirect on a local loopback port. 

**Six — the confirmed decisions this document is built on, stated plainly so nothing here is a silent guess:** - **Desktop framework: Electron + React** , reusing the dashboard route scaffolding (Overview/Alerts/Incidents/Endpoints placeholders, component patterns) from the previous revision instead of discarding it. - **Agent telemetry/heartbeats/Redis ingestion: documented, not implemented.** Step 13’s flow diagram shows the eventual end-to-end path; none of it is built here. Implementation stays where the Implementation Plan’s dependency order already puts it (item 5, its own phase). - **Two separate installers** , not one bundled MSI: a SentinelConsole.msi (Electron app, for analyst/CSO/admin workstations — built this phase, Step 12) and SentinelAgent.msi (Windows Service, for protected endpoints — not built this phase, still Phase 5’s job). The public site’s Download page (Step 11) presents them as two distinct, clearly-labeled downloads specifically so nobody installs the wrong one on the wrong machine. 

## **0. What Phase 3 Actually Covers** 

**In scope:** - applications table and the full status chain, POST /applications, Support Team review actions, Owner approval calling into OrganizationsService.CreateOrganizationAsync — unchanged from the prior revision (Steps 2, 3, 6). - **Real email delivery** (Step 4) — the onboarding approval response and the user-invitation flow (Step 7) both now send a real email, not just return data for manual relay. - **User Invitation Flow** (Step 7) — CSO invites Security Administrators, Security Administrators invite Security Analysts, from inside the desktop app’s User Management page. Reuses Phase 1’s CanManageUserHandler (the CSO→Security 

3 

Administrator→Security Analyst hierarchy check already existed; this phase is the first thing that actually calls it from an HTTP endpoint) and Step 3’s Keycloak provisioning. - Fixing the Phase 2 sub-vs-users.id TODO (Step 8). - **Public Website** (Step 11) — marketing pages, Apply form, and a Download page offering the two installers, per the decisions above. No authentication, no dashboard routes — those moved to the desktop app. - **Sentinel Desktop Application** (Step 12) — Electron + React, OAuth via system browser + PKCE + loopback redirect, safeStorage-backed token persistence, protected dashboard shell with the same placeholder pages as before (still no real data — see below). 

- **Explicitly deferred (do not build these yet): Agent telemetry, heartbeats, Redis Streams ingestion, endpoint registration.** Step 13 documents the intended end-to-end flow so the desktop app’s eventual data source is clear; none of the backend work behind it exists yet. This is Implementation Plan item 5, its own phase, unchanged from before. - **Real dashboard data.** /dashboard/alerts needs the Detection Engine (item 7); /dashboard/incidents needs Correlation (item 9); /dashboard/endpoints needs Agent Telemetry Ingestion (item 5). The desktop app’s protected shell proves login →token →protected view works end to end; it does not fabricate data to look more finished than the backend actually is. - **SentinelAgent.msi itself.** The public site’s Download page links to it; the link is a placeholder until Phase 5 produces a real one. - Keycloak Groups (see note one above). - approval_policy on organizations — still SOAR-phase scope. - HTML email templates, retry queues for failed sends, or any email delivery beyond synchronous plaintext SMTP — see Step 4’s →skipped note. 

## **1. Add the Missing Folders** 

- $ cd path/to/your/project/backend 

- $ mkdir -p src/Sentinel.Identity/Onboarding 

- $ mkdir -p src/Sentinel.Identity/Keycloak 

- $ mkdir -p src/Sentinel.Identity/Email 

- $ cd path/to/your/project/frontend 

- $ mkdir -p src/routes/public src/components src/lib 

- $ cd path/to/your/project 

- $ mkdir -p desktop/main desktop/preload desktop/renderer/src/routes/dashboard 

- $ mkdir -p desktop/renderer/src/components desktop/renderer/src/lib 

Updated structure after this phase: 

your-project/ ├──frontend/ │ ├──src/ │ │ ├──App.tsx │ │ ├──routes/public/ │ │ │ │ │ └──components/ │ ├──vite.config.ts 

- ←marketing site only, no auth, no dashboard 

- ←plain router, no lazy-split needed anymore 

- ←Home, Features, Pricing, About, Contact, Apply, Privacy, Terms, Download 

4 

│ └──package.json ├──desktop/ ←new this phase — Electron + React │ ├──main/ │ │ ├──index.ts ←BrowserWindow, IPC handlers │ │ ├──auth.ts ←loopback OAuth + PKCE (Node/main process) │ │ └──tokenStore.ts ←safeStorage-backed refresh token persistence │ ├──preload/ │ │ └──index.ts ←contextBridge, narrow renderer-facing API │ ├──renderer/ │ │ ├──index.html │ │ └──src/ │ │ ├──App.tsx │ │ └──routes/dashboard/ ←same placeholder pages as prior revision │ ├──electron-builder.yml │ └──package.json ├──backend/ │ ├──migrations/ │ │ ├──001_init.sql │ │ ├──002_organizations_licensing.sql │ │ └──003_applications.sql │ └──src/ │ ├──Sentinel.Api/ │ │ └──Controllers/ │ │ ├──ApplicationsController.cs │ │ └──UsersController.cs ←new, Step 7 │ ├──Sentinel.Identity/ │ │ ├──Onboarding/ │ │ ├──Keycloak/ │ │ ├──Email/ ←new, Step 4 │ │ └──(existing Phase 1/2 files, unchanged except middleware, Step 8) │ └──Sentinel.Licensing/ └──docs/ └──Sentinel_Phase3_Onboarding_DesktopApp.md 

## **— 2. Migration 003 Applications** 

**Where:** backend/migrations/003_applications.sql 

_-- backend/migrations/003_applications.sql_ 

**CREATE TYPE** application_status **AS** ENUM ( 'PENDING', 'IN_REVIEW', 'IN_DISCUSSION', 'APPROVED', 'REJECTED' ); **CREATE TABLE** applications ( **id** UUID **PRIMARY KEY DEFAULT** gen_random_uuid(), 

5 

company_name TEXT **NOT NULL** , contact_name TEXT **NOT NULL** , contact_email TEXT **NOT NULL** , requested_endpoint_cap INTEGER **NOT NULL CHECK** (requested_endpoint_cap ↪ **>** 0), notes TEXT, status application_status **NOT NULL DEFAULT** 'PENDING', reviewed_by UUID **REFERENCES** users( **id** ), rejection_reason TEXT, organization_id UUID **REFERENCES** organizations( **id** ), created_at TIMESTAMPTZ **NOT NULL DEFAULT** now(), updated_at TIMESTAMPTZ **NOT NULL DEFAULT** now(), **CONSTRAINT** chk_org_only_when_approved **CHECK** (organization_id **IS NULL OR** status **=** 'APPROVED'), **CONSTRAINT** chk_reason_only_when_rejected **CHECK** (rejection_reason **IS NULL OR** status **=** 'REJECTED') ); 

**CREATE INDEX** idx_applications_status **ON** applications(status); 

**If you’re running the corrected full schema delivered separately** (the one with created_organization_id instead of organization_id, keycloak_user_id instead of keycloak_id), use those column names in every query below instead of the ones shown — this document’s code samples were not re-derived against that schema; adjust the same way Step 8 already does for the sub/users.id distinction. 

No company_scope flag here — applications is only ever reachable through [Authorize(Policy = "SentinelCompany")] or is fully public (POST only), so there’s no crosstenant query surface for the flag to guard. 

**How to run:** Supabase dashboard →SQL Editor →New Query →paste →Run. **How to know it worked:** Table Editor shows applications, empty, with the 5-value application_status enum available. 

## **3. Real Keycloak Admin API Client** 

**First, a realm config change.** The sentinel-api client’s service account needs realm-management’s manage-users role before it can create users through the Admin REST API. 

**Where:** backend/keycloak/realm-export.json — add a users entry for the service account, and (new this phase) a public client for the desktop app’s PKCE flow: 

**{** "clients" **:** [ **{** "clientId" **:** "sentinel-desktop" **,** 

6 

"publicClient" **: true,** "standardFlowEnabled" **: true,** "directAccessGrantsEnabled" **: false,** "redirectUris" **:** ["http://127.0.0.1:53682/callback"] **,** "attributes" **: {** "pkce.code.challenge.method" **:** "S256" **} }** ] **,** "users" **:** [ **{** "username" **:** "bootstrap-owner" **,** "..." **:** "unchanged from Phase 1" **}** , **{** "username" **:** "service-account-sentinel-api" **,** "serviceAccountClientId" **:** "sentinel-api" **,** "clientRoles" **: {** "realm-management" **:** ["manage-users"] **} }** ] **}** 

sentinel-desktop **supersedes** the previous revision’s sentinel-dashboard client for authentication purposes — there is no browser-based login anymore, so nothing calls sentinel-dashboard’s auth flow. Leave sentinel-dashboard in the realm if you want to keep the option of a browser admin panel later; it just isn’t used by anything built in this phase. 

Realm imports only run once, on first container creation. If your sentinel realm already exists, reset it to pick this up: 

$ docker compose down -v 

$ docker compose up 

**Where:** backend/src/Sentinel.Identity/Keycloak/KeycloakUserProvisioningService.cs — unchanged from the prior revision: 

_// back-_ 

↪ _end/src/Sentinel.Identity/Keycloak/KeycloakUserProvisioningService.cs_ **using** System **.Net.Http.Json; using** System **.Text.Json; using** Sentinel **.Identity.Organizations;** 

**namespace** Sentinel **.Identity.Keycloak;** 

**public** record **ProvisionedUser(** Guid KeycloakId **,** string TemporaryPassword **);** 

**public interface** IKeycloakAdminProvisioningService **:** ↪ IKeycloakUserProvisioningService 

**{** 

Task **<** ProvisionedUser **> ProvisionUserWithPasswordAsync(** string email **,** ↪ string role **,** Guid organizationId **);** 

**}** 

7 

**public class** KeycloakUserProvisioningService **:** ↪ IKeycloakAdminProvisioningService 

**{ private readonly** HttpClient _http **; private readonly** string _realm **; private readonly** string _clientId **; private readonly** string _clientSecret **;** 

**public KeycloakUserProvisioningService(** HttpClient http **,** IConfiguration ↪ config **)** 

**{** 

_http **=** http **;** _realm **=** config **[** "Keycloak:Realm" **] ??** "sentinel" **;** _clientId **=** config **[** "Keycloak:AdminClientId" **] ??** "sentinel-api" **;** _clientSecret **=** config **[** "Keycloak:AdminClientSecret" **] ?? throw new** ↪ **InvalidOperationException(** "Keycloak:AdminClientSecret is ↪ missing." **); }** 

_// ponytail: no token caching — onboarding/invite volume is a handful of // calls a day, not a hot path._ **private** async Task **<** string **> GetAdminTokenAsync() {** 

var response **=** await _http **.PostAsync(** $"/realms/{_realm}/protocol/openid-connect/token" **, new FormUrlEncodedContent(new** Dictionary **<** string **,** string **> { [** "grant_type" **] =** "client_credentials" **, [** "client_id" **] =** _clientId **, [** "client_secret" **] =** _clientSecret **, }));** response **.EnsureSuccessStatusCode();** var payload **=** await ↪ response **.Content.ReadFromJsonAsync<** JsonElement **>(); return** payload **.GetProperty(** "access_token" **).GetString()!; }** 

**public** async Task **<** Guid **> ProvisionUserAsync(** string email **,** string role **,** ↪ Guid organizationId **) => (** await **ProvisionUserWithPasswordAsync(** email **,** role **,** ↪ organizationId **)).KeycloakId;** 

**public** async Task **<** ProvisionedUser **> ProvisionUserWithPasswordAsync(** string email **,** string role **,** Guid organizationId **)** 

**{** 

var adminToken **=** await **GetAdminTokenAsync();** 

8 

_http **.DefaultRequestHeaders.Authorization =** 

**new** System **.Net.Http.Headers.AuthenticationHeaderValue(** "Bearer" **,** ↪ adminToken **);** 

var tempPassword **=** Guid **.NewGuid().ToString(** "N" **)[..16];** 

var createResponse **=** await ↪ _http **.PostAsJsonAsync(** $"/admin/realms/{_realm}/users" **, new {** username **=** email **,** email **,** enabled **= true,** attributes **= new {** organizationId **= new[] {** ↪ organizationId **.ToString() } },** credentials **= new[] { new {** type **=** "password" **,** value **=** ↪ tempPassword **,** temporary **= true } }, });** createResponse **.EnsureSuccessStatusCode();** 

var location **=** createResponse **.Headers.Location ?? throw new InvalidOperationException(** "Keycloak did not return ↪ a Location header." **);** var keycloakId **=** Guid **.Parse(** location **.Segments[^** 1 **]);** var roleResponse **=** await _http **.GetFromJsonAsync<** JsonElement **>(** $"/admin/realms/{_realm}/roles/{role}" **);** var assignResponse **=** await _http **.PostAsJsonAsync(** ↪ $"/admin/realms/{_realm}/users/{keycloakId}/role-mappings/realm" **, new[] { new {** id **=** roleResponse **.GetProperty(** "id" **).GetString(),** ↪ name **=** role **} });** assignResponse **.EnsureSuccessStatusCode(); return new ProvisionedUser(** keycloakId **,** tempPassword **); } }** 

→skipped: retry/rollback if the role-assignment call fails after user creation succeeds. add when: onboarding/invite volume outgrows “a human notices and cleans it up manually.” 

## **4. Email Delivery** 

**Where:** backend/src/Sentinel.Identity/Email/ 

9 

System.Net.Mail.SmtpClient is in the BCL and needs no package — but Microsoft has marked it not recommended for new development (blocking I/O, no modern auth). Onboarding correctness now depends on this actually working, the same way PKCE correctness mattered enough to pull in a real library instead of hand-rolling it — so this is the other place ponytail’s “skip the dependency” instinct doesn’t apply. Use **MailKit** . 

$ cd backend/src/Sentinel.Identity 

$ dotnet add package MailKit 

**Where:** backend/src/Sentinel.Identity/Email/IEmailSender.cs 

_// backend/src/Sentinel.Identity/Email/IEmailSender.cs_ **namespace** Sentinel **.Identity.Email;** 

**public interface** IEmailSender **{** 

Task **SendAsync(** string toEmail **,** string subject **,** string bodyText **);** 

**}** 

**Where:** backend/src/Sentinel.Identity/Email/SmtpEmailSender.cs 

_// backend/src/Sentinel.Identity/Email/SmtpEmailSender.cs_ **using** MailKit **.Net.Smtp; using** MailKit **.Security; using** MimeKit **;** 

**namespace** Sentinel **.Identity.Email;** 

**public class** SmtpEmailSender **:** IEmailSender 

**{** 

**private readonly** IConfiguration _config **; public SmtpEmailSender(** IConfiguration config **) =>** _config **=** config **;** 

**public** async Task **SendAsync(** string toEmail **,** string subject **,** string ↪ bodyText **) {** 

var fromAddress **=** _config **[** "Smtp:FromAddress" **] ?? throw new InvalidOperationException(** "Smtp:FromAddress is ↪ missing." **);** 

var message **= new MimeMessage();** message **.From.Add(** MailboxAddress **.Parse(** fromAddress **));** message **.To.Add(** MailboxAddress **.Parse(** toEmail **));** message **.Subject =** subject **;** message **.Body = new TextPart(** "plain" **) {** Text **=** bodyText **};** 

**using** var client **= new SmtpClient();** await client **.ConnectAsync(** 

10 

_config **[** "Smtp:Host" **],** int **.Parse(** _config **[** "Smtp:Port" **] ??** "587" **),** ↪ SecureSocketOptions **.StartTls);** await client **.AuthenticateAsync(** _config **[** "Smtp:Username" **],** ↪ _config **[** "Smtp:Password" **]);** await client **.SendAsync(** message **);** await client **.DisconnectAsync(true); } }** 

**Where:** backend/src/Sentinel.Identity/Onboarding/OnboardingEmailComposer.cs 

_// backend/src/Sentinel.Identity/Onboarding/OnboardingEmailComposer.cs_ **namespace** Sentinel **.Identity.Onboarding;** 

**public static class** OnboardingEmailComposer **{ public static (** string Subject **,** string Body **) ComposeCsoWelcome(** string companyName **,** string username **,** string temporaryPassword **,** ↪ string desktopDownloadUrl **) => (** $"Welcome to Sentinel, {companyName}" **,** $""" 

Your Sentinel organization has been approved **.** 

Username **: {** username **}** Temporary password **: {** temporaryPassword **} (** You will be asked to set a **new** password on first sign **-in.)** 

Download the Sentinel Desktop Application **: {** desktopDownloadUrl **}** 

Install it **,** then sign **in** with the credentials above **.** """); 

**public static (** string Subject **,** string Body **) ComposeInvite(** string username **,** string temporaryPassword **,** string ↪ desktopDownloadUrl **) => (** "You've been invited to Sentinel" **,** $""" You've been added **as** a Sentinel user **.** 

Username **: {** username **}** Temporary password **: {** temporaryPassword **}** 

Download the Sentinel Desktop Application **: {** desktopDownloadUrl **}** """); 

**}** 

11 

→skipped: HTML templates, localization, a send queue with retry. add when: there’s more than one email in the system to justify a template engine, or send volume is high enough that synchronous SMTP inside a request becomes a real latency problem — neither is true yet. 

**Where:** backend/.env — add SMTP config alongside the existing Supabase/Keycloak secrets: 

SMTP_HOST **=** smtp.example.com SMTP_PORT **=** 587 SMTP_USERNAME **=** apikey SMTP_PASSWORD **=** change-this SMTP_FROM_ADDRESS **=** onboarding@sentinel.example.com 

Wire these through docker-compose.yml’s sentinel-api environment block the same way as the Keycloak admin secret (Smtp__Host: ${SMTP_HOST}, etc.). 

## **5. Onboarding / Applications Service** 

Unchanged from the prior revision except ApproveApplicationAsync, which now sends the welcome email — and handles the email failing without losing the organization it just correctly created. 

**Where:** backend/src/Sentinel.Identity/Onboarding/ApplicationModels.cs 

_// backend/src/Sentinel.Identity/Onboarding/ApplicationModels.cs_ **namespace** Sentinel **.Identity.Onboarding;** 

**public** record **SubmitApplicationRequest(** 

string CompanyName **,** string ContactName **,** string ContactEmail **,** int RequestedEndpointCap **,** string **?** Notes **);** 

**public** record **Application(** 

Guid Id **,** string CompanyName **,** string ContactName **,** string ContactEmail **,** int RequestedEndpointCap **,** string **?** Notes **,** string Status **,** Guid **?** OrganizationId **,** DateTimeOffset CreatedAt **);** 

_// EmailDelivered lets the desktop/website caller know whether the fallback // path (relay the password manually) is actually needed, instead of // assuming email always works._ **public** record **ApproveApplicationResult(** 

Guid OrganizationId **,** Guid CsoUserId **,** Guid LicenseId **,** string TemporaryPassword **,** bool EmailDelivered **);** 

**Where:** backend/src/Sentinel.Identity/Onboarding/ApplicationsService.cs — the ApproveApplicationAsync method (rest of the class is unchanged from the prior revision): 

12 

**public** async Task **<** ApproveApplicationResult **> ApproveApplicationAsync(** Guid ↪ applicationId **)** 

**{** 

await **using** var conn **=** _dataSource **.CreateConnection();** await conn **.OpenAsync();** 

var app **=** await **GetForUpdateAsync(** conn **,** applicationId **); if (** app **.Status !=** "IN_DISCUSSION" **)** 

**throw new InvalidOperationException(** 

$"Application {applicationId} is {app.Status}, not IN_DISCUSSION ↪ — cannot approve." **);** 

var orgResult **=** await _organizations **.CreateOrganizationAsync( new CreateOrganizationRequest(** app **.CompanyName,** app **.ContactEmail,** ↪ app **.RequestedEndpointCap));** 

await **using** var cmd **= new NpgsqlCommand(** @"UPDATE applications SET status = 'APPROVED', organization_id = ↪ @orgId, updated_at = now() WHERE id **=** @id", conn); cmd **.Parameters.AddWithValue(** "id" **,** applicationId **);** cmd **.Parameters.AddWithValue(** "orgId" **,** orgResult **.OrganizationId);** await cmd **.ExecuteNonQueryAsync();** 

_// Email failing must not undo an org that was already correctly_ ↪ _created — // data integrity wins. The already-existing "Owner relays the password // manually" path (Human Discussion, established back in the onboarding // status chain itself) becomes the fallback for THIS failure mode too, // not just "we hadn't built email yet."_ var emailDelivered **= true; try { var (** subject **,** body **) =** OnboardingEmailComposer **.ComposeCsoWelcome(** app **.CompanyName,** app **.ContactEmail,** ↪ orgResult **.CsoTemporaryPassword,** _config **[** "PublicSite:DesktopDownloadUrl" **] ??** ↪ "https://sentinel.example.com/download" **);** await _emailSender **.SendAsync(** app **.ContactEmail,** subject **,** body **); } catch {** 

emailDelivered **= false;** _// logged by whatever the caller's exception_ ↪ _middleware already does_ **}** 

**return new ApproveApplicationResult(** 

13 

orgResult **.OrganizationId,** orgResult **.CsoUserId,** orgResult **.LicenseId,** orgResult **.CsoTemporaryPassword,** emailDelivered **);** 

**}** 

ApplicationsService’s constructor gains IEmailSender emailSender, IConfiguration config — both already registered in DI by Step 9. 

## **6. Applications Controller & Rate Limiting** 

Unchanged from the prior revision — POST /applications public and rate-limited, review/approve/reject actions restricted by [Authorize(Policy = ...)]. See Step 9 for the DI registrations this now needs in addition to what was already there. 

## **7. User Invitation Flow** 

This is genuinely new scope, not a Phase 2 leftover — flagged plainly rather than folded in silently. It reuses two things that already existed for a different purpose: Phase 1’s CanManageUserHandler (the CSO→Security Administrator→Security Analyst hierarchy check had no HTTP endpoint calling it until now) and Step 3’s Keycloak provisioning. 

**Where:** backend/src/Sentinel.Api/Controllers/UsersController.cs 

_// backend/src/Sentinel.Api/Controllers/UsersController.cs_ **using** Microsoft **.AspNetCore.Authorization; using** Microsoft **.AspNetCore.Mvc; using** Npgsql **; using** Sentinel **.Identity.Data; using** Sentinel **.Identity.Email; using** Sentinel **.Identity.Keycloak; using** Sentinel **.Identity.Onboarding;** 

**namespace** Sentinel **.Api.Controllers;** 

**public** record **InviteUserRequest(** string Email **,** string Role **);** 

**[** ApiController **] [Route(** "users" **)] [Authorize(** Policy **=** "AnyOrganizationRole" **)] public class** UsersController **:** ControllerBase **{** 

**private readonly** IKeycloakAdminProvisioningService _provisioning **; private readonly** IEmailSender _emailSender **;** 

14 

**private readonly** IAuthorizationService _authz **; private readonly** IConfiguration _config **; private readonly** ISentinelDataSource _dataSource **;** 

**public UsersController(** 

IKeycloakAdminProvisioningService provisioning **,** IEmailSender ↪ emailSender **,** 

IAuthorizationService authz **,** IConfiguration config **,** 

↪ ISentinelDataSource dataSource **) {** 

_provisioning **=** provisioning **;** _emailSender **=** emailSender **;** _authz **=** authz **;** _config **=** config **;** _dataSource **=** dataSource **;** 

**}** 

##### **[HttpPost(** "invite" **)]** 

**public** async Task **<** IActionResult **> Invite([** FromBody **]** InviteUserRequest ↪ request **)** 

**{** 

- _// Narrows [Authorize(AnyOrganizationRole)] down to the actual_ 

- _// hierarchy rule: CSO can only invite security-administrator,_ 

- _// Security Administrator can only invite security-analyst._ 

- var authResult **=** await _authz **.AuthorizeAsync(** User **,** request **.Role,** ↪ "CanManageUser" **);** 

- **if (!** authResult **.Succeeded) return Forbid();** 

var orgIdClaim **=** User **.FindFirst(** "organizationId" **)?.Value;** 

**if (!** Guid **.TryParse(** orgIdClaim **, out** var organizationId **))** 

**return Forbid();** _// Owner/Support Team carry no organizationId —_ ↪ _this endpoint isn't for them._ 

var provisioned **=** await 

↪ _provisioning **.ProvisionUserWithPasswordAsync(** request **.Email,** request **.Role,** organizationId **);** 

_// Keycloak provisioning alone isn't enough — every other org-scoped // query in this codebase joins against Postgres `users`. Without_ ↪ _this // insert, the new account can authenticate but is invisible to // audit logs, incident assignment, everything else._ await **using (** var conn **=** _dataSource **.CreateConnection()) {** 

await conn **.OpenAsync();** await **using** var cmd **= new NpgsqlCommand(** 

@"INSERT INTO users (id, organization_id, keycloak_user_id, ↪ email, display_name, role) 

**VALUES (** @id **,** @orgId **,** @kcId **,** @email **,** @displayName **,** ↪ @role **::** user_role **)** ", conn); 

15 

cmd **.Parameters.AddWithValue(** "id" **,** Guid **.NewGuid());** cmd **.Parameters.AddWithValue(** "orgId" **,** organizationId **);** cmd **.Parameters.AddWithValue(** "kcId" **,** ↪ provisioned **.KeycloakId.ToString());** cmd **.Parameters.AddWithValue(** "email" **,** request **.Email);** cmd **.Parameters.AddWithValue(** "displayName" **,** ↪ request **.Email.Split(** '@' **)[** 0 **]);** _// "security-administrator" -> "SECURITY_ADMINISTRATOR" — must_ ↪ _land // exactly on the Postgres enum value, not just be uppercased._ cmd **.Parameters.AddWithValue(** "role" **,** ↪ request **.Role.ToUpperInvariant().Replace(** '-' **,** '_' **));** await cmd **.ExecuteNonQueryAsync(); }** var emailDelivered **= true; try { var (** subject **,** body **) =** OnboardingEmailComposer **.ComposeInvite(** request **.Email,** provisioned **.TemporaryPassword,** _config **[** "PublicSite:DesktopDownloadUrl" **] ??** ↪ "https://sentinel.example.com/download" **);** await _emailSender **.SendAsync(** request **.Email,** subject **,** body **); } catch {** emailDelivered **= false; } return Ok(new {** keycloakId **=** provisioned **.KeycloakId,** temporaryPassword **=** provisioned **.TemporaryPassword,** emailDelivered **, }); } }** 

The response still includes temporaryPassword even on success — same reasoning as Step 5’s EmailDelivered flag. The inviter (CSO or Security Administrator, already authenticated, already trusted) is the fallback delivery path if emailDelivered comes back false. 

display_name is set to the email’s local part, same placeholder-quality gap flagged in earlier revisions — the invite form should collect a real name and pass it through; not fixed here, still worth fixing before this feels finished. 

16 

## **8. Fix the Phase 2 sub / users.id TODO** 

**Where:** backend/src/Sentinel.Identity/Data/UserLookupService.cs 

_// backend/src/Sentinel.Identity/Data/UserLookupService.cs_ **using** Npgsql **;** 

**namespace** Sentinel **.Identity.Data;** 

**public interface** IUserLookupService **{** Task **<** Guid **?> GetInternalUserIdAsync(** Guid keycloakId **); }** 

**public class** UserLookupService **:** IUserLookupService **{** 

**private readonly** ISentinelDataSource _dataSource **; public UserLookupService(** ISentinelDataSource dataSource **) =>** _dataSource ↪ **=** dataSource **;** 

**public** async Task **<** Guid **?> GetInternalUserIdAsync(** Guid keycloakId **) {** await **using** var conn **=** _dataSource **.CreateConnection();** await conn **.OpenAsync();** await **using** var cmd **= new NpgsqlCommand(** "SELECT id FROM users WHERE keycloak_user_id = @keycloakId" **,** ↪ conn **);** cmd **.Parameters.AddWithValue(** "keycloakId" **,** keycloakId **.ToString());** var result **=** await cmd **.ExecuteScalarAsync(); return** result **is** Guid id **?** id **: null; } }** 

(Column name here is keycloak_user_id, matching the actual deployed schema — not keycloak_id, which earlier drafts of this document used before the schema was reconciled.) 

**Where:** backend/src/Sentinel.Api/OrganizationIsolationMiddleware.cs — same fix as before: 

var keycloakId **=** orgContext **.UserId** 

- **?? throw new UnauthorizedAccessException(** "Token contains no resolvable ↪ user identity." **);** 

var supportUserId **=** await userLookup **.GetInternalUserIdAsync(** keycloakId **)** 

**?? throw new UnauthorizedAccessException(** "No matching users row for 

- ↪ this Keycloak identity." **);** 

17 

## **9. Wire It All Into Program.cs** 

**Where:** backend/src/Sentinel.Api/Program.cs 

**using** Sentinel **.Identity.Keycloak; using** Sentinel **.Identity.Onboarding; using** Sentinel **.Identity.Email; using** Sentinel **.Identity.Data;** 

builder **.Services.AddHttpClient<** IKeycloakAdminProvisioningService **,** ↪ KeycloakUserProvisioningService **>(** client **=> {** client **.BaseAddress = new** ↪ **Uri(** builder **.Configuration[** "Keycloak:BaseUrl" **]!); });** builder **.Services.AddScoped<** IApplicationsService **,** ApplicationsService **>();** builder **.Services.AddScoped<** IUserLookupService **,** UserLookupService **>();** builder **.Services.AddScoped<** IEmailSender **,** SmtpEmailSender **>();** _// new, Step_ ↪ _4_ builder **.Services.AddRateLimiter(** options **=> {** options **.RejectionStatusCode =** StatusCodes **.Status429TooManyRequests;** options **.AddFixedWindowLimiter(** "public-applications" **,** opt **=> {** opt **.PermitLimit =** 3 **;** opt **.Window =** TimeSpan **.FromHours(** 1 **);** opt **.QueueLimit =** 0 **; }); });** _// ... app.Build() ..._ app **.UseAuthentication();** app **.UseRateLimiter();** app **.UseMiddleware<** OrganizationIsolationMiddleware **>();** app **.UseAuthorization();** 

**Where:** backend/.env — Keycloak admin secret and SMTP config from Steps 3 and 4 both live here, never in appsettings.json. 

## **10. Update the CI Workflow** 

**Where:** .github/workflows/ci.yml — one job for the marketing site, one for the desktop app. The desktop job builds and type-checks; it does **not** produce a real signed MSI in CI unless the runner is windows-latest — electron-builder --win 

18 

msi needs Windows (or Wine) to actually package an MSI, so a Linux runner is fine for “does the app build,” not for “produce the installer artifact.” 

**website: runs-on:** ubuntu-latest **steps: - uses:** actions/checkout@v4 **- uses:** actions/setup-node@v4 **with: { node-version:** "20" **} - run:** npm ci **working-directory:** frontend **- run:** npm run build **working-directory:** frontend **desktop: runs-on:** windows-latest _# needed for a real electron-builder MSI_ ↪ _output_ **steps: - uses:** actions/checkout@v4 **- uses:** actions/setup-node@v4 **with: { node-version:** "20" **} - run:** npm ci **working-directory:** desktop **- run:** npm run build **working-directory:** desktop 

## **11. Public Website** 

#### **Where:** frontend/ 

Back to being small and static — no oidc-client-ts, no /login, no /callback, no protected routes, no lazy-split concern, because there is no longer any dashboard code sitting in this app for an anonymous visitor to accidentally download. App.tsx becomes a plain router over the public pages; Home/Features/Pricing/About/Contact/Privacy/Terms/Apply carry over unchanged from the prior revision (still no real logic beyond Apply’s fetch call — write the actual marketing/legal copy before shipping). 

**Where:** frontend/src/routes/public/Download.tsx — new this phase, and it’s where the two-installer decision becomes something a real visitor sees, not just a document: 

// frontend/src/routes/public/Download.tsx export default function Download() { return ( <div> <h1>Download Sentinel</h1> 

19 

<section> <h2>Sentinel Desktop Application</h2> <p> For Security Analysts, Security Administrators, and CSOs — the application you use to sign in and monitor your organization. </p> <a href="/downloads/SentinelConsole.msi"> Download Desktop Console (SentinelConsole.msi) </a> </section> <section> <h2>Sentinel Agent</h2> <p> For endpoints you want protected — runs as a background Windows Service, no user interface. Install this on the machines you want monitored, <strong>not</strong> on analyst or admin workstations. </p> <p><em>Not available yet — ships in a later phase.</em></p> </section> </div> ); } 

**How to run locally:** cd frontend && npm install && npm run dev. **How to know it worked:** Vite prints a localhost:5173 URL. /, /apply, /download load with no auth-related network calls at all — check the network tab; there should be nothing resembling a Keycloak request anywhere on this site anymore. 

## **12. Sentinel Desktop Application (Electron + React)** 

**Where:** desktop/ 

**Where:** desktop/package.json 

**{** "name" **:** "sentinel-desktop" **,** "private" **: true,** "main" **:** "main/index.js" **,** "scripts" **: {** "dev" **:** "electron ." **,** "build" **:** "tsc" **,** "build:win" **:** "electron-builder --win msi" **},** "dependencies" **: {** "react" **:** "^18.3.0" **,** 

20 

"react-dom" **:** "^18.3.0" **,** "react-router-dom" **:** "^6.26.0" **,** "openid-client" **:** "^5.6.0" **},** "devDependencies" **: {** "electron" **:** "^31.0.0" **,** "electron-builder" **:** "^24.13.0" **,** "typescript" **:** "^5.5.0" **} }** 

openid-client — not oidc-client-ts (that library assumes a browser context; the code exchange here happens in Electron’s Node-based main process). Getting this library choice right matters the same way the PKCE library choice mattered before: this is security-relevant state machine code, not boilerplate worth hand-rolling. 

**Where:** desktop/main/auth.ts — the loopback OAuth flow. This is the piece that actually implements note five at the top of this document; the shell.openExternal call is not optional styling. 

_// desktop/main/auth.ts_ import { shell } from "electron" **;** import ***** as http from "node:http" **;** import { Issuer **,** generators **, type** TokenSet } from "openid-client" **; const** REDIRECT_PORT **=** 53682 **;** _// must match the redirect URI registered in_ ↪ _realm-export.json (Step 3)_ **const** REDIRECT_URI **=** `http://127.0.0.1: **${** REDIRECT_PORT **}** /callback` **; async function getClient** () { **const** issuer **= await** ↪ Issuer **.discover** ("http://localhost:8080/realms/sentinel") **; return new** issuer **.Client** ({ client_id **:** "sentinel-desktop" **,** redirect_uris **:** [REDIRECT_URI] **,** response_types **:** ["code"] **,** token_endpoint_auth_method **:** "none" **,** _// public client — no secret_ ↪ _shipped in a desktop binary_ }) **;** } export **async function signIn** () **:** Promise **<** TokenSet **>** { **const** client **= await getClient** () **; const** codeVerifier **=** generators **.codeVerifier** () **; const** codeChallenge **=** generators **.codeChallenge** (codeVerifier) **; const** authUrl **=** client **.authorizationUrl** ({ scope **:** "openid profile email" **,** code_challenge **:** codeChallenge **,** 

21 

code_challenge_method **:** "S256" **,** }) **; const** code **= await new** Promise **<** string **>** ((resolve **,** reject) **=>** { **const** server **=** http **.createServer** ((req **,** res) **=>** { **const** url **= new URL** (req **.** url **!,** REDIRECT_URI) **; const** authCode **=** url **.** searchParams **.get** ("code") **;** res **.end** ("Signed in **\u2014** you can close this tab and return to ↪ Sentinel.") **;** server **.close** () **;** authCode **? resolve** (authCode) **: reject** ( **new** Error("No authorization ↪ code returned.")) **;** }) **;** server **.listen** (REDIRECT_PORT **,** "127.0.0.1") **;** shell **.openExternal** (authUrl) **;** _// system default browser \u2014 never an_ ↪ _embedded webview_ }) **; return** client **.callback** (REDIRECT_URI **,** { code } **,** { code_verifier **:** ↪ codeVerifier }) **;** } 

export **async function refreshAccessToken** (refreshToken **:** string) **:** ↪ Promise **<** TokenSet **>** { **const** client **= await getClient** () **; return** client **.refresh** (refreshToken) **;** } 

**Where:** desktop/main/tokenStore.ts — refresh tokens go through Electron’s safeStorage (OS keychain / Windows DPAPI), never a plaintext file. 

_// desktop/main/tokenStore.ts_ import { app **,** safeStorage } from "electron" **;** import { readFileSync **,** writeFileSync **,** existsSync } from "node:fs" **;** import { join } from "node:path" **;** 

**const** TOKEN_PATH **= join** (app **.getPath** ("userData") **,** "refresh-token.enc") **;** export **function saveRefreshToken** (token **:** string) **:** void { **if** ( **!** safeStorage **.isEncryptionAvailable** ()) **throw new** Error("OS-level credential storage unavailable **\u2014** ↪ refusing to store the refresh token in plaintext.") **; writeFileSync** (TOKEN_PATH **,** safeStorage **.encryptString** (token)) **;** } 

export **function loadRefreshToken** () **:** string **|** null { **if** ( **!existsSync** (TOKEN_PATH)) **return null;** 

22 

**return** safeStorage **.decryptString** ( **readFileSync** (TOKEN_PATH)) **;** } 

Refusing to fall back to plaintext when OS encryption is unavailable is a deliberate choice, not an oversight — a security product silently degrading its own token storage is worse than failing loudly and telling the person to fix their OS keychain. 

**Where:** desktop/main/index.ts — the webPreferences block below is **mandatory** , not a default worth leaving unexamined; this is exactly where an Electron app for a security product would embarrass itself. 

_// desktop/main/index.ts_ import { app **,** BrowserWindow **,** ipcMain } from "electron" **;** import { join } from "node:path" **;** import { signIn **,** refreshAccessToken } from "./auth" **;** import { saveRefreshToken **,** loadRefreshToken } from "./tokenStore" **; function createWindow** () **:** void { **const** win **= new BrowserWindow** ({ width **:** 1280 **,** height **:** 800 **,** webPreferences **:** { preload **: join** (__dirname **,** "../preload/index.js") **,** contextIsolation **: true,** _// mandatory \u2014 renderer never touches_ ↪ _Node directly_ nodeIntegration **: false,** _// mandatory_ sandbox **: true,** _// mandatory_ } **,** }) **;** win **.loadFile** ( **join** (__dirname **,** "../renderer/index.html")) **;** } ipcMain **.handle** ("auth:login" **, async** () **=>** { **const** tokens **= await signIn** () **; if** (tokens **.** refresh_token) **saveRefreshToken** (tokens **.** refresh_token) **; return** tokens **.** access_token **;** }) **;** ipcMain **.handle** ("auth:getAccessToken" **, async** () **=>** { **const** refreshToken **= loadRefreshToken** () **; if** ( **!** refreshToken) **return null; const** tokens **= await refreshAccessToken** (refreshToken) **; if** (tokens **.** refresh_token) **saveRefreshToken** (tokens **.** refresh_token) **;** _//_ ↪ _Keycloak rotates refresh tokens_ **return** tokens **.** access_token **;** }) **;** app **.whenReady** () **.then** (createWindow) **;** 

23 

**Where:** desktop/preload/index.ts — the only surface the renderer ever sees; no direct IPC, no direct Node. 

_// desktop/preload/index.ts_ import { contextBridge **,** ipcRenderer } from "electron" **;** contextBridge **.exposeInMainWorld** ("sentinel" **,** { login **:** () **=>** ipcRenderer **.invoke** ("auth:login") **,** getAccessToken **:** () **=>** ipcRenderer **.invoke** ("auth:getAccessToken") **,** }) **;** 

**Where:** desktop/renderer/src/App.tsx — reuses the dashboard route placeholders from the prior revision (Overview/Alerts/Incidents/Endpoints, still “authenticated, no backend data source yet”), just moved here from frontend/src/routes/dashboard/ and driven by window.sentinel instead of oidcclient-ts. 

// desktop/renderer/src/App.tsx import { useEffect, useState } from "react"; import DashboardRoutes from "./routes/dashboard"; 

declare global { interface Window { sentinel: { login: () => Promise<string>; getAccessToken: () => Promise<string | null>; }; } } export default function App() { const [token, setToken] = useState<string | null | ↪ "checking">("checking"); useEffect(() => { window.sentinel.getAccessToken().then(setToken); }, []); if (token === "checking") return <p>Checking session\u2026</p>; if (!token) return <button onClick={() => ↪ window.sentinel.login().then(setToken)}>Sign in</button>; return <DashboardRoutes />; } 

desktop/renderer/src/routes/dashboard/index.tsx is the same placeholder component from the prior revision — move the file, don’t rewrite it; nothing about its content changed, only where it lives. 

**Where:** desktop/electron-builder.yml 

24 

**appId:** com.sentinel.console **productName:** SentinelConsole **win: target:** msi **icon:** build/icon.ico **directories: output:** dist 

**How to run locally:** cd desktop && npm install && npm run dev — Electron opens a window; clicking “Sign in” opens your real default browser to Keycloak, not an in-app window. **How to know it worked:** after signing in in the browser, the browser tab shows “Signed in — you can close this tab,” and the Electron window itself transitions to the Overview placeholder without you doing anything else. Closing and reopening the app should skip the sign-in button entirely (refresh token roundtrip via safeStorage). 

## **— 13. End-to-End Flows (Documentation Only Not Implemented This Phase)** 

Per the confirmed decision, these are recorded so the intended shape is unambiguous when their backends actually get built; nothing below this line is running code in this phase. 

#### **Onboarding, start to finish:** 

Prospect visits Website /apply 

- -> POST /applications (status: PENDING) 

- -> Support Team: POST /applications/{id}/review (status: IN_REVIEW) 

- -> Human discussion, off-system (status: IN_DISCUSSION) 

- -> Owner: POST /applications/{id}/approve 

      - -> OrganizationsService creates org + CSO + license 

   - -> Email sent: username, temp password, SentinelConsole.msi download link 

- -> CSO visits Website /download, installs SentinelConsole.msi 

- -> CSO opens Sentinel Desktop, clicks Sign in 

      - -> system browser opens Keycloak 

      - -> CSO enters temp password, is forced to set a new one 

      - -> loopback redirect returns the auth code to the desktop app 

- -> CSO lands on the dashboard shell (placeholders only) 

#### **User invitation, start to finish:** 

- CSO, inside Desktop App -> User Management -> Invite Security Administrator 

- -> POST /users/invite { email, role: "security-administrator" } 

   - -> CanManageUserHandler confirms CSO -> security-administrator is allowed 

      - -> Keycloak account provisioned + Postgres users row inserted 

   - -> Email sent: username, temp password, SentinelConsole.msi download link 

- -> New Security Administrator installs SentinelConsole.msi, signs in (same loopback flow 

25 

#### **Agent telemetry — end-state shape only, nothing here exists yet:** 

Endpoint operator installs Agent MSI (not built this phase) 

- -> Windows Service starts, registers endpoint, receives Endpoint ID 

- -> Receives collection policy 

- -> Starts 60s heartbeat cycle 

- -> Collects telemetry -> SUES -> uploads to backend 

- -> Redis Streams -> Detection Engine (not built this phase) 

- -> Alerts / Incidents persisted 

- -> Desktop App's Alerts/Incidents placeholders become real once this exists 

## **14. Verification Checklist for Phase 3** 

- Migration 003 applied — applications table exists, 5-value status enum. 

- sentinel-desktop client visible in Keycloak Admin Console after docker compose down -v && docker compose up, with redirectUris containing the loopback URL and PKCE S256 set. 

- service-account-sentinel-api has manage-users under realm-management client roles. 

- dotnet build backend/Sentinel.sln succeeds with MailKit referenced and UsersController compiling. 

- POST /applications (no auth) succeeds and rate-limits correctly, same as before. 

- Approving an application via POST /applications/{id}/approve sends a real email (check your SMTP provider’s sent log, or a local catch-all like MailHog for dev) containing a working /download link and a temporary password; the API response’s emailDelivered is true. 

- Temporarily breaking Smtp:Host and re-approving a different test application still succeeds — org/CSO/license are created, emailDelivered comes back false, and temporaryPassword is still present in the response (the manual-relay fallback actually works). 

- A CSO token can POST /users/invite with role: "security-administrator" and succeeds; the same CSO token attempting role: "security-analyst" is rejected (CSO manages Security Administrators only, not Analysts directly) — confirms CanManageUserHandler is actually being consulted, not just present in code. 

- The invited user appears in Postgres users with the correct role enum value and a matching keycloak_user_id. 

- npm run build succeeds in both frontend/ and desktop/. 

- Website network tab shows zero Keycloak-related requests anywhere on the public site. 

- cd desktop && npm run dev, click Sign in — system default browser opens, not an in-app window. After completing sign-in, the Electron window shows the Overview placeholder without further action. 

- Quit and relaunch the desktop app — no sign-in prompt, straight to Overview (refresh token round-trip confirmed working). 

26 

## **15. Deliberately Not in Phase 3** 

No Agent, no telemetry, no heartbeats, no Redis Streams ingestion, no detection engine — Step 13’s diagrams document the shape, nothing there is implemented. No real dashboard data — every /dashboard/* page in the desktop app is still an authenticated placeholder. No SentinelAgent.msi — the website’s Download page links to a placeholder until Phase 5. No Keycloak Groups (note one, top of document). No approval_policy on organizations — SOAR-phase scope. No HTML email templates or send-retry queue (Step 4’s →skipped note). No distributed rate limiting — single-node fixed-window limiter, unchanged rationale from before. No auto-update mechanism for the desktop app — electron-builder’s auto-update story is a real feature with real security implications (signed update manifests, update-server trust) that deserves its own deliberate pass, not a default bolted on here. 

27 

