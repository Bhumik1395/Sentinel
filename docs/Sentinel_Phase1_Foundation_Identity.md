# **Contents** 

|**Sentinel — Phase 1: Foundation & Identity**<br>**2**|
|---|
|0. One-Time Setup on Your Computer<br>. . . . . . . . . . . . . . . . . . . . .<br>2|
|1. Open Your Existing Project and Add the Missing Folders<br>. . . . . . . . .<br>3|
|2. Create Your Supabase Project (This Is Your Database) . . . . . . . . . . .<br>4|
|3. Create the Docker Compose File — Inside backend/ . . . . . . . . . . . .<br>5|
|4. Create the Database Tables — Run in Supabase, Not Locally<br>. . . . . . .<br>7|
|5. Bootstrap Keycloak’s Roles and Users<br>. . . . . . . . . . . . . . . . . . .<br>7|
|6. Create the Backend Project<br>. . . . . . . . . . . . . . . . . . . . . . . . .<br>9|
|7. Add the Identity/Tenant Code<br>. . . . . . . . . . . . . . . . . . . . . . . .<br>11|
|8. Add the CI Skeleton — This One File Lives at Your Project Root, Not in|
|backend/<br>. . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . .<br>15|
|9. Verifcation Checklist for Phase 1 . . . . . . . . . . . . . . . . . . . . . .<br>16|
|10. Deliberately Not in Phase 1<br>. . . . . . . . . . . . . . . . . . . . . . . .<br>17|



1 

# **— Sentinel Phase 1: Foundation & Identity** 

### **(Fitted to your actual project structure: frontend/, backend/, docs/)** 

**Patched.** This revision fixes two real bugs found by tracing the actual call graph in Phase 3, not by re-reading this document’s prose: (1) CanManageUserHandler was registered as a service but never attached to a named authorization policy — every call to it threw instead of correctly rejecting; fixed in Step 6. (2) SameOrganizationHandler was registered but never wired to anything and never called anywhere — removed, since OrganizationIsolationMiddleware already covers the same check globally. Column name standardized to keycloak_user_id throughout (was keycloak_id). See the Sentinel Project Consistency Report for the full list this was drawn from. 

**Before anything else, an honest note.** This guide tells you exactly where to save each file and what to type, so you can follow it step by step. That makes it _followable_ — it doesn’t make it effortless. You’ll still be installing real developer tools and typing commands into a terminal. If something on screen doesn’t match what’s described, stop and re-read the step rather than guessing — in security-related code, guessing is how access-control bugs get shipped. 

**Where things live in your project:** everything in this phase goes into your existing backend/ folder, except this guide itself (put it in docs/) and one small hidden folder called .github/ that has to sit at your project’s root — GitHub’s automated testing feature only looks there, not inside backend/. Your frontend/ folder isn’t touched at all in Phase 1 — there’s no dashboard UI work yet, that comes later. 

## **0. One-Time Setup on Your Computer** 

Do this once, before Step 1. Skip anything you’ve already installed. 

1. **Install Docker Desktop** — runs Keycloak (login system), Neo4j, and Redis as small isolated units called “containers.” 

2 

   - Go to https://www.docker.com/products/docker-desktop/, download and install it for your OS, then **open it once** and leave it running. Look for a whale icon in your system tray (Windows) or menu bar (Mac) — that means it’s running. 

2. **Install Visual Studio Code** — the text editor you’ll use for every file below. 

   - https://code.visualstudio.com/ 

3. **Install the .NET 8 SDK** — lets your computer run the backend code (C#). 

   - https://dotnet.microsoft.com/download/dotnet/8.0 — download the **SDK** , not just the Runtime. 

   - Check it worked: open a terminal (see box below) and type dotnet -- version. You should see 8.0.xxx. 

4. **Create a free Supabase account** — this is where your database lives. 

   - https://supabase.com → **Sign Up** . 

**What’s a “terminal”?** Windows: press the Windows key, type PowerShell, press Enter. Mac: press Cmd+Space, type Terminal, press Enter. Every command below starting with $ goes into this window (don’t type the $ itself). 

## **1. Open Your Existing Project and Add the Missing Folders** 

You already have a project folder containing frontend/, backend/, and docs/. Open a terminal and navigate into it: 

$ cd path/to/your/project 

(Replace path/to/your/project with wherever it actually lives — for example ~/Documents/sentinel if that’s what you called it.) 

Now add the subfolders Phase 1 needs inside your **existing** backend/ folder — nothing here touches frontend/: 

$ mkdir -p backend/keycloak 

- $ mkdir -p backend/migrations 

- $ mkdir -p backend/src/Sentinel.Api 

- $ mkdir -p backend/src/Sentinel.Identity 

- $ mkdir -p .github/workflows 

That last one (.github/workflows) is created at your project’s root — as a sibling of frontend/, backend/, and docs/ — not inside any of them. It’s a hidden folder (the leading dot) that GitHub’s automated build-and-test feature requires to live exactly there. 

Your structure now looks like this: 

your-project/ ├──frontend/ ←untouched in Phase 1 ├──backend/ │ ├──docker-compose.yml ←Step 3 

3 

│ ├──.env ←Step 3 │ ├──keycloak/ │ │ └──realm-export.json ←Step 5 │ ├──migrations/ │ │ └──001_init.sql ←Step 4 (you'll run this in Supabase, not locally) │ └──src/ │ ├──Sentinel.Api/ ←Step 6 │ └──Sentinel.Identity/ ←Step 7 ├──docs/ │ └──Sentinel_Phase1_Foundation_Identity.md ←this guide, for reference └──.github/ └──workflows/ └──ci.yml ←Step 8 (root-level, required by GitHub) 

Keep this terminal window open — you’ll reuse it throughout. 

## **2. Create Your Supabase Project (This Is Your Database)** 

1. Log in at https://supabase.com/dashboard. 

2. Click **New Project** . 

3. Fill in: 

   - **Name:** sentinel (or anything recognizable) 

   - **Database Password:** click “Generate a password” or set your own strong one. **Copy this password somewhere safe right now** — you can’t view it again later, only reset it. 

   - **Region:** whichever is closest to you or your customers. 

4. Click **Create new project** . Wait 1–2 minutes. 

5. Left sidebar → **Project Settings** (gear icon) → **Database** . 

6. Scroll to **Connection string** → **Connection pooling** → **Transaction** mode tab. You’ll see something like: 

7. Copy this into a plain text file for now, with [YOUR-PASSWORD] replaced by your real password. You’ll need it twice below. 

**Why “pooled” and not the plain connection?** Supabase caps how many direct connections a project allows at once. The pooled connection (port 6543) shares connections efficiently and won’t hit that limit as your app grows. 

4 

## **— 3. Create the Docker Compose File Inside backend/** 

This one file starts Keycloak, Neo4j, and Redis (and later, the backend itself). 

**Where:** backend/docker-compose.yml. **How:** Visual Studio Code → **File →Open Folder** →select your project’s root folder (the one containing frontend, backend, docs) →in the left panel, right-click backend → **New File** →name it docker-compose.yml → paste: 

_# backend/docker-compose.yml_ **version:** "3.9" **services: redis: image:** redis:7 **ports: -** "6379:6379" **healthcheck: test: [** "CMD" **,** "redis-cli" **,** "ping" **] interval:** 5s **retries:** 10 **neo4j: image:** neo4j:5-community **environment: NEO4J_AUTH:** neo4j/${NEO4J_PASSWORD} **volumes: -** neo4j-data:/data **ports: -** "7474:7474" **-** "7687:7687" **keycloak: image:** quay.io/keycloak/keycloak:25.0 **command:** start-dev --import-realm **environment: KEYCLOAK_ADMIN:** ${KEYCLOAK_ADMIN_USER} **KEYCLOAK_ADMIN_PASSWORD:** ${KEYCLOAK_ADMIN_PASSWORD} **KC_DB:** postgres **KC_DB_URL:** ${SUPABASE_DB_URL_JDBC} **KC_DB_USERNAME:** ${SUPABASE_DB_USERNAME} **KC_DB_PASSWORD:** ${SUPABASE_DB_PASSWORD} **volumes: -** ./keycloak/realm-export.json:/opt/keycloak/data/import/realm↪ export.json **ports: -** "8080:8080" 

5 

**sentinel-api: build: context:** . **dockerfile:** Dockerfile **environment: ConnectionStrings__Supabase:** ${SUPABASE_DB_URL_DOTNET} **Keycloak__Authority:** "http://keycloak:8080/realms/sentinel" **Keycloak__Audience:** "sentinel-api" **depends_on: keycloak: condition:** service_started **ports: -** "5000:5000" **volumes: neo4j-data:** 

Note the build context: . — this file now lives _inside_ backend/, right next to where its Dockerfile will eventually sit, so the build context is just “this same folder,” not a path pointing to a sibling folder like it would be if the compose file lived somewhere else. 

**Where:** create a second file right next to it: backend/.env. 

_# backend/.env_ NEO4J_PASSWORD **=** choose-a-strong-password-here KEYCLOAK_ADMIN_USER **=** admin KEYCLOAK_ADMIN_PASSWORD **=** choose-a-strong-password-here 

_# From Step 2 — your Supabase connection details._ SUPABASE_DB_URL_JDBC **=** jdbc:postgresql://aws-0-us-east↪ 1.pooler.supabase.com:6543/postgres SUPABASE_DB_USERNAME **=** postgres.abcdefghijk SUPABASE_DB_PASSWORD **=** the-password-you-saved-in-step-2 

SUPABASE_DB_URL_DOTNET **=** Host=aws-0-us-east-1.pooler.supabase.com **;** Port **=** 6543 **;** ⌋ ↪ Database **=** postgres **;** Username **=** postgres.abcdefghijk **;** Password **=** the-password↪ you-saved-in-step-2 

Replace the placeholders with your real values from Step 2. 

**Never upload this file anywhere public** (like GitHub). Open (or create) a file called .gitignore at your project’s root and make sure it contains a line that says backend/.env — this tells Git to never track that file. 

**How to run this:** in your terminal: 

- $ cd path/to/your/project/backend 

- $ docker compose up 

6 

Make sure Docker Desktop is open first, or you’ll get a “cannot connect to Docker daemon” error. 

**How to know it worked:** visit http://localhost:8080 — you should see the Keycloak login page. 

## **4. Create the Database Tables — Run in Supabase, Not Locally** 

**This step changed.** Earlier drafts had you write and run 001_init.sql directly. That file is now **superseded** — Sentinel has one canonical schema going forward, delivered as a standalone corrected full-schema script (see the Project Consistency Report for why two schemas existed and how they were reconciled). Run that script instead of hand-writing tables here. Do not additionally run any 001_init.sql/002_.../003_... numbered migration file against a fresh environment — the corrected script already contains everything they would have created, with the actual column names (keycloak_user_id, not keycloak_id; company_scope, not type) that the rest of this project’s code is written against. 

**How to actually run this:** 1. Supabase dashboard →left sidebar → **SQL Editor** → **New Query** . 2. Open backend/migrations/001_full_schema_corrected.sql in VS Code, select all (Ctrl+A / Cmd+A), copy (Ctrl+C / Cmd+C). 3. Paste into the Supabase SQL Editor box. 4. Click the green **Run** button. 

**How to know it worked:** left sidebar → **Table Editor** →you should see organizations (one row, company_scope = true, slug = 'sentinel-company') and users (empty for now), along with every other table the corrected schema defines — this one script bootstraps the whole project’s data model, not just Phase 1’s slice of it. 

## **5. Bootstrap Keycloak’s Roles and Users** 

**Where:** backend/keycloak/realm-export.json. 

**{** "realm" **:** "sentinel" **,** "enabled" **: true,** "roles" **: {** "realm" **:** [ **{** "name" **:** "owner" **,** "description" **:** "Sentinel Company - platform owner" ↪ **}** , **{** "name" **:** "support-team" **,** "description" **:** "Sentinel Company - ↪ platform-wide support role" **}** , **{** "name" **:** "cso" **,** "description" **:** "Customer Organization - executive ↪ role" **}** , 

7 

**{** "name" **:** "security-administrator" **,** "description" **:** "Customer ↪ Organization - operational admin" **}** , **{** "name" **:** "security-analyst" **,** "description" **:** "Customer Organization - ↪ investigation role" **}** ] **},** "clients" **:** [ **{** "clientId" **:** "sentinel-dashboard" **,** "publicClient" **: true,** "redirectUris" **:** ["http://localhost:3000/*"] **,** "webOrigins" **:** ["http://localhost:3000"] **,** "standardFlowEnabled" **: true,** "directAccessGrantsEnabled" **: false }** , **{** "clientId" **:** "sentinel-api" **,** "publicClient" **: false,** "secret" **:** "change-this-secret" **,** "serviceAccountsEnabled" **: true,** "standardFlowEnabled" **: false,** "directAccessGrantsEnabled" **: false,** "protocolMappers" **:** [ **{** "name" **:** "organizationId" **,** "protocol" **:** "openid-connect" **,** "protocolMapper" **:** "oidc-usermodel-attribute-mapper" **,** "config" **: {** "user.attribute" **:** "organizationId" **,** "claim.name" **:** "organizationId" **,** "jsonType.label" **:** "String" **,** "access.token.claim" **:** "true" **} }** ] **}** ] **,** "users" **:** [ **{** "username" **:** "bootstrap-owner" **,** "enabled" **: true,** "credentials" **:** [ **{** "type" **:** "password" **,** "value" **:** "change-this-password" **,** "temporary" **:** ↪ **true }** ] **,** "realmRoles" **:** ["owner"] **}** 

8 

] 

**}** 

This is already referenced by docker-compose.yml’s volumes: line under keycloak: (./keycloak/realm-export.json — relative to backend/, which is exactly where you just put it). 

**How to run this:** if docker compose up is already running from Step 3, stop it (Ctrl+C in that terminal) and restart it: 

$ docker compose up 

**How to know it worked:** http://localhost:8080 →log in with your .env admin credentials →switch from “master” realm to “sentinel” realm (top-left dropdown) → **Realm roles** shows all 5 → **Users** shows bootstrap-owner. 

## **6. Create the Backend Project** 

**Where:** inside backend/. 

- $ cd path/to/your/project/backend 

$ dotnet new webapi -n Sentinel.Api -o src/Sentinel.Api 

$ dotnet new classlib -n Sentinel.Identity -o src/Sentinel.Identity 

**Where:** backend/src/Sentinel.Api/Program.cs (already exists from the command above — open in VS Code, replace everything inside it). 

_// backend/src/Sentinel.Api/Program.cs_ **using** Microsoft **.AspNetCore.Authentication.JwtBearer; using** Microsoft **.IdentityModel.Tokens; using** Sentinel **.Identity;** 

var builder **=** WebApplication **.CreateBuilder(** args **);** 

var keycloakAuthority **=** builder **.Configuration[** "Keycloak:Authority" **]!;** var keycloakAudience **=** builder **.Configuration[** "Keycloak:Audience" **]!;** 

builder **.Services** 

**.AddAuthentication(** JwtBearerDefaults **.AuthenticationScheme)** 

**.AddJwtBearer(** options **=>** 

**{** 

options **.Authority =** keycloakAuthority **;** options **.Audience =** keycloakAudience **;** options **.RequireHttpsMetadata =** builder **.Environment.IsProduction();** options **.TokenValidationParameters = new** TokenValidationParameters **{** 

ValidateIssuer **= true,** ValidateAudience **= true,** 

9 

ValidateLifetime **= true,** ClockSkew **=** TimeSpan **.FromSeconds(** 30 **)** 

**}; });** 

builder **.Services.AddAuthorization(** options **=> {** 

options **.AddPolicy(** "OwnerOnly" **,** p **=>** p **.RequireRole(** "owner" **));** options **.AddPolicy(** "SentinelCompany" **,** p **=>** p **.RequireRole(** "owner" **,** ↪ "support-team" **));** options **.AddPolicy(** "CsoOrAbove" **,** p **=>** p **.RequireRole(** "owner" **,** "cso" **));** options **.AddPolicy(** "SecurityAdministratorOrAbove" **,** p **=>** p **.RequireRole(** "owner" **,** "cso" **,** "security-administrator" **));** options **.AddPolicy(** "AnyOrganizationRole" **,** p **=>** p **.RequireRole(** "cso" **,** "security-administrator" **,** "security-analyst" **));** 

_// FIXED — this line was missing in every earlier revision._ ↪ _CanManageUserHandler // was registered below as a service, but nothing ever attached the_ ↪ _string // "CanManageUser" to it as a named policy. Every AuthorizeAsync(user,_ ↪ _role, // "CanManageUser") call — Phase 1's UserManagementService, Phase 3's // UsersController.Invite — was throwing InvalidOperationException_ ↪ _instead // of correctly rejecting. Found by tracing the call graph, not by_ ↪ _testing, // because no test existed that would have caught it._ options **.AddPolicy(** "CanManageUser" **,** p **=>** p **.Requirements.Add(new** ↪ **CanManageUserRequirement())); });** 

builder **.Services.AddScoped<** IOrganizationContext **,** OrganizationContext **>();** builder **.Services.AddScoped<** IUserManagementService **,** ↪ UserManagementService **>();** builder **.Services.AddSingleton<** IAuthorizationHandler **,** ↪ CanManageUserHandler **>();** _// SameOrganizationHandler REMOVED — it was registered here but never_ ↪ _attached // to any named policy and never called anywhere in any phase. Dead code_ ↪ _since // this file was first written. OrganizationIsolationMiddleware already_ ↪ _performs // the equivalent cross-organization check globally, for every request, so_ ↪ _this // wasn't a gap in coverage — just an unused second mechanism that read as // protection without actually protecting anything._ 

10 

builder **.Services.AddControllers();** 

var app **=** builder **.Build();** 

app **.UseAuthentication();** app **.UseMiddleware<** OrganizationIsolationMiddleware **>();** app **.UseAuthorization();** app **.MapControllers();** 

app **.Run();** 

**Where:** backend/src/Sentinel.Api/appsettings.json (already exists — add these inside the existing braces): 

**{** "Keycloak" **: {** "Authority" **:** "http://localhost:8080/realms/sentinel" **,** "Audience" **:** "sentinel-api" **},** "ConnectionStrings" **: {** "Supabase" **:** "Host=aws-0-us-east↪ ↪ password-here" **} }** 

Use your real Supabase pooled connection details. For anything beyond local testing, move the password into an environment variable instead of leaving it in this file. 

#### **How to run just the backend on its own:** 

$ cd path/to/your/project/backend/src/Sentinel.Api $ dotnet run 

**How to know it worked:** terminal prints Now listening on: http://localhost:5000. Visiting that address showing a 404 is expected — there are no routes yet, it just confirms the app started. 

## **7. Add the Identity/Tenant Code** 

All four files below go inside backend/src/Sentinel.Identity/, except the middleware, which goes in backend/src/Sentinel.Api/. 

**Where:** backend/src/Sentinel.Identity/IOrganizationContext.cs 

_// backend/src/Sentinel.Identity/IOrganizationContext.cs_ **namespace** Sentinel **.Identity;** 

11 

**public interface** IOrganizationContext **{** Guid **?** OrganizationId **{ get; }** string Role **{ get; }** bool IsPlatformTier **{ get; }** 

**}** 

**public class** OrganizationContext **:** IOrganizationContext **{ public** Guid **?** OrganizationId **{ get; } public** string Role **{ get; } public** bool IsPlatformTier **=>** Role **is** "owner" or "support-team" **;** 

**public OrganizationContext(** IHttpContextAccessor accessor **) {** 

var user **=** accessor **.HttpContext?.User ?? throw new InvalidOperationException(** "No HTTP context ↪ available." **);** 

Role **=** user **.Claims.FirstOrDefault(** c **=>** c **.Type ==** ↪ System **.Security.Claims.ClaimTypes.Role)?.Value** 

**?? throw new UnauthorizedAccessException(** "Token contains no role ↪ claim." **);** 

var orgClaim **=** user **.FindFirst(** "organizationId" **)?.Value;** OrganizationId **=** string **.IsNullOrEmpty(** orgClaim **) ? null :** ↪ Guid **.Parse(** orgClaim **); } }** 

**Where:** backend/src/Sentinel.Api/OrganizationIsolationMiddleware.cs — note this one goes in the **Api** project, not Identity. 

_// backend/src/Sentinel.Api/OrganizationIsolationMiddleware.cs_ **namespace** Sentinel **.Api;** 

**public class** OrganizationIsolationMiddleware 

**{** 

**private readonly** RequestDelegate _next **;** 

**public OrganizationIsolationMiddleware(** RequestDelegate next **) =>** _next **=** ↪ next **;** 

**public** async Task **InvokeAsync(** HttpContext context **,** IOrganizationContext ↪ orgContext **) {** 

12 

**if (!** context **.User.Identity?.IsAuthenticated ?? true)** 

**{** 

await **_next(** context **); return;** 

**}** 

**if (!** orgContext **.IsPlatformTier &&** orgContext **.OrganizationId is null) {** 

context **.Response.StatusCode =** StatusCodes **.Status403Forbidden;** await context **.Response.WriteAsync(** "No organization scope on ↪ token." **);** 

**return;** 

**}** 

**if (** context **.Request.RouteValues.TryGetValue(** "orgId" **, out** var 

↪ routeOrgIdRaw **)** 

**&&** Guid **.TryParse(** routeOrgIdRaw **?.ToString(), out** var routeOrgId **) && !** orgContext **.IsPlatformTier** 

**&&** routeOrgId **!=** orgContext **.OrganizationId)** 

**{** 

context **.Response.StatusCode =** StatusCodes **.Status403Forbidden;** await context **.Response.WriteAsync(** "Cross-organization access ↪ denied." **);** 

**return;** 

**}** 

await **_next(** context **);** 

**}** 

**}** 

**Removed: SameOrganizationHandler.** Earlier drafts of this document had you create backend/src/Sentinel.Identity/SameOrganizationHandler.cs here. Don’t — it registered a requirement and handler that nothing ever attached to a named policy and nothing ever called via AuthorizeAsync(user, resource, "policyName"), in this phase or any later one. OrganizationIsolationMiddleware, immediately above, already performs the equivalent same-organization check for every request. If you already created this file from an earlier revision, delete it and remove its AddSingleton<IAuthorizationHandler, SameOrganizationHandler>() line from Program.cs (Step 6, already updated above). 

**Where:** backend/src/Sentinel.Identity/CanManageUserHandler.cs — enforces “CSO manages Security Administrators, Security Administrator manages Security Analysts.” This is an account-creation hierarchy check only — it says nothing about policy management, endpoint management, or any other capability; those stay governed by the role-based policies already registered in Step 6 (SecurityAdministratorOrAbove, etc.), unaffected by anything in this section. 

13 

_// backend/src/Sentinel.Identity/CanManageUserHandler.cs_ **namespace** Sentinel **.Identity;** 

**public class** CanManageUserRequirement **:** IAuthorizationRequirement **{ } public class** CanManageUserHandler **:** ↪ AuthorizationHandler **<** CanManageUserRequirement **,** string **> { private static readonly** Dictionary **<** string **,** string **[]>** AllowedTargets **=** ↪ **new() { [** "owner" **] = new[] {** "cso" **}, [** "cso" **] = new[] {** "security-administrator" **}, [** "security-administrator" **] = new[] {** "security-analyst" **} }; protected override** Task **HandleRequirementAsync(** AuthorizationHandlerContext context **,** CanManageUserRequirement requirement **,** string targetRole **) {** var actorRole **=** context **.User.FindFirst(** System **.Security.Claims.** ⌋ ↪ **ClaimTypes.Role)?.Value;** 

**if (** actorRole **is** not **null &&** AllowedTargets **.TryGetValue(** actorRole **, out** var allowed **) &&** allowed **.Contains(** targetRole **)) {** context **.Succeed(** requirement **); } return** Task **.CompletedTask; } }** 

**Where:** backend/src/Sentinel.Identity/UserManagementService.cs 

_// backend/src/Sentinel.Identity/UserManagementService.cs_ **namespace** Sentinel **.Identity;** 

**public interface** IUserManagementService **{** 

Task **<** Guid **> CreateUserAsync(** Guid actingUserOrgId **,** string actingUserRole **,** ↪ string targetRole **,** string email **); }** 

**public class** UserManagementService **:** IUserManagementService 

14 

**{** 

**private readonly** IAuthorizationService _authz **;** 

**public UserManagementService(** IAuthorizationService authz **) =>** _authz **=** ↪ authz **;** 

**public** async Task **<** Guid **> CreateUserAsync(** Guid actingUserOrgId **,** string actingUserRole **,** string targetRole **,** ↪ string email **) {** var actingUser **= new** System **.Security.Claims.ClaimsPrincipal( new** System **.Security.Claims.ClaimsIdentity(new[] { new** System **.Security.Claims.Claim(** System **.Security.Claims.** ⌋ ↪ **ClaimTypes.Role,** ↪ actingUserRole **) }));** var result **=** await _authz **.AuthorizeAsync(** actingUser **,** targetRole **,** ↪ "CanManageUser" **); if (!** result **.Succeeded) { throw new UnauthorizedAccessException(** $"{actingUserRole} cannot create a {targetRole} account." **); } return** Guid **.NewGuid(); } }** 

#### **How to make sure it all still compiles:** 

$ cd path/to/your/project/backend $ dotnet build 

Build succeeded means every file is wired together correctly. Red errors are almost always a file saved in the wrong folder or a missing using line — re-check against the “Where” in each step above. 

## **8. Add the CI Skeleton — This One File Lives at Your Project Root, Not in backend/** 

This only matters once you push your code to GitHub — nothing runs locally from this file. 

**Where:** .github/workflows/ci.yml — at your project’s root, as a sibling of frontend/, 

15 

backend/, and docs/, not inside any of them. _# .github/workflows/ci.yml_ **name:** CI **on: push: branches: [** main **] pull_request: jobs: backend: runs-on:** ubuntu-latest **steps: - uses:** actions/checkout@v4 **- uses:** actions/setup-dotnet@v4 **with: dotnet-version:** "8.0.x" **- name:** Restore **run:** dotnet restore backend/Sentinel.sln **- name:** Build **run:** dotnet build backend/Sentinel.sln --no-restore **- name:** Test **run:** dotnet test backend/Sentinel.sln --no-build --verbosity normal 

Notice the paths inside the file (backend/Sentinel.sln) — these are written relative to your project’s root, because that’s the folder GitHub Actions checks out. This is exactly why the workflow file itself has to live at the root: it’s describing paths starting from there. 

This step also doesn’t spin up a local test database — tests that need one should point at a separate Supabase project made specifically for testing, never your real one. 

## **9. Verification Checklist for Phase 1** 

- docker compose up (run from inside backend/) starts Redis, Neo4j, and Keycloak with no errors. 

- http://localhost:8080 shows Keycloak’s login page; the sentinel realm has exactly 5 roles. 

- Supabase’s **Table Editor** shows organizations (one company_scope = true row, slug = 'sentinel-company') and users, plus every other table the corrected full schema defines — this step now bootstraps the whole project, not just Phase 1’s slice (Step 4). 

- dotnet build (run from inside backend/) succeeds with no red errors. 

- dotnet run (from backend/src/Sentinel.Api) starts the API and prints a localhost:5000 address. 

16 

- You’ve manually created one test user per role (cso, security-administrator, security-analyst) in Keycloak, each with an organizationId attribute, and confirmed the claim appears in their token (test tokens only — never paste a real production token into an external site like jwt.io). 

- **New:** a cso token attempting to create a security-administrator account succeeds, and a security-analyst token attempting the same is rejected with 403 — **not** a 500. If you see a 500 with InvalidOperationException: No policy found with the name 'CanManageUser', the policy registration fix in Step 6 wasn’t applied — this was a real, silent bug in every revision before this one, and it fails loud enough now that you’ll know immediately if it regresses. 

- A Security Administrator token attempting to create another securityadministrator or a cso account is rejected once you’ve wired UserManagementService into a controller. 

## **10. Deliberately Not in Phase 1** 

No endpoint registration, no telemetry ingestion, no licensing enforcement, no onboarding application flow, no SOAR, no detection engine, and no frontend/ work. Those depend on everything above being verified first. 

17 

