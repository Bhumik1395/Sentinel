# **Contents** 

|**Sentinel — Application Flow Document**<br>**2**|
|---|
|Table of Contents<br>. . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . .<br>2|
|1. Platform Overview<br>. . . . . . . . . . . . . . . . . . . . . . . . . . . . . .<br>3|
|2. System Architecture<br>. . . . . . . . . . . . . . . . . . . . . . . . . . . . .<br>3|
|3. User Roles & Access . . . . . . . . . . . . . . . . . . . . . . . . . . . . .<br>5|
|4. Authentication Flow<br>. . . . . . . . . . . . . . . . . . . . . . . . . . . . .<br>7|
|5. Agent Lifecycle Flow . . . . . . . . . . . . . . . . . . . . . . . . . . . . .<br>9|
|6. Telemetry Collection & Normalization Flow<br>. . . . . . . . . . . . . . . .<br>11|
|7. Event Processing Pipeline . . . . . . . . . . . . . . . . . . . . . . . . . .<br>13|
|8. Detection Engine Flow . . . . . . . . . . . . . . . . . . . . . . . . . . . .<br>14|
|9. ML Anomaly Detection Flow . . . . . . . . . . . . . . . . . . . . . . . . .<br>16|
|10. Alert Generation & Deduplication Flow<br>. . . . . . . . . . . . . . . . . .<br>17|
|11. Incident Correlation Flow<br>. . . . . . . . . . . . . . . . . . . . . . . . .<br>18|
|12. Attack Graph Flow<br>. . . . . . . . . . . . . . . . . . . . . . . . . . . . .<br>19|
|13. Threat Hunting Flow<br>. . . . . . . . . . . . . . . . . . . . . . . . . . . .<br>20|
|14. Investigation Workfows<br>. . . . . . . . . . . . . . . . . . . . . . . . . .<br>21|
|15. Reporting Flow<br>. . . . . . . . . . . . . . . . . . . . . . . . . . . . . . .<br>23|
|16. Sentinel Desktop Application & Public Website Flow . . . . . . . . . . .<br>24|
|17. IOC Management Flow . . . . . . . . . . . . . . . . . . . . . . . . . . .<br>27|
|18. User & Organization Management Flow . . . . . . . . . . . . . . . . . .<br>28|
|19. Policy Management Flow . . . . . . . . . . . . . . . . . . . . . . . . . .<br>29|
|20. Failure Handling & Degradation . . . . . . . . . . . . . . . . . . . . . .<br>29|
|21. Platform Administration Flow (Owner)<br>. . . . . . . . . . . . . . . . . .<br>30|
|22. Performance Boundaries . . . . . . . . . . . . . . . . . . . . . . . . . .<br>32|
|23. Manual Onboarding & Public Website Flow . . . . . . . . . . . . . . . .<br>32|
|24. Licensing Flow<br>. . . . . . . . . . . . . . . . . . . . . . . . . . . . . . .<br>34|
|25. SOAR Response Flow . . . . . . . . . . . . . . . . . . . . . . . . . . . .<br>35|
|26. Agent Command Channel (Architectural Note)<br>. . . . . . . . . . . . . .<br>35|
|27. Deception Technology Flow<br>. . . . . . . . . . . . . . . . . . . . . . . .<br>36|
|28. Owner Analytics Flow . . . . . . . . . . . . . . . . . . . . . . . . . . . .<br>36|
|Appendix A — Technology Stack<br>. . . . . . . . . . . . . . . . . . . . . . . .<br>36|
|Appendix B — Containerized Services (Docker Compose)<br>. . . . . . . . . .<br>37|
|Appendix C — Deferred Features . . . . . . . . . . . . . . . . . . . . . . . .<br>37|



1 

# **— Sentinel Application Flow Document** 

· **Source Documents:** Sentinel PRD Sentinel TRD **Status:** Engineering Reference 

**Audit Pass 2 note:** Sections 7 and 8 previously described detection as “parallel consumption from Redis Streams” across four separate engines. This contradicted TRD §20, which specifies a single Redis Streams consumer (sentinel-detection-group, 1 detection worker) with multiple/distributed consumers explicitly deferred to a future release. The wording below is corrected to match: the four detection engines run as in-process modules within one consumer worker, not as independent Redis Streams consumers. 

## **Table of Contents** 

1. Platform Overview 

2. System Architecture 

3. User Roles & Access 

4. Authentication Flow 

5. Agent Lifecycle Flow 

6. Telemetry Collection & Normalization Flow 

7. Event Processing Pipeline 

8. Detection Engine Flow 

9. ML Anomaly Detection Flow 

10. Alert Generation & Deduplication Flow 

11. Incident Correlation Flow 

12. Attack Graph Flow 

13. Threat Hunting Flow 

14. Investigation Workflows 

15. Reporting Flow 

16. Sentinel Desktop Application & Public Website Flow 

17. IOC Management Flow 

18. User & Organization Management Flow 

19. Policy Management Flow 

20. Failure Handling & Degradation 

2 

21. Platform Administration Flow (Owner) 22. Performance Boundaries 

## **1. Platform Overview** 

Sentinel is a **self-hosted, AI-assisted Windows Endpoint Detection and Response (EDR) platform** with investigation, approval-gated response, and deception capabilities. ⚠ “AI-assisted” refers specifically to the ML Anomaly Detection Engine (Isolation Forest, Section 9) — it does not imply LLM-based investigations, AI copilots, autonomous reasoning, natural-language threat hunting, or any other AI capability not otherwise specified. It provides: 

- Endpoint telemetry collection from Windows endpoints via Windows-native sources (ETW, Sysmon, AMSI, Windows Event Logs) — deep system visibility without a custom kernel-mode driver 

- Threat detection (behavioral rules, Sigma rules, IOC matching, ML anomaly detection) 

- Incident correlation and investigation workflow 

- Attack graph visualization 

- Threat hunting over historical telemetry 

- Security reporting 

- Discrete, human-approval-gated response actions (Kill Process, Endpoint Isolation, Create Incident) and deception assets (Honey Files, Canary Tokens) 

**Sentinel does not perform autonomous or playbook-driven response.** SOAR actions require explicit approval — by a Security Administrator by default, or a CSO when the organization is configured for executive-approval mode (Section 25) — before any action executes. 

## **2. System Architecture** 

### **High-Level Data Flow** 

Windows Endpoint ↓ 

Sentinel Agent 

(Telemetry Collector →Policy Engine →Normalization Engine →Local Buffer [SQLite]) ↓HTTPS (TLS 1.3 + Certificate Pinning) ASP.NET Core API 

↓ 

Redis Streams (sentinel-events) 

↓ 

Detection Pipeline 

├──Behavioral Rules Engine 

3 

├──Sigma Engine ├──IOC Matching Engine 

└──ML Anomaly Detection Engine ↓ 

— PostgreSQL Supabase-hosted (telemetry, alerts, incidents, users, config) Neo4j — self-hosted (entity relationships, attack graph data) ↓ 

Incident Correlation Engine →Threat Hunting Engine →Reporting Engine ↓ 

Desktop Application API ↓ 

Sentinel Desktop Application (Electron + React, installed via SentinelConsole.msi) 

### **Component Responsibilities** 

|Component|Responsibility|
|---|---|
|Sentinel Agent|Collect, normalize, bufer, and transmit<br>telemetry; health heartbeats|
|ASP.NET Core API|Authentication, event ingestion,<br>detection processing, dashboard APIs|
|Redis Streams (sentinel-events)|Event pipeline, bufering, detection<br>queue|
|Behavioral Rules Engine|Native rule-based detection (15 rules)|
|Sigma Engine|Community-rule detection (25–40 rules)|
|IOC Matching Engine|Signature-based detection against IOC<br>dataset|
|ML Anomaly Engine|Anomaly scoring; Low/Medium severity<br>standalone alerts|
|Incident Correlation Engine|Group related alerts into incidents|
|Neo4j|Store entity relationships and attack<br>path data|
|PostgreSQL (Supabase-hosted)|Store all operational data (events, alerts,<br>incidents, users, policies)|
|Keycloak|Authentication, identity management,<br>RBAC enforcement|
|Sentinel Desktop Application|Electron + React; UI for monitoring,<br>investigation, hunting, reporting.<br>Installed via SentinelConsole.msi,<br>separate from the Agent installer (see<br>Section 23a).|
|Public Website|React, unauthenticated; marketing,<br>Apply, Download only. Separate deploy<br>from the Desktop Application.|



4 

### **Architectural Principles** 

1. **Normalize Before Processing** — No component consumes raw telemetry. All telemetry is converted to SUES first. 

2. **Decouple Ingestion From Detection** — Redis Streams separates event ingestion from detection execution. 

3. **Store Relationships Separately** — PostgreSQL (Supabase-hosted) for operational data; Neo4j (self-hosted) for entity relationships only. Raw telemetry is never stored in Neo4j. 

4. **Security By Default** — TLS 1.3, certificate pinning, JWT authentication, RBAC on all communications. 

5. **Graceful Degradation** — Non-critical service failures do not stop the platform (e.g., Neo4j failure →graphs unavailable but detection continues). 

6. **Detection Confidence Tiering** — ML engine may independently raise only Low/Medium severity. High/Critical severity requires a deterministic source (Behavioral Rules, Sigma, or IOC Matching). 

## **3. User Roles & Access** 

Roles are organized into two tiers: **Sentinel Company** — a persisted platform-level entity containing Owner and Support Team, with platform-wide permissions — and **Customer Organization** — each a licensed tenant. 

### **Role Definitions** 

**Owner** _(Sentinel Company)_ - Platform-level role; exists above all organizational boundaries - Responsibilities: create/delete organizations, assign first CSO per organization, license management, platform configuration, system recovery, onboarding approval, cross-organization analytics - Access scope: all organizations, all users, all endpoints, all reports - Not responsible for day-to-day security investigations 

**Support Team** _(Sentinel Company)_ - Platform-wide (not per-organization) role, distinct from Owner - Responsibilities: review onboarding applications, onboard organizations, provide temporary time-limited support access to a customer organization’s telemetry - No SOAR trigger/approval authority; no unrestricted or standing crossorganization access — access to any organization’s data is limited to an active, timeboxed support engagement 

**CSO (Chief Security Officer)** _(Customer Organization)_ - Executive-level role, above Security Administrator within the organization - Responsibilities: organization ownership, executive oversight, **manage Security Administrators** (create/deactivate Security Administrator accounts), licensing/subscription point of contact, report review, SOAR approval **only when the organization is configured for executive-approval mode** (default is Security Administrator approval — see Section 25) - Access scope: own organization only 

**Security Administrator** _(Customer Organization)_ - Organization-level role - Re- 

5 

~~acs UU~~ UU U U U UU UU U U U O (all O U (own O O O (all O O U (own U 

O O O U (own O O O O U (own O 

O O O U (own O 

U 

U U U 

~~UU U~~ U U U U U U U U 0 (cannot 

UU 

1(all O [] (own [] (own [] (own UU 1(all O 1 (all 1 (all 1] (excl. 

UU U0 0 (own 1] (own UU <u>UU</u> U U U 

↓ Refresh Token issued (7-day expiry) 

   - ↓ 

- Dashboard Access granted ↓ 

- [On token expiry] Refresh Token used →new Access Token issued ↓ 

- [On logout] Active session invalidated 

### **Token Details** 

- **Access Token:** 15-minute validity 

- **Refresh Token:** 7-day validity 

- **On logout:** session immediately invalidated 

### **Account Lockout** 

- ⚠ Threshold: 5 failed login attempts _(confirm against PRD source)_ 

- Lockout Duration: 15 minutes 

- Implemented via Keycloak brute-force detection 

### **Password Policy** 

- Minimum 12 characters 

- Must include: uppercase letter, lowercase letter, number, special character 

- Storage algorithm: Argon2id (Keycloak default) 

### **Password Reset Flow** 

User requests password reset 

↓ 

Single-use, securely random reset token generated 

   - ↓ 

- Reset link delivered via email (SMTP required) ↓ 

- ⚠ Token expires after 15 minutes (confirm against PRD source) ↓ 

- User sets new password ↓ 

- Token invalidated 

SMTP configuration is **mandatory** for production. Without it, password reset is unavailable. 

### **Owner Break-Glass Recovery** 

If the Owner account is locked: 

8 

Recovery via Keycloak Admin Console or kcadm.sh (bypasses Sentinel application layer entirely) 

Security Administrators have no authority to recover Owner accounts. 

### **Keycloak Bootstrap (First Deployment)** 

Docker startup scripts execute ↓ Sentinel Realm created automatically ↓ Dashboard Client + API Client created ↓ owner, security-administrator, security-analyst roles created ↓ 

Bootstrap Owner account created ↓ 

Owner creates first Organization 

↓ 

Owner assigns first Security Administrator to that Organization 

## **5. Agent Lifecycle Flow** 

### **Installation** 

Administrator packages `SentinelAgent.msi` ↓ Deploy via: Manual Installation | GPO | SCCM | Microsoft Intune ↓ `SentinelAgent.msi` runs on the Windows Endpoint being protected — never on an analyst/CSO/admin workstation unless that machine is itself a ↓ 

Trusted server certificate + fingerprint embedded during installation ↓ 

Sentinel Agent installed as Windows Service 

↓ 

Startup Type: Automatic (starts on every endpoint boot) 

### **Agent Startup Flow** 

Endpoint boots ↓ 

Sentinel Agent Windows Service starts automatically 

↓ 

Agent loads local configuration from SQLite 

↓ 

9 

Agent checks current Policy Version ↓ 

Agent connects to backend API (HTTPS + TLS 1.3 + Certificate Pinning) 

↓ 

Certificate pinning validated: ├──PASS →communication established 

└──FAIL →transmission blocked; failure logged; agent health →DEGRADED; local buffering ↓ Agent begins telemetry collection per active policy ↓ Agent begins 60-second heartbeat cycle 

### **Agent Health Monitoring** 

#### Heartbeat interval: **60 seconds** 

Heartbeat payload: 

##### **{** 

"endpointId" **:** "endpoint-001" **,** "agentVersion" **:** "1.0.3" **,** "policyVersion" **:** "4" **,** "status" **:** "HEALTHY" **,** "timestamp" **:** "2026-06-18T10:00:00Z" 

##### **}** 

|Agent Status|Trigger|
|---|---|
|HEALTHY|Heartbeat received normally; all<br>components operational|
|DEGRADED|3 consecutive missed heartbeats; or<br>partial functionality loss (API<br>unreachable, telemetry failure, bufer<br>nearing capacity, certifcate validation<br>failure)|
|OFFLINE|10 consecutive missed heartbeats|



### **Certificate Pinning Validation Failure** 

Certificate fingerprint validation fails 

↓ 

Telemetry transmission blocked ↓ Failure event logged locally ↓ 

Agent health marked DEGRADED ↓ 

Local telemetry buffering continues 

10 

### **Certificate Compromise Recovery** 

Certificate compromised ↓ New certificate generated ↓ Agent redeployed (manual `SentinelAgent.msi` reinstall) ↓ 

Manual trust replacement 

Automatic certificate rotation is deferred to a future release. 

### **Agent Upgrade** 

Manual SentinelAgent.msi redeployment. Automatic update checks and staged rollouts are deferred to a future release. (The Desktop Application, SentinelConsole.msi, is upgraded independently — see Section 23a.) 

## **6. Telemetry Collection & Normalization Flow** 

### **Collection Sources** 

|Source|Event Types|
|---|---|
|ETW (Event Tracing for Windows)|Process activity, network activity,<br>security events|
|AMSI|Script content, script execution,<br>PowerShell activity|
|Sysmon|Process creation, network connections,<br>registry activity, fle activity|
|Windows Event Logs|Authentication events, security events,<br>system events|



### **Policy Engine Control** 

↓ 

Policy Engine applies toggles: ├──Collect Process Events: ON/OFF ├──Collect Network Events: ON/OFF ├──Collect Registry Events: ON/OFF └──Collect File Events: ON/OFF 

↓ Active policy version reported in every heartbeat 

11 

Policy sync mechanism: the heartbeat response carries the current policyVersion. If it differs from the agent’s cached version, the agent fetches the full policy document from the dedicated policy endpoint. 

### **Normalization Flow** 

Raw telemetry event received from ETW / AMSI / Sysmon / Event Logs ↓ Normalization Engine converts to SUES (Sentinel Unified Event Schema) ↓ SUES event written to SQLite local buffer ↓ 

SUES event queued for transmission to backend API 

#### **No component downstream of the agent ever processes raw telemetry.** 

### **SUES — Sentinel Unified Event Schema** 

Every event must include these mandatory fields: 

|Field|Description|
|---|---|
|eventId|Unique Event ID|
|schemaVersion|Schema version (e.g., "1.0")|
|eventType|One of: PROCESS_START, PROCESS_STOP,<br>NETWORK_CONNECTION, DNS_QUERY,<br>FILE_ACCESS, FILE_MODIFICATION,<br>REGISTRY_MODIFICATION,<br>POWERSHELL_EXECUTION, ALERT, INCIDENT|
|source|Telemetry source|
|timestamp|Event timestamp|
|organizationId|Organization identifer|
|endpointId|Endpoint identifer|
|hostname|Hostname|
|userId|Associated user|
|severity|INFORMATIONAL / LOW / MEDIUM / HIGH /<br>CRITICAL|



Optional fields: processName, processId, parentProcessName, parentProcessId, commandLine, domain, ipAddress, destinationPort, protocol, filePath, registryKey, alertId, incidentId. 

### **Local Buffer Management** 

- **Technology:** SQLite 

- **Maximum Buffer Size:** 1 GB 

- **Eviction Trigger:** Buffer utilization exceeds 10% 

12 

#### **Telemetry Priority Levels:** 

|Priority|Examples|
|---|---|
|High|Detection Alerts, Authentication Events,<br>AMSI Events, Process Creation Events,<br>Security Events|
|Medium|Network Events, Registry Events, File<br>Events|
|Low|Informational System Events, Diagnostic<br>Events, Performance Events|



#### **Eviction Order (lowest priority first):** 

Low Priority evicted first 

↓ Medium Priority evicted next ↓ High Priority retained whenever possible 

Telemetry collection continues uninterrupted during eviction. 

### **Delivery Retry Policy** 

Event transmission fails 

↓ Event retained in SQLite buffer ↓ Retry every 30 seconds ↓ Unlimited retry attempts while buffer space remains available ↓ 

Failure logged locally 

## **7. Event Processing Pipeline** 

End-to-end event flow from endpoint to storage: 

1. Collection 

   - ETW / AMSI / Sysmon / Windows Event Logs →Raw Events 

2. Normalization 

   - Raw Events →SUES format (Normalization Engine) 

3. Local Buffering 

   - SUES Events →SQLite (offline operation and reliability) 

13 

4. API Ingestion 

   - Agent →HTTPS (TLS 1.3 + Cert Pinning) →ASP.NET Core API 

5. Redis Streams 

   - API →sentinel-events stream (Consumer Group: sentinel-detection-group) 

6. Detection Processing (single detection worker consuming `sentinelevents` via consumer group `sentinel-detection-group`; the four engines below run as inprocess modules within that one worker, not as separate Redis consumers — exactly 1 detection worker, per TRD §20) 

   - ├──Behavioral Rules Engine 

   - ├──Sigma Engine 

   - ├──IOC Matching Engine 

   - └──ML Anomaly Engine 

7. Storage 

   - Detection results →PostgreSQL (alerts, incidents, events) Entity relationships →Neo4j (attack graph) 

8. Presentation 

   - Processed data →Desktop Application API →Sentinel Desktop Application (Alerts, Incidents, Threat Hunting, Attack Graphs, Reports) 

## **8. Detection Engine Flow** 

### **Purpose** 

Identify malicious or suspicious activity from normalized SUES events consumed from Redis Streams. 

### **Behavioral Rules Engine** 

SUES event received from Redis Streams 

↓ Rules evaluated in priority order: Critical →High →Medium →Low ↓ 

- Multi-match enabled: one event may trigger multiple rules ↓ 

- Match found →Alert generated 

   - ↓ 

- Alert deduplication check (60-second window, same rule + endpoint + user) ├──Duplicate →occurrence counter incremented, no new alert created └──New →alert persisted to PostgreSQL + Neo4j updated 

#### **Detection Categories (15 native rules):** 

14 

|Category|Examples|
|---|---|
|Initial Access|Ofice →|
||PowerShell/CMD/WScript/MSHTA,<br>suspicious PDF reader child process|
|Execution|Encoded PowerShell, download cradle,<br>Rundll32/Regsvr32/Certutil/MSHTA/WMI<br>abuse|
|Persistence|Run registry keys, startup folder,<br>scheduled tasks, service creation|
|Defense Evasion|AMSI tampering, PowerShell<br>obfuscation, Sysmon disabled, Sentinel<br>service disabled|
|Credential Access|LSASS access, Mimikatz indicators|
|Discovery|User enumeration, group enumeration,<br>host discovery|
|Lateral Movement|Pass-the-hash, remote service creation,<br>admin share access, PsExec-style<br>execution|
|Command & Control|DNS tunneling, beaconing indicators,<br>known malicious domains/IPs|



**Escalation:** Alerts mapped to Credential Access or Defense Evasion carry a minimum severity of **High** . 

### **Sigma Engine** 

Sigma Rules →Build-Time Conversion →C# Detection Predicates ↓ [At runtime] SUES event evaluated against C# predicates ↓ 

Match →Alert generated 

- 25–40 Sigma Rules operational 

- Runtime Sigma compilation is not supported; dynamic rule updates are deferred to a future release 

### **IOC Matching Engine** 

SUES event received 

↓ Incoming event fields (IP, Domain, URL, File Hash) compared against IOC dataset ↓ 

Match found →Alert generated ↓ 

Alert persisted to PostgreSQL 

15 

### **Alert Output** 

Every generated alert contains: 

· · · · · · · Alert ID Rule ID Severity Endpoint User MITRE Technique Timestamp · Evidence Occurrence Count 

### **Alert Generation SLA** 

**High-severity alerts must be visible on the dashboard within <5 seconds** from event occurrence (end-to-end: collection →normalization →Redis →detection →persistence →dashboard poll). 

## **9. ML Anomaly Detection Flow** 

### **Role** 

The ML engine is a supplementary detector. It: 1. Acts as a standalone Low/Medium severity detector 2. Provides a confidence signal (Anomaly Score) that enriches alerts from other engines 

It **never** independently generates High or Critical severity alerts. 

### **Learning Period** 

- New endpoint registered ↓ 

- 7-day learning period begins 

   - ↓ 

During learning period: 

- Anomaly scores generated and recorded 

- Only Informational-severity alerts produced 

- Informational alerts visible on Alerts page 

- Informational alerts DO NOT participate in Incident Correlation 

- Informational alerts DO NOT affect incident severity 

- ↓ 

After 7 days: full anomaly detection operational (Low/Medium alerts possible) 

### **Detection Flow (Post-Learning)** 

SUES events consumed from Redis Streams 

- ↓ 

- Feature extraction per endpoint: 

   - (process frequency, parent-process frequency, execution time patterns, network volume, domain diversity, login activity patterns) 

   - ↓ 

Isolation Forest model scores event against per-endpoint baseline 

16 

↓ 

Anomaly Score generated 

↓ 

Score used for: 

├──Standalone Low/Medium severity alert (if threshold met) ├──Confidence signal attached to rule-based alerts (enrichment) └──Surfacing unknown/novel threats 

↓ 

Results persisted to PostgreSQL 

### **Endpoint Reimaging** 

Endpoint reimaged ↓ New Endpoint ID generated ↓ Previous ML baseline NOT reused ↓ New 7-day learning period begins under new Endpoint ID 

## **10. Alert Generation & Deduplication Flow** 

### **Alert Creation** 

Detection engine (Behavioral / Sigma / IOC / ML) produces a match ↓ Deduplication check: 

Same rule + same endpoint + same user within 60-second window? ├──YES (duplicate) →occurrence counter on existing alert incremented └──NO (new) →new alert record created in PostgreSQL 

↓ 

Alert persisted to PostgreSQL ↓ Neo4j relationship extraction ↓ 

Neo4j relationship extraction triggered (synchronous) 

Alert available via Dashboard API ↓ Dashboard polls alerts every 5 seconds →alert appears on dashboard 

### **Alert Severities** 

· · · · INFORMATIONAL LOW MEDIUM HIGH CRITICAL 

- Informational: ML learning period only; not correlated into incidents 

- High/Critical from ML: requires confirming signal from Behavioral, Sigma, or IOC engine 

17 

## **11. Incident Correlation Flow** 

### **Purpose** 

Group related alerts into incidents to reduce analyst workload and provide attackchain context. 

### **Correlation Flow** 

New alert generated and persisted 

↓ 

- Correlation Engine evaluates: does an open incident exist that matches ALL five criteria? 1. Endpoint — same endpoint as an existing open incident 

2. User — same user as the existing incident 

3. Process — same process (by name or PID) as the existing incident 

4. Time Window — alert falls within the correlation window (default: 15 minutes) 

↓ ├──ALL FIVE match →alert added to existing incident 

- └──Any factor fails, OR alert arrives after correlation window →new incident created 

**Post-window behavior:** An alert arriving after the 15-minute correlation window **always creates a new incident** — no exceptions (per PRD §13). 

### **Incident Severity Escalation** 

Alert added to incident ↓ Severity reassessed: 

- Incident severity = highest severity among all constituent alerts 

- Credential Access or Defense Evasion alert →minimum severity HIGH 

- Multiple High-severity alerts in one incident →may escalate to CRITICAL 

### **Incident States** 

Alert correlated →Incident created (status: Open) ↓ 

Analyst picks up →status: Investigating ↓ 

Resolution found →status: Resolved 

↓ Formally closed →status: Closed 

18 

### **Manual Incident Operations (Analyst/Admin)** 

- **Merge incidents:** highest severity wins; most active status wins (Investigating > Open > Resolved > Closed); primary incident assignee retained; all alerts, notes, evidence, entities, and timelines preserved 

- **Split incidents:** move alerts to a new or different incident 

- **Reassign alerts:** move individual alerts between incidents 

### **Incident Assignment** 

- Incidents are **unassigned by default** 

- Security Analysts may self-assign incidents 

- Security Administrators may assign incidents to analysts within their organization 

- Only one primary assignee at a time 

### **Incident Alert Capacity** 

- Unlimited alerts per incident 

- Incident Details page paginates at 100 alerts per page 

## **12. Attack Graph Flow** 

### **Purpose** 

Visualize entity relationships and attack paths to provide analysts with a complete picture of how an attack progressed. 

### **Graph Generation Flow** 

Alert generated and persisted to PostgreSQL ↓ Relationship Extraction triggered (synchronous) ↓ Entity nodes created or updated in Neo4j: (User, Endpoint, Process, File, Domain, IP Address, Alert, Incident) ↓ 

- Relationships written to Neo4j: 

   - (LOGGED_INTO, EXECUTED, CREATED, ACCESSED, MODIFIED, DELETED, LOADED, CONNECTED_TO, RESOLVES_TO, TRIGGERED_BY, AFFECTS, PART_OF, INVOLVES) ↓ 

Graph available via Dashboard API →Attack Graph page 

### **Supported Node Types** 

· · · · · · · User Endpoint Process File Domain IP Address Alert Incident 

19 

### **Key Relationship Types (14 total)** 

|Relationship|Example|
|---|---|
|LOGGED_INTO (User →Endpoint)|Alice LOGGED_INTO PC-001|
|EXECUTED (Endpoint →Process)<br>CREATED (Process →Process)<br>ACCESSED (Process →File)|PC-001 EXECUTED powershell.exe<br>winword.exe CREATED powershell.exe<br>powershell.exe ACCESSED salary.xlsx|
|CREATED (Process →File)<br>MODIFIED (Process →File)|powershell.exe CREATED payload.exe<br>ransomware.exe MODIFIED<br>document.docx|
|DELETED (Process →File)|malware.exe DELETED backup.zip|
|LOADED (Process →File)|rundll32.exe LOADED malicious.dll|
|CONNECTED_TO (Process →Domain)|powershell.exe CONNECTED_TO<br>evil.com|
|RESOLVES_TO (Domain →IP Address)|evil.com RESOLVES_TO 185.66.22.11|
|TRIGGERED_BY (Alert →|Encoded PowerShell Alert|
|Process/File/Domain/IP)|TRIGGERED_BY powershell.exe|
|AFFECTS (Alert →Endpoint)|Credential Dumping Alert AFFECTS<br>PC-001|
|PART_OF (Alert →Incident)|PowerShell Alert PART_OF Incident-123|
|INVOLVES (Incident →User)|Incident-123 INVOLVES Alice|



### **Dashboard Interaction (Attack Graph Page)** 

- Powered by Neo4j + Cytoscape.js 

- Supports: node expansion, relationship exploration, entity inspection, attack path visualization 

### **Failure Handling** 

Neo4j becomes unavailable ↓ Alert processing continues � Incident processing continues � Attack Graph page →unavailable � 

## **13. Threat Hunting Flow** 

### **Purpose** 

Enable analysts to proactively search historical telemetry without waiting for alerts. 

### **Supported Search Targets** 

· · · · · · · Users Endpoints Processes Files Domains IP Addresses Alerts Incidents 

20 

### **Search Flow** 

Analyst opens Threat Hunting page ↓ Analyst selects filters: 

Time Range | Endpoint | User | Process | File | Domain | IP Address | Severity ↓ 

Query submitted to backend API ↓ Backend queries PostgreSQL (scoped to analyst's organization) ↓ 

Results returned (<4 seconds for normal workloads) 

↓ Results displayed in table: 

Timestamp | Entity Type | Entity Name | Endpoint | User | Severity | Related Alert Count ↓ 

Analyst pivots between related entities using Entity Pivoting 

### **Entity Pivoting** 

Analysts can navigate directly between related entities without manual searching: 

User →Endpoint →Process →File →Domain/IP →Alert →Incident 

### **Search Scope by Role** 

|Role|Scope|
|---|---|
|Owner|All organizations|
|Security Administrator|Own organization only|
|Security Analyst|Own organization only|



### **Performance** 

Normal hunting queries must return results within **<4 seconds** under supported deployment scale (100 endpoints). 

## **14. Investigation Workflows** 

Four named investigation workflows guide analysts from initial signal to root cause. The end-to-end investigation SLA from alert visibility to root-cause identification is **≤10 minutes** . 

### **Workflow 1 — Alert-Based Investigation** 

Alert (on Alerts page) 

21 

↓ Endpoint details ↓ Process details ↓ File details ↓ Domain / IP details ↓ 

Incident (attack chain context) 

### **Workflow 2 — User-Based Investigation** 

User (on Users page or Threat Hunting) ↓ Endpoints associated with user ↓ Processes run by user on those endpoints ↓ 

Alerts linked to those processes 

↓ 

Incidents involving those alerts 

### **Workflow 3 — IOC-Based Investigation** 

Domain / IP / File Hash (from IOC dataset or Threat Hunting search) ↓ Files (if applicable) ↓ Affected Endpoints ↓ Processes touching those endpoints/files ↓ 

Alerts generated 

↓ Incidents containing those alerts 

### **Workflow 4 — File Investigation** 

File (from Threat Hunting or Alert evidence) ↓ Creating Process ↓ Parent Process ↓ Endpoint ↓ User 

22 

↓ Related Alerts 

## **15. Reporting Flow** 

### **Report Types** 

|Report|Contents|
|---|---|
|Incident Report|Incident summary, severity, timeline,<br>related alerts, related entities,<br>investigation notes, resolution actions,<br>closure details|
|Security Operations Report|Reporting period, alert statistics,<br>incident statistics, detection statistics,<br>endpoint statistics, analyst activity, trend<br>analysis|
|Executive Summary Report|Reporting period, security overview,<br>critical incidents, incident/detection<br>trends, risk overview, key metrics|



### **Report Generation Flow (Manual/On-Demand)** 

Analyst or Administrator opens Reports page 

↓ Selects report type: Incident | Security Operations | Executive Summary ↓ Applies filters: Time Range | Severity | Endpoint | User | Incident Status ↓ Clicks Generate ↓ 

Backend generates report (target: within 30 seconds for normal deployment sizes) ↓ [If report is large] Asynchronous background job generates report artifact ↓ Report available for export: CSV or JSON (PDF deferred to a future release) 

### **Access Control** 

|Role|Report Scope|May Generate|
|---|---|---|
|Owner|All organizations|Incident, Security|
|||Operations, Executive<br>Summary|



23 

|Role|Report Scope|May Generate|
|---|---|---|
|Security Administrator|Own organization only|Incident, Security<br>Operations, Executive<br>Summary|
|Security Analyst|Own organization only|Incident, Security<br>Operations only (not<br>Executive Summary)|



Access is enforced at the authentication, authorization, API, and database query layers — not the UI layer alone. 

### **Audit Logging for Reports** 

· · Every report action is logged: Report Generation Report Export Report Download 

· · · · Logged fields: User Role Timestamp Report Type Export Format 

## **16. Sentinel Desktop Application & Public Website Flow** 

**Revision note:** superseded from “Dashboard Flow.” The authenticated interface is the Sentinel Desktop Application (Electron + React, SentinelConsole.msi), not a browser dashboard. The public website (16a) is a fully separate, unauthenticated artifact. 

### **16a. Public Website Pages (Unauthenticated)** 

|Page|Content|
|---|---|
|**Home / Features / Pricing / About**<br>**Contact**|Marketing content<br>Contact email|
|**Apply**|Company application submission —<br>Section 23|
|**Privacy Policy / Terms & Conditions**<br>**Download**|Legal<br>Two installer downloads, clearly labeled:<br>SentinelConsole.msi (Desktop<br>Application, for Security<br>Analysts/Administrators/CSOs) and<br>SentinelAgent.msi (Windows Service,<br>for protected endpoints) — see Section<br>23a|



There is no Login page here — login only happens inside the Desktop Application (16b). No dashboard content, no authenticated routes, no OIDC client of any kind ships in this artifact. 

24 

### **16b. Sentinel Desktop Application Pages (Authenticated)** 

#### **Customer Organization view:** 

|Page|Content|
|---|---|
|**Overview**|Total/Healthy/Degraded/Ofine<br>Endpoints, Active/Critical/High Alerts,<br>Open/Investigating/Critical Incidents,<br>Recent Alerts, Recent Incidents,<br>Detection Trends|
|**Endpoints**|Inventory, health status, agent version,<br>policy version, last heartbeat|
|**Alerts**|Severity, alert name, endpoint, user,<br>detection source, MITRE technique,<br>timestamp, status;|
||search/flter/sort/paginate|
|**Incidents**|Summary, timeline, evidence, notes,<br>related alerts, related entities, attack<br>graph|
|**Threat Hunting**|Search interface, flters, results table,<br>entity pivot links|
|**Attack Graph**|Neo4j + Cytoscape.js, rendered inside<br>the Desktop Application’s embedded<br>web content; node expansion,<br>relationship exploration, entity<br>inspection, attack path visualization|
|**SOAR**|Action request form (Security Analyst),<br>approval queue (CSO/Security<br>Administrator), audit history — Section<br>25|
|**Deception**|Honey File / Canary Token deployment<br>(Security Administrator), trigger history<br>— Section 27|
|**Reports**|Incident, Security Operations, Executive<br>Summary reports|
|**Users**|User accounts, assigned roles, account<br>status (Owner: all orgs; CSO/Security<br>Administrator: own org). Account<br>creation is by self-service invitation, not<br>manual CRUD-only setup — CSO invites<br>Security Administrators, Security<br>Administrator invites Security Analysts,<br>each provisioning Keycloak + sending an<br>activation email. See Section 23b.|
|**Settings — Platform**|Owner only: Licensing, global platform<br>settings, certifcate management, system<br>confguration|



25 

|Page|Content|
|---|---|
|**Settings — Organization**|CSO/Security Administrator: user<br>management, IOC management, policy<br>management, organization settings.<br>_(Unchanged: policy management is_<br>_Security Administrator’s, not CSO’s.)_|



#### **Sentinel Company view (Owner / Support Team):** 

|Page|Content|
|---|---|
|**Organizations**|Organization list, license status,<br>endpoint counts, revenue (⚠blocked<br>pending billing data source — Section<br>28)|
|**Support Queue**|Engagement-scoped customer telemetry<br>view (⚠scope not confrmed — see<br>Section 3)|
|**Onboarding Review**|Application approval queue — Section 23|



**Login:** email and password only, no role selector — the backend resolves organization, role, and permissions from the JWT and the Desktop Application loads the corresponding view automatically. A role is never user-selectable. 

### **Data Polling Intervals** 

|Data|Polling Interval|
|---|---|
|Alerts|5 seconds|
|Incidents|30 seconds|
|Endpoints|60 seconds|
|Threat Hunting|On demand|
|Reports|On demand|



Real-time WebSockets and Server-Sent Events are deferred to a future release. Note: these intervals were originally reasoned about in a browsertab context. Revisit background-polling behavior when the Desktop Application is minimized — an Electron app’s resource profile while backgrounded differs from a browser tab a user might simply close. 

### **Application Data Flow** 

26 

### **Frontend State Requirements** 

Every page must implement: - **Loading State** — skeleton loaders (not spinner-only) - **Empty State** — e.g., “No Data Available” - **Error State** — e.g., “Unable to Load Data” with retry option - **Security** — no stack traces, internal errors, or sensitive configuration data exposed. The Desktop Application’s embedded web content runs with contextIsolation: true, nodeIntegration: false, and sandbox: true — mandatory, not optional, for an Electron application handling security data. 

## **17. IOC Management Flow** 

### **Supported IOC Types** 

· · · IP Address Domain URL File Hash 

### **IOC Upload Flow** 

Security Administrator opens Settings →Organization →IOC Management ↓ Uploads IOC dataset via: ├──JSON file upload └──CSV file upload ↓ Backend validates each entry: 

- Reject malformed entries 

- Reject unsupported IOC types 

- Deduplicate identical IOCs 

↓ Valid IOCs written to PostgreSQL (with expiration date) ↓ IOC Matching Engine begins matching incoming events against updated dataset 

### **IOC Lifecycle** 

- ⚠ Default Expiration: 180 days _(confirm against PRD source)_ 

- Expired IOCs are ignored automatically 

- Security Administrators receive notifications: 

   - **30 days before expiration** (approaching warning) 

   - **On expiration** (expired notification) 

Automatic threat intelligence feeds (MISP, STIX/TAXII, Abuse.ch, AlienVault OTX) are deferred to a future release. 

27 

## **18. User & Organization Management Flow** 

### **Organization Creation (Owner only)** 

Owner accesses Settings →Platform ↓ Creates new Organization ↓ Assigns first Security Administrator to the Organization ↓ Security Administrator can now manage users and resources within that org 

### **User Creation Flow** 

Security Administrator (or Owner) opens Users page ↓ Creates new user account (no public self-registration exists) ↓ Assigns role: Security Administrator | Security Analyst ↓ User account created in Active state ↓ User receives credentials via email (SMTP is a required see Section 23's revision note) 

### **User Lifecycle** 

|Action|Who Can Perform|
|---|---|
|Create user|Security Administrator (own org); Owner (any org)|
|Deactivate user|Security Administrator (own org); Owner (any org)|
|Reactivate user|Security Administrator (own org); Owner (any org)|
|Delete user|Not a required operation; deactivation is standard|



#### **On Deactivation:** 

User account deactivated ↓ User cannot authenticate ↓ Account data and audit history retained ↓ Role assignment preserved (for potential reactivation) 

### **User States** 

28 

|State|Description|
|---|---|
|Active|User can authenticate and access<br>Sentinel|
|Deactivated|User cannot authenticate; data and audit|
||history retained|
|Reactivated|Deactivated user restored to Active by|
||authorized administrator|



## **19. Policy Management Flow** 

### **Purpose** 

Allow administrators to control which telemetry categories agents collect, on a perorganization basis. 

### **Policy Update Flow** 

Security Administrator updates collection policy (Settings →Organization →Policies) ↓ New policy version saved to PostgreSQL ↓ Agent sends heartbeat to backend ↓ Backend includes updated policyVersion in heartbeat response ↓ 

Agent detects version mismatch ↓ Agent fetches full policy ↓ 

Agent fetches full policy document from policy endpoint 

Policy Engine applies new toggles: 

   - Collect Process Events: ON/OFF 

   - Collect Network Events: ON/OFF 

   - Collect Registry Events: ON/OFF 

   - Collect File Events: ON/OFF 

- ↓ 

- Agent reports new policyVersion in subsequent heartbeats 

## **20. Failure Handling & Degradation** 

### **Failure Behavior Table** 

29 

|Component|Failure Behavior|
|---|---|
|Agent Ofine<br>API Failure|Events bufered in SQLite; upload<br>resumes after connectivity returns<br>Agent retries uploads every 30 seconds;<br>events remain bufered in SQLite|
|Redis Failure|Events retried until the queue recovers;<br>unacknowledged messages remain<br>pending|
|PostgreSQL Failure|Events remain queued in Redis;<br>processing resumes after recovery. ⚠<br>Since PostgreSQL is Supabase-hosted,<br>“recovery” now depends on Supabase’s<br>uptime/incident response, not<br>on-premises restart control — factor<br>Supabase’s status/SLA into any incident<br>runbook.|
|Neo4j Failure|Alert processing continues �; incident<br>processing continues �; graph features<br>disabled �|
|Keycloak Failure|Existing sessions remain valid �; new<br>authentication requests fail �|



### **Graceful Degradation Example** 

Neo4j becomes unavailable ↓ Detection Engine continues processing � Incident Correlation Engine continues � Alerts visible on dashboard � Incidents visible on dashboard � Threat Hunting continues � Attack Graph page →"Unable to Load Data" � 

## **21. Platform Administration Flow (Owner)** 

### **Owner Responsibilities** 

##### First Deployment 

- ├──Keycloak bootstrap auto-provisions realm, roles, clients 

- ├──Bootstrap Owner account created automatically 

- ├──Owner creates first Organization 

- └──Owner assigns first Security Administrator 

##### Ongoing Operations 

- ├──License Management (Settings →Platform →Licensing) 

30 

├──Organization Management (create/delete orgs, assign admins) ├──Global Platform Configuration (certificates, system settings) 

├──Monitoring operational platform health 

└──System Recovery (including break-glass Owner recovery via Keycloak) 

### **Log Retention Configuration (Owner/Admin)** 

Default: 90 days. Configurable tiers: 

· · · 30 days 90 days 180 days 365 days 

Applies to: Authentication Events · Authorization Events · Administrative Actions · · · · · Alert Generation Incident Creation System Errors Report Generation Report Downloads 

### **Telemetry Retention** 

⚠ Default: 90 days _(confirm against PRD source)_ . Configurable: 30 / 60 / 90 / 365 days. 

### **Backup & Recovery** 

|Item|Details|
|---|---|
|PostgreSQL Backup|Supabase-managed backups<br>(tier-dependent) + independent pg_dump<br>run externally against the Supabase<br>connection string, daily, 30-day retention<br>— see Section 21|
|Neo4j Backup|neo4j-admin database dump, daily,<br>30-day retention (self-hosted,<br>unafected)|
|Storage|Neo4j: Docker-mounted backup volume.<br>PostgreSQL: pg_dump output stored<br>wherever your ops team keeps backups.|
|Validation|Backups validated; restoration tested on<br>documented recurring schedule|
|Recovery Scope|Users, endpoints, events, alerts,<br>incidents, policies, IOCs, graph<br>relationships|
|Recovery Time Objective|24 hours|



### **Deployment Safety** 

All deployments support **rollback to the previous stable version** without requiring full platform restoration. 

31 

## **22. Performance Boundaries** 

### **Endpoint Agent** 

|Metric|Target|
|---|---|
|CPU Usage|< 3% during normal operation|
|RAM Usage|< 200 MB during normal operation|
|Startup Time|< 10 seconds|
|Maximum Local Bufer|1 GB|



### **Platform Latency** 

|Metric|Target|
|---|---|
|High-Severity Alert Visibility|< 5 seconds end-to-end (event<br>occurrence →dashboard)|
|Standard Telemetry Visibility|< 10 seconds end-to-end|
|Dashboard Page Load|< 3 seconds|
|Threat Hunting Query|< 4 seconds (normal workloads)|
|Incident Load Time|< 3 seconds|
|Attack Graph Load Time|< 5 seconds|
|Investigation Workfow (Alert →Root|≤10 minutes|
|Cause)||
|Report Generation|< 30 seconds (normal deployment sizes)|



### **Scalability** 

- Validated: **100 concurrent endpoints** without architectural redesign 

- Future scaling: horizontal API scaling, PostgreSQL optimization, Redis scaling, Neo4j scaling 

### **Rate Limiting** 

|API Class|Limit|Scope|
|---|---|---|
|Authentication APIs|5 requests/minute|Per user/IP|
|Standard APIs|100 requests/minute|Per authenticated user|
|Administrative APIs|50 requests/minute|Owner, Security<br>Administrator|



## **23. Manual Onboarding & Public Website Flow** 

No organization can self-register. Every customer follows: 

32 

Prospective customer visits Public Website →submits Company Application (Apply page) ↓ Application stored (status: PENDING) ↓ Support Team reviews (status: IN_REVIEW) ↓ Human discussion — sales/technical call (status: IN_DISCUSSION) ↓ 

Owner gives final approval (status: APPROVED) 

↓ ├──Rejected at any stage →applicant notified, no organization created └──Approved →Customer Organization created ↓ License allocated (Section 24) ↓ First CSO account provisioned, credentials issued via email — required, not conditional 

(username, temporary password/activation link, SentinelConsole.msi download link) 

Public Website pages: **Apply** (application form), **Privacy Policy** , **Terms & Conditions** , **Contact email** , **Download** (Section 23a). No signup path exists anywhere on the site — the only routes to an active account are this full status chain, or the invitation flow (Section 23b) for accounts below the first CSO. **Revision note:** there is no “Login (redirects to authenticated dashboard)” page anymore — that only made sense when the dashboard was itself a web page at a URL. Login happens exclusively inside the Sentinel Desktop Application (Section 16b); there is nothing on the public website to redirect a login attempt to. 

### **23a. Two-Installer Strategy** 

Sentinel ships as two separate installers, not one bundled package: 

SentinelAgent.msi 

SentinelConsole.msi 

→Windows Service only →Sentinel Desktop Application only →installed on protected endpoints →installed on Security Analyst / →no user interface Security Administrator / CSO workstations →no dependency on the Desktop App →no telemetry collection, no endpoint registration, no heartbeat participation 

The set of machines an organization wants monitored is not the set its security team works from. Bundling would force every analyst’s own workstation to become a monitored endpoint, or force every monitored endpoint to carry a dashboard nobody there uses. Both installers are built, versioned, and upgraded independently. 

### **23b. User Invitation Flow** 

Beyond the Owner-approval-triggered creation of the first CSO (above), every subsequent account is created by self-service invitation, not manual administratorpopulated account setup: 

33 

- CSO, inside Desktop Application →Users →Invite Security Administrator ↓ 

- POST /users/invite { email, role: "security-administrator" } ↓ 

- Authorization check: can the caller's role invite the requested role? (CSO →security-administrator: yes. CSO →security-analyst: no.) ↓ 

Keycloak account provisioned, scoped to caller's organization 

↓ 

Postgres users row inserted (Keycloak provisioning alone is not sufficient — every other org-scoped query joins against this table) 

↓ 

Invitee emailed: username, temporary password/activation link, SentinelConsole.msi link ↓ 

Invitee installs SentinelConsole.msi, signs in 

The same pattern applies one level down: a Security Administrator invites Security Analysts, never another Security Administrator. Neither role can invite a peer or superior — enforced at the API layer against the caller’s own role, not a client-side restriction. If the invitation email fails to deliver, the inviting user (already authenticated) is shown the temporary password directly as a fallback, the same pattern used for the CSO’s own onboarding email above. **This supersedes** any earlier description of the Users page as a manual CRUD form with no email step. 

## **24. Licensing Flow** 

↓ 

Agent attempts registration (Section 5) 

↓ 

├──YES →registration proceeds normally 

└──NO →registration rejected; Security Administrator notified (cap reached, or license 

⚠ **Renewal and Suspension enforcement behavior are not yet specified** — specifically, whether a suspended organization’s already-registered endpoints stop being accepted for telemetry ingestion, or only new registrations are blocked, and whether the dashboard becomes read-only. This determines real runtime behavior and should be confirmed before the status field’s transitions are wired into enforcement logic. 

34 

## **25. SOAR Response Flow** 

Scope: **Kill Process** , **Endpoint Isolation** , **Create Incident** — discrete, humanapproval-gated actions only. No playbooks, no automated workflows, no autonomous execution. 

↓ 

↓ Approver determined by the organization's approval policy: ├──Default →Security Administrator reviews the request └──Executive-approval mode →CSO reviews the request 

↓ ├──Denied →action closed, no dispatch, logged with reason └──Approved →dispatched to the target agent via the agent command polling vs. persistent connection — not yet decided, see Section 26) 

↓ Agent executes the action locally ↓ Result reported back to backend →action status: EXECUTED or FAILED ↓ 

**Constraints:** an Analyst cannot approve their own request, regardless of which role is the approver. Approval routing (Security Administrator vs. CSO) is enforced at the API layer based on the organization’s stored approval policy, not only in the UI. ⚠ Who sets an organization’s approval policy (Owner at onboarding, or CSO self-service) is not yet specified. 

## **26. Agent Command Channel (Architectural Note)** 

The Sentinel Agent (Section 5) is upload-only in its base telemetry design: it initiates all communication with the backend, and the backend never initiates contact with the agent. SOAR execution (Section 25) requires the reverse — the backend must be able to reach a specific agent to dispatch an approved action. 

⚠ **Not yet decided:** - **Command polling** — agent checks for pending commands on its existing 60-second heartbeat cycle. Lower risk, up to 60 seconds of dispatch latency. - **Persistent connection** — agent maintains an open channel for nearimmediate delivery. Lower latency, new listening/connection surface on the endpoint not present in the base telemetry design. 

Whichever mechanism is chosen, command authenticity must be validated with the same rigor as the existing certificate-pinning model (Section 5), and Endpoint Isolation must have a tested recovery path so that isolating an endpoint does not also cut off the ability to de-isolate it remotely. 

35 

## **27. Deception Technology Flow** 

#### Scope: **Honey Files** and **Canary Tokens** only. 

↓ 

Policy Engine pushes the asset definition to selected endpoint(s) — reuses the existing policy sync mechanism (Section 6/19) ↓ 

Agent places the decoy file / registers the token locally ↓ Sysmon file-access telemetry detects access to the decoy path — no new collector required ↓ 

No new alert pipeline and no new telemetry source are introduced — deception reuses the existing policy and detection plumbing. 

## **28. Owner Analytics Flow** 

Owner opens Sentinel Company / Organizations view ↓ Backend aggregates: organization count, active ↓ 

## **Appendix A — Technology Stack** 

|Layer|Technology|
|---|---|
|Endpoint Agent|C# / .NET 8 / Windows Service / SQLite|
|Backend API|C# / ASP.NET Core / Serilog / REST|
|Sentinel Desktop Application|Electron / React / TypeScript / Tailwind<br>CSS / Cytoscape.js, distributed as<br>SentinelConsole.msi|
|Public Website|React / TypeScript / Tailwind CSS,<br>unauthenticated only, distributed<br>independently|



36 

|Layer|Technology|
|---|---|
|Relational DB|PostgreSQL, hosted via Supabase<br>(managed cloud Postgres) — ⚠exception<br>to otherwise self-hosted model, see<br>Section 20|
|Graph DB|Neo4j Community Edition|
|Queue|Redis Streams|
|Identity|Keycloak|
|ML Engine|Python + scikit-learn Isolation Forest<br>(recommended)|
|Background Jobs|Hangfre on PostgreSQL or dedicated<br>Redis Streams consumer (recommended)|
|Deployment|Docker / Docker Compose|



## **Appendix B — Containerized Services (Docker Compose)** 

|Service|Role|
|---|---|
|sentinel-api|Event ingestion, detection processing,<br>dashboard APIs|
|postgres|Operational data storage|
|neo4j|Relationship and attack graph storage|
|redis|Event streaming|
|keycloak|Authentication and authorization|
|website|Public marketing site (static build or<br>lightweight container); unauthenticated<br>only|
|ml-worker _(recommended)_|ML Anomaly Engine scoring process|



Sentinel Agent runs as a Windows Service on endpoints — it is **not** containerized. 

## **— Appendix C Deferred Features** 

|Feature Area|Examples|Notes|
|---|---|---|
|Full Autonomous SOAR /<br>Playbooks|Fully automated<br>remediation, multi-step<br>automated workfows|Out of scope. Discrete<br>approval-gated actions<br>(Kill Process, Endpoint|
|||Isolation, Create Incident)|
|||are in scope — Section 25.|



37 

|Feature Area|Examples|Notes|
|---|---|---|
|Honey Credentials / AD|Active Directory decoy|Not confrmed in scope.|
|Decoys|accounts, decoy<br>credentials|Honey Files and Canary<br>Tokens are in scope —<br>Section 27.|
|Advanced Threat Hunting|Saved hunts, scheduled<br>hunts, advanced query<br>language, boolean|Deferred|
||operators||
|Advanced Risk Scoring /|XGBoost confdence|Deferred|
|UEBA|scoring, user risk scores,<br>insider threat scoring||
|Advanced Graph Analytics|Blast radius analysis, path<br>scoring, automated attack<br>chain discovery|Deferred|
|Cross-Platform Agents|Linux, macOS|Deferred|
|Automatic Agent Updates|Update checks, signed<br>packages, staged rollouts|Deferred|
|Automatic Threat Intel|MISP, STIX/TAXII,|Deferred|
|Feeds|Abuse.ch, AlienVault OTX||
|Automatic Certifcate<br>Rotation|Automated trust updates|Deferred|
|Scheduled Report|Periodic automated|Deferred|
|Generation|reports||
|PDF Export|Report export in PDF<br>format|Deferred|
|Real-Time Dashboard|WebSockets / Server-Sent<br>Events|Deferred|
|Zero Trust (dynamic trust<br>scoring)|Continuous device posture<br>scoring, dynamic access<br>decisions|⚠Not scheduled —<br>narrower<br>per-request-validation<br>interpretation is in scope,<br>see Section 26/PRD §24.7|
|Kernel Drivers|Custom kernel-level<br>telemetry|Not scheduled|



#### _End of Sentinel Application Flow Document_ 

38 

