# **Contents** 

|**Sentinel — Product Requirements Document (PRD)**|**2**|
|---|---|
|Section 1 — Purpose<br>. . . . . . . . . . . . . . . . . . . . . . . . . . .|. . .<br>3|
|Section 2 — Product Vision . . . . . . . . . . . . . . . . . . . . . . . .|. . .<br>3|
|Section 3 — Product Goals<br>. . . . . . . . . . . . . . . . . . . . . . . .|. . .<br>3|
|Section 4 — Product Scope . . . . . . . . . . . . . . . . . . . . . . . .|. . .<br>4|
|Section 5 — Out of Scope . . . . . . . . . . . . . . . . . . . . . . . . .|. . .<br>5|
|Section 6 — Target Users . . . . . . . . . . . . . . . . . . . . . . . . .|. . .<br>5|
|Section 7 — Success Criteria . . . . . . . . . . . . . . . . . . . . . . .|. . .<br>6|
|Section 8 — Sentinel Agent . . . . . . . . . . . . . . . . . . . . . . . .|. . .<br>7|
|Section 9 — Telemetry Collection<br>. . . . . . . . . . . . . . . . . . . .|. . .<br>9|
|Section 10 — Sentinel Unifed Event Schema (SUES) . . . . . . . . . .|. . .<br>10|
|Section 11 — Detection Engine . . . . . . . . . . . . . . . . . . . . . .|. . .<br>11|
|Section 12 — ML Anomaly Engine<br>. . . . . . . . . . . . . . . . . . . .|. . .<br>12|
|Section 13 — Incident Correlation . . . . . . . . . . . . . . . . . . . .|. . .<br>13|
|Section 14 — Attack Graphs<br>. . . . . . . . . . . . . . . . . . . . . . .|. . .<br>14|
|Section 15 — Threat Hunting . . . . . . . . . . . . . . . . . . . . . . .|. . .<br>16|
|Section 16 — Reporting . . . . . . . . . . . . . . . . . . . . . . . . . .|. . .<br>18|
|Section 17 — Authentication & RBAC<br>. . . . . . . . . . . . . . . . . .|. . .<br>20|
|Section 18 — Sentinel Desktop Application & Public Website<br>. . . . .|. . .<br>22|
|Section 19 — Non-Functional Requirements . . . . . . . . . . . . . . .|. . .<br>25|
|Section 20 — Operational Security & Reliability Requirements<br>. . . .|. . .<br>26|
|Section 21 — Deployment Model . . . . . . . . . . . . . . . . . . . . .|. . .<br>28|
|Section 22 — Deferred Features<br>. . . . . . . . . . . . . . . . . . . . .|. . .<br>29|
|Section 23 — Release Defnition<br>. . . . . . . . . . . . . . . . . . . . .|. . .<br>29|
|Section 24 — SOAR, Deception, Licensing & Platform Operations . . .|. . .<br>30|



1 

# **— Sentinel Product Requirements Document (PRD)** 

_(Includes Owner/Support Team, Customer Organization/CSO structure, SOAR, Deception Technology, Licensing, and Onboarding)_ 

**Note on this revision:** The original file was an OCR export of a scanned/exported document and contained extensive character-level recognition errors, broken bullet lists, mangled tables, and corrupted flowdiagram text. This version corrects those issues and restores standard Markdown structure (headings, lists, tables, and flow diagrams as fenced code blocks) while preserving the original content and section numbering (1–23). 

A small number of numeric values were not cleanly legible in the scanned source. These are marked ⚠ below and have been reconstructed on a best-effort basis, consistent with the values used in the companion Sentinel Technical Requirements Document and Application Flow Document. They should be treated as provisional until confirmed against the original source file. 

**Audit Pass 2 — Cross-Document Consistency Fixes:** A second audit checked the PRD against both the TRD (Revision 4) and the AFD directly (Revision 4’s audit only checked PRD→TRD). The following fixes are applied in this version: - Added hostname to the mandatory SUES field list (Section 10) — present in both TRD and AFD but omitted here. - Reconciled the “Supported Event Types” list in Section 10 with the literal eventType enum used by TRD/AFD, including ALERT and INCIDENT as event types. - Fixed an internal inconsistency in Section 16: the “Report Contents” subsection had silently dropped “Analyst Activity,” “Incident Severity,” and “Closure Details,” which are all listed earlier in the same section under “Report Types.” - Quantified Goal 3 (Section 3) with the explicit ≤10-minute investigation SLA. TRD and AFD both already state this figure and cite it as sourced from “PRD Goal 3” — but the PRD itself only said “within minutes rather than hours.” The PRD is now the authoritative source for that number instead of retroactively agreeing with a figure the TRD introduced. 

2 

## **Section 1 — Purpose** 

Sentinel is a self-hosted, AI-assisted Windows Endpoint Detection and Response (EDR) platform with investigation, approval-gated response, and deception capabilities. ⚠ “AI-assisted” refers specifically to the ML Anomaly Detection Engine (Isolation Forest, Section 12) — it does not imply LLM-based investigations, AI copilots, autonomous reasoning, natural-language threat hunting, or any other AI capability not otherwise specified in this document. Sentinel provides: 

- Endpoint telemetry collection 

- Threat detection 

- Incident correlation 

- Attack graph visualization 

- Threat hunting 

- Security reporting 

Sentinel focuses on visibility, detection, and investigation rather than automated response. 

## **Section 2 — Product Vision** 

Provide organizations with a modern security platform that enables security teams to: 

- Detect malicious activity 

- Investigate attacks quickly 

- Understand attack progression 

- Correlate related security events 

- Visualize attack paths 

- Hunt for threats proactively 

Sentinel aims to provide enterprise-grade visibility while remaining fully self-hosted and under customer control. 

## **— Section 3 Product Goals** 

**Goal 1 — Detect common Windows attack techniques in near real time.** Target: High-severity alerts visible in under 5 seconds, from event occurrence to dashboard visibility. 

**Goal 2 — Provide complete attack visibility.** Security analysts should be able to identify, through a single investigation workflow: 

- Initial Access 

- Execution 

- Persistence 

3 

- Lateral Movement 

- Credential Access 

**Goal 3 — Reduce investigation time.** Take analysts from Alert →Incident →Root Cause within minutes rather than hours. Target: root-cause identification within **≤10 minutes** of alert visibility, under normal deployment conditions (100 concurrent endpoints) and assuming relevant telemetry is present. 

**Goal 4 — Provide attack path visualization.** Analysts should understand attack progression through attack graphs, e.g.: 

User →Endpoint →Process →Domain/IP →Alert →Incident 

**Goal 5 — Support proactive threat hunting.** Analysts should be able to search historical telemetry across: 

- Users 

- Endpoints 

- Processes 

- Files 

- Domains 

- IP Addresses 

- Alerts 

- Incidents 

## **Section 4 — Product Scope** 

### **Included in Scope** 

**Endpoint Monitoring** - Windows Endpoint Telemetry Collection - ETW Integration - AMSI Integration - Sysmon Integration - Windows Event Log Integration 

**Threat Detection** - Behavioral Rules - Sigma Rules - IOC Matching - ML-Based Anomaly Detection - Alert Deduplication 

**Incident Management** - Alert Generation - Alert Grouping - Incident Creation - Incident Tracking - Investigation Workflow 

**Attack Graph Visualization** - Entity Relationships - Attack Path Visualization - Interactive Graph Exploration - Neo4j-Powered Attack Graphs 

**Threat Hunting** - Historical Search - Entity Pivoting - Investigation Workflows 

**Reporting** - Incident Reports - Security Reports - Executive Summaries - CSV Export - JSON Export 

**User Management** - Authentication - RBAC - User Lifecycle Management 

**Platform Administration** - Organization Management - License Management - Platform Configuration 

4 

## **— Section 5 Out of Scope** 

**Full Autonomous SOAR / Playbooks** — Fully automated, unsupervised remediation and multi-step automated workflows are out of scope. (Discrete, human-approvalgated actions — Kill Process, Endpoint Isolation, Create Incident — are in scope; see Section 24.3.) 

**Zero Trust — Dynamic Trust Scoring** — Continuous device posture scoring and dynamic access decisions are out of scope. (Per-request credential validation and strict no-implicit-cross-organization-access are in scope; see Section 24.7.) 

**UEBA** — Examples: User Risk Scores, Insider Threat Scoring. 

**Cross-Platform Agents** — Deferred: Linux, macOS. 

**Kernel Drivers** — Sentinel leverages Windows-native telemetry sources (ETW, Sysmon, AMSI, and Windows Event Logs) to obtain deep system visibility without developing custom kernel-mode drivers. No custom kernel drivers are developed. (See Section 24.8 for a clarification on informal “kernel telemetry” phrasing that has been used to describe these existing sources.) 

**Honey Credentials / Active Directory Decoys** — Not confirmed in scope. Only Honey Files and Canary Tokens are specified (Section 24.4). 

## **— Section 6 Target Users** 

User roles are organized into two tiers: **Sentinel Company** — a persisted platformoperator entity containing Owner and Support Team, with platform-wide permissions — and **Customer Organization** , each a licensed tenant. 

### **Tier 1 — Sentinel Company** 

#### **Owner** 

The Owner is the Sentinel platform owner. 

Responsibilities: Platform Management, Organization Management, Initial CSO Assignment, License Management, Platform Maintenance, System Recovery, Onboarding Approval, Cross-Organization Analytics. 

Permissions: Full Platform Access. 

The Owner has visibility across all organizations. The Owner is **not** responsible for day-to-day investigations. 

#### **Support Team** 

Sentinel-Company-tier role, distinct from Owner, with platform-wide (not perorganization) permissions scoped to the responsibilities below. 

5 

Responsibilities: review onboarding applications; onboard organizations (facilitate the Apply →Review →Human Discussion →Approval flow, Section 24.5); provide temporary, time-limited support access to a customer organization’s telemetry for troubleshooting. 

Explicit restrictions: **no** SOAR trigger or approval authority; **no** unrestricted crossorganization access — support access to a specific organization is time-limited to an active engagement, not standing. 

### **Tier 2 — Customer Organization** 

#### **CSO (Chief Security Officer)** 

Executive owner of the organization’s security posture. 

Responsibilities: organization ownership; executive oversight; **manage Security Administrators** (create/deactivate Security Administrator accounts within the organization); subscription/licensing point of contact; generate and review reports; approve destructive SOAR actions **when the organization is configured to require executive approval** (Section 24.3) — this is the exception path, not the default. 

Permissions: Organization-Level Executive Access. 

#### **Security Administrator** 

Responsibilities: - Day-to-day platform configuration: deploy Sentinel, **manage Security Analysts** (create/deactivate Analyst accounts), manage endpoints, manage policies, manage IOC datasets, manage organization configuration, deploy deception assets - Approve SOAR actions **by default** for organizations not configured to require executive (CSO) approval (Section 24.3) 

Permissions: Organization-Level Administrative Access. 

Security Administrators can only access resources within their organization. 

#### **Security Analyst** 

Responsibilities: - Monitor Alerts - Investigate Incidents - Use Threat Hunting - Generate Reports - Analyze Attack Graphs - Request SOAR Actions (cannot self-approve) Permissions: Investigation Access. 

Security Analysts cannot manage users, policies, or organization settings, and cannot approve their own SOAR requests. 

## **Section 7 — Success Criteria** 

**Detection** - At least 15 native Behavioral Rules operational. - At least 25–40 Sigma Rules operational. - IOC Matching operational. - ML Anomaly Detection operational. 

6 

**Endpoint Visibility** - Endpoint telemetry collected successfully. - High-severity alerts visible within 5 seconds. - Standard telemetry visible within 10 seconds. 

**Investigation** - Incident Correlation operational. - Attack Graphs operational. - Threat Hunting operational. 

**Performance** - Agent CPU usage: < 3% - Agent memory usage: < 200 MB - Dashboard load time: < 3 seconds 

**Scalability** - Sentinel shall support 100 concurrent endpoints without architectural redesign. 

## **— Section 8 Sentinel Agent** 

### **Purpose** 

The Sentinel Agent is installed on every protected Windows endpoint. The agent is responsible for: 

- Collecting telemetry 

- Normalizing telemetry into SUES 

- Buffering telemetry locally 

- Sending telemetry securely to Sentinel 

- Sending health heartbeats 

- Applying collection policies 

### **Platform** 

Supported Platform: Windows only. 

### **Installation** 

Installation Method: **SentinelAgent.msi** — a dedicated installer, distinct from the Desktop Application’s installer (Section 18a). Installed only on the endpoints being protected, never on analyst/administrator/CSO workstations unless that machine is itself a protected endpoint. See Section 24.11 for the full two-installer rationale. Supported Deployment Methods: Manual Installation, Group Policy (GPO), SCCM, Microsoft Intune. 

### **Execution Mode** 

The agent runs as a Windows Service. Startup Type: Automatic — the agent starts whenever the endpoint boots. 

### **Telemetry Collection Responsibilities** 

The agent shall collect: ETW Events, AMSI Events, Sysmon Events, Windows Event Logs. 

7 

### **Event Normalization** 

All collected telemetry shall be converted into SUES (Sentinel Unified Event Schema) before transmission. 

### **Local Event Buffer** 

The agent shall maintain a local event buffer. Purpose: survive network outages, backend failures, and temporary connectivity issues. 

### **Buffer Eviction Policy** 

Maximum Buffer Size: 1 GB. 

When utilization exceeds 10%, the agent shall: 1. Retain security-relevant telemetry. 2. Evict oldest low-priority telemetry first. 

Telemetry collection shall continue uninterrupted. 

### **Telemetry Priority Levels** 

Telemetry shall be categorized into three priority levels: 

**High Priority** — Examples: Detection Alerts, Authentication Events, AMSI Events, Process Creation Events, Security Events. 

**Medium Priority** — Examples: Network Events, Registry Events, File Events. 

**Low Priority** — Examples: Informational System Events, Diagnostic Events, Performance Events. 

When buffer utilization exceeds 10%, the agent shall evict telemetry in the following order: 

Low Priority →Medium Priority →High Priority 

High Priority telemetry shall be retained whenever possible. 

### **Delivery Retry Policy** 

Failed event deliveries shall be retried automatically. Retry Interval: 30 seconds. Retry Attempts: Unlimited, while buffer space remains available. Failures shall be logged locally. 

### **Secure Communication** 

Agent communication shall use HTTPS with TLS 1.3 and certificate pinning. 

8 

### **Certificate Validation Failure** 

If certificate pinning validation fails: - Telemetry transmission shall be blocked. - Events shall be logged locally. - Agent health shall be marked as Degraded. - Local telemetry buffering shall continue. 

This behavior protects against potential MITM attacks. 

### **Certificate Compromise Recovery** 

If a pinned certificate becomes compromised, recovery requires: - New certificate generation - Agent redeployment - Manual trust replacement 

Automatic certificate rotation is deferred to a future release. 

### **Health Monitoring** 

Agent health messages shall include: Endpoint ID, Agent Version, Policy Version, Status, Timestamp. 

### **Heartbeat Policy** 

Heartbeat Interval: 60 seconds. 

**Endpoint Status Rules:** - **Healthy** — Heartbeat received normally. - **Degraded** — Miss 3 consecutive heartbeats. - **Offline** — Miss 10 consecutive heartbeats. 

## **— Section 9 Telemetry Collection** 

### **Purpose** 

Provide endpoint visibility required for detection and investigation. 

### **Telemetry Sources** 

**ETW** — Collect: Process Activity, Network Activity, Security Events. 

**AMSI** — Collect: Script Content, Script Execution Events, PowerShell Activity. 

**Sysmon** — Collect: Process Creation, Network Connections, Registry Activity, File Activity. 

**Windows Event Logs** — Collect: Authentication Events, Security Events, System Events. 

### **Policy-Based Collection** 

Telemetry collection shall be configurable. Examples: Collect Process Events, Collect Network Events, Collect Registry Events, Collect File Events. 

9 

### **Telemetry Retention** 

⚠ Default Retention: 90 Days _(scanned source was ambiguous; reconstructed consistent with the TRD/AFD)_ . Configurable Options: 30 Days, 60 Days, 90 Days, 365 Days. 

## **— Section 10 Sentinel Unified Event Schema (SUES)** 

### **Purpose** 

Provide a common internal schema for all telemetry. All telemetry must be normalized into SUES before: Storage, Detection, Threat Hunting, Graph Processing. 

### **Core Fields** 

Every SUES event shall contain: 

|Field|Description|
|---|---|
|eventId|Unique Event ID|
|schemaVersion|Schema Version|
|eventType|Event Type|
|source|Telemetry Source|
|timestamp|Event Timestamp|
|organizationId|Organization Identifer|
|endpointId|Endpoint Identifer|
|hostname|Hostname|
|userId|Associated User|
|severity|Event Severity|



### **Valid Severity Values** 

Informational, Low, Medium, High, Critical. 

### **Schema Versioning** 

Every SUES event shall declare its schema version, e.g.: 

**{** "schemaVersion" **:** "1.0" **}** 

Future schema versions shall maintain backward compatibility whenever possible. 

### **Supported Event Types** 

- **Process Event** — Examples: Process Created, Process Terminated 

- **Network Event** — Examples: Connection Opened, Connection Closed 

- **Authentication Event** — Examples: Login Success, Login Failure 

10 

- **Registry Event** — Examples: Registry Created, Registry Modified, Registry Deleted 

- **File Event** — Examples: File Created, File Accessed, File Modified, File Deleted, File Loaded 

- **Script Event** — Examples: PowerShell, VBScript, JavaScript 

- **Alert Event** — A generated alert represented as a SUES event, carrying alertId and the originating rule/severity data. 

- **Incident Event** — A generated or updated incident represented as a SUES event, carrying incidentId. 

The canonical eventType enum (used identically by the TRD and AFD) is: PROCESS_START, PROCESS_STOP, NETWORK_CONNECTION, DNS_QUERY, FILE_ACCESS, FILE_MODIFICATION, REGISTRY_MODIFICATION, POWERSHELL_EXECUTION, ALERT, INCIDENT. The category names above are the product-facing grouping of these values; engineering implementation must use the literal enum values, not the category labels. 

### **Design Objectives** 

SUES shall provide: Consistent Event Structure, Vendor-Neutral Processing, Simplified Detection Logic, Simplified Threat Hunting, Simplified Graph Generation. 

### **Data Consumers** 

SUES events shall be consumed by: Detection Engine, ML Anomaly Engine, Incident Correlation Engine, Attack Graph Engine, Threat Hunting Engine, Reporting Engine. 

## **Section 11 — Detection Engine** 

### **Purpose** 

Identify suspicious or malicious activity. 

### **Architecture** 

Detection Engine ├──Behavioral Rules ├──Sigma Rules ├──IOC Matching └──ML Anomaly Engine 

### **Behavioral Rules** 

Native Sentinel detections. Examples: Office Spawning PowerShell, Encoded PowerShell, Certutil Abuse, Rundll32 Abuse, MSHTA Abuse, Credential Dumping. Target: 15 native rules. 

11 

### **Sigma Rules** 

Community-driven detections. Target: 25–40 Sigma rules. 

Sigma Processing: 

Sigma Rule →SUES Mapping →Detection Logic →Execution 

### **IOC Matching** 

Supported Indicator Types: IP Address, Domain, URL, File Hash. IOC Sources: Manual Upload — Supported Formats: CSV, JSON. 

### **IOC Expiration** 

⚠ Default Expiration: 180 Days _(scanned source was ambiguous; reconstructed consistent with the TRD/AFD)_ . Expired indicators shall be ignored automatically. 

### **IOC Expiration Notifications** 

Security Administrators shall receive notifications when: - IOC expiration is approaching - IOC expiration has occurred 

Default Warning Period: 30 days before expiration. 

### **Alert Deduplication** 

Duplicate alerts shall be suppressed. Criteria: Same Rule, Same Endpoint, Same User, Within 60 Seconds. 

Instead of generating multiple alerts, the occurrence counter shall be updated. The occurrence count shall be visible to analysts within the Alert Details page. 

## **Section 12 — ML Anomaly Engine** 

### **Purpose** 

Identify suspicious activity not detected by traditional rules. 

### **Role** 

ML acts as: a standalone Low/Medium detector, and a secondary confidence signal. 

### **Detection Model** 

Initial Model: Isolation Forest. 

12 

### **Inputs** 

Examples: Process Frequency, Parent Process Frequency, Execution Time, Network Activity, Domain Diversity, Login Activity. 

### **Baseline Requirements** 

New endpoints enter a learning period. Default Learning Period: 7 days. 

During learning: anomaly scores are generated; informational alerts only are produced. 

### **Learning Period Alert Behavior** 

Informational alerts: - Are visible in the Alerts page - Do not participate in incident correlation - Do not affect incident severity - Exist solely for model visibility and tuning 

### **Endpoint Reimaging Handling** 

If an endpoint is reimaged: - A new Endpoint ID shall be generated - Previous ML baselines shall not be reused - A new learning period shall begin automatically 

### **Alert Generation** 

ML may independently generate: Low Alerts, Medium Alerts. ML shall **not** independently generate: High Alerts, Critical Alerts. 

High and Critical alerts require confirmation from: Behavioral Rules, Sigma Rules, or IOC Matching. 

### **Output** 

ML produces an Anomaly Score, used for: Alert Prioritization, Confidence Enhancement, Unknown Threat Detection. 

## **— Section 13 Incident Correlation** 

### **Purpose** 

Group related alerts into investigations. 

### **Correlation Factors** 

Alerts may be grouped using: Endpoint, User, Process, Time Window, Shared Indicators. 

13 

### **Correlation Window** 

Default: 15 Minutes. 

### **Post-Window Handling** 

Alerts arriving after the correlation window create a new incident. 

### **Incident Alert Capacity** 

An incident may contain an unlimited number of alerts. To maintain usability, 100 alerts per page shall be used for pagination on the Incident Details page. 

### **Incident Merge Rules** 

- **Severity** — Highest severity wins. 

- **Status** — Most active status wins. Priority order: Investigating →Open →Resolved →Closed. 

- **Assignee** — Primary incident assignee is retained. 

### **Incident Assignment** 

Incidents shall be unassigned by default. Security Analysts may self-assign incidents. Security Administrators may assign incidents to analysts within their organization. Only one primary assignee may exist at a time. 

All alerts, notes, evidence, entities, and timelines shall be preserved. 

### **Incident Statuses** 

Open, Investigating, Resolved, Closed. 

### **Incident Severities** 

Low, Medium, High, Critical. 

### **Escalation Logic** 

Credential Access or Defense Evasion alerts escalate to High severity, minimum. Multiple High alerts may escalate an incident to Critical. 

## **Section 14 — Attack Graphs** 

### **Purpose** 

Provide visual understanding of attack progression. Attack Graphs help analysts understand how an attack moved through users, endpoints, processes, files, domains, 

14 

IP addresses, alerts, and incidents. 

### **Technology** 

Neo4j Community Edition. 

### **Supported Nodes** 

User, Endpoint, Process, File, Domain, IP Address, Alert, Incident. 

### **Supported Relationships** 

|Relationship|Pattern|Example|
|---|---|---|
|User Authentication|User —LOGGED_INTO→<br>Endpoint|Alice —LOGGED_INTO→<br>PC-001|
|Process Execution|Endpoint —EXECUTED→|PC-001 —EXECUTED→|
||Process|powershell.exe|
|Process Spawning|Process —CREATED→|WINWORD.EXE —CREATED→|
||Process|powershell.exe|
|File Access|Process —ACCESSED→File|powershell.exe<br>—ACCESSED→salary.xlsx|
|File Creation|Process —CREATED→File|powershell.exe<br>—CREATED→payload.exe|
|File Modifcation|Process —MODIFIED→File|ransomware.exe<br>—MODIFIED→|
|||document.docx|
|File Deletion|Process —DELETED→File|malware.exe —DELETED→<br>backup.zip|
|DLL / File Loading|Process —LOADED→File|rundll32.exe —LOADED→<br>malicious.dll|
|Network Communication|Process —CONNECTED_TO→<br>Domain|powershell.exe<br>—CONNECTED_TO→evil.com|
|DNS Resolution|Domain —RESOLVES_TO→IP<br>Address|evil.com —RESOLVES_TO→<br>185.66.22.11|
|Alert Attribution|Alert —TRIGGERED_BY→<br>Entity (Process, File,<br>Domain, or IP Address)|Encoded PowerShell<br>Alert —TRIGGERED_BY→<br>powershell.exe|
|Endpoint Attribution|Alert —AFFECTS→<br>Endpoint|Credential Dumping<br>Alert —AFFECTS→PC-001|
|Incident Membership|Alert —PART_OF→<br>Incident|PowerShell Alert<br>—PART_OF→Incident-123|
|User Attribution|Incident —INVOLVES→<br>User|Incident-123 —INVOLVES→<br>Alice|



Additional Alert Attribution examples: Known Malware Alert —TRIGGERED_BY→payload.exe; Malicious Domain Alert —TRIGGERED_BY→evil.com; Threat Intelligence 

15 

Alert —TRIGGERED_BY→185.66.22.11. 

### **Capabilities** 

**Relationship Visualization** — Display relationships between: Users, Endpoints, Processes, Files, Domains, IP Addresses, Alerts, Incidents. 

**Investigation Support** — Allow analysts to: expand nodes, explore relationships, inspect entity metadata, trace attack paths, pivot between entities. 

**Incident Context** — Provide complete attack-chain visibility. Example: 

### **Example Scenarios** 

#### **Malware Propagation Scenario** 

WINWORD.EXE —CREATED→powershell.exe —CREATED→payload.exe —CONNECTED_TO→evil.com — RESOLVES_TO→185.66.22.11 

#### **Ransomware Scenario** 

User →Endpoint →ransomware.exe —MODIFIED→500 Files →Alert →Incident 

#### **Credential Theft Scenario** 

### **Investigation Objectives** 

Attack Graphs shall enable analysts to answer: - How did the attack start? - Which user was involved? - Which endpoint was affected? - Which processes executed? - Which files were created, modified, accessed, loaded, or deleted? - Which domains and IP addresses were contacted? - Which alerts contributed to the incident? - What was the complete attack path? 

### **Design Objectives** 

Attack Graphs shall provide: Visual Attack Reconstruction, Entity Relationship Exploration, Investigation Acceleration, Threat Context Enrichment, Root Cause Analysis Support. 

## **— Section 15 Threat Hunting** 

### **Purpose** 

Enable analysts to proactively search historical telemetry for suspicious activity. Threat Hunting allows analysts to investigate Known Threats, Emerging Threats, Suspicious Behavior, and Incident-Related Activity without waiting for alerts. 

16 

### **Supported Search Targets** 

- **Users** — Examples: Alice, Bob, Administrator 

- **Endpoints** — Examples: PC-001, LAPTOP-123, SERVER-01 

- **Processes** — Examples: powershell.exe, cmd.exe, rundll32.exe 

- **Files** — Examples: invoice.docx, payload.exe, malicious.dll 

- **Domains** — Examples: google.com, evil.com 

- **IP Addresses** — Examples: 8.8.8.8, 185.66.22.11 

- **Alerts** — Search by: Alert Name, Severity, Detection Source, MITRE Technique 

- **Incidents** — Search by: Incident ID, Status, Severity, Assignee 

### **Threat Hunting Capabilities** 

**Historical Search** — Search historical telemetry across configured retention periods. 

**Entity Pivoting** — Analysts shall be able to pivot between related entities without manual searching, e.g.: 

User →Endpoint →Process →File →Domain/IP →Alert →Incident 

**Filtered Search** — Support: Time Range, Endpoint, User, Process, File, Domain, IP Address, Severity. 

**Search Results** — Shall support: Sorting, Filtering, Pagination, Entity Pivoting. 

### **Threat Hunting Results Table** 

|Column|Description|
|---|---|
|Timestamp<br>Entity Type|Event Time<br>User, Endpoint, Process, File, Domain, IP,<br>Alert, Incident|
|Entity Name|Entity Identifer|
|Endpoint|Associated Endpoint|
|User|Associated User|
|Severity|Event Severity|
|Related Alert Count|Linked Alerts|



### **Investigation Workflows** 

#### **Workflow 1 — Alert-Based Investigation** 

Alert →Endpoint →Process →File →Domain/IP →Incident 

#### **Workflow 2 — User-Based Investigation** 

User →Endpoints →Processes →Alerts →Incidents 

#### **Workflow 3 — IOC-Based Investigation** 

17 

#### **Workflow 4 — File Investigation** 

File →Creating Process →Parent Process →Endpoint →User →Related Alerts 

### **Search Performance Requirements** 

Normal hunting queries shall return results within 4 seconds under supported deployment scale. 

### **Access Control** 

- **Owner** — May search across all organizations. 

- **Security Administrator** — May search within their organization. 

- **Security Analyst** — May search within their organization. 

### **Deferred Features** 

Deferred to a future release: Saved Hunts, Scheduled Hunts, Advanced Query Language, Boolean Operators, Query Templates. 

### **Design Objectives** 

Threat Hunting shall provide: Fast Investigation Workflows, Historical Visibility, Efficient Entity Pivoting, Minimal Navigation Depth, Context-Rich Investigations. 

## **— Section 16 Reporting** 

### **Purpose** 

Provide security reporting capabilities for analysts, administrators, management, and platform owners. Reporting shall enable: Incident Documentation, Security Operations Visibility, Trend Analysis, Compliance Support, Executive Communication. 

### **Report Types** 

**Incident Report** Purpose: Provide detailed documentation of a security incident. Contains: Incident Summary, Incident Severity, Timeline, Related Alerts, Related Entities, Investigation Notes, Resolution Actions, Closure Details. 

**Security Operations Report** Purpose: Provide operational visibility into security activities. Contains: Alert Statistics, Incident Statistics, Detection Statistics, Endpoint Statistics, Analyst Activity, Trend Analysis. 

**Executive Summary Report** Purpose: Provide high-level security posture visibility. Contains: Security Overview, Critical Incidents, Incident Trends, Detection Trends, Risk Overview, Key Metrics. 

18 

### **Supported Export Formats** 

Supported: CSV, JSON. Deferred: PDF Export. 

### **Report Filters** 

Reports shall support filtering by: Time Range, Severity, Endpoint, User, Incident Status, Detection Source. 

### **Report Generation** 

Sentinel supports Manual Report Generation — analysts may generate reports on demand. Deferred: Scheduled Report Generation. 

### **Report Access Control** 

**Owner** — Access: All Reports, All Organizations. May generate Incident Reports, Security Operations Reports, and Executive Summary Reports across all organizations. 

**CSO** — Access: All Reports within their organization. May generate Incident Reports, Security Operations Reports, and Executive Summary Reports for their organization only. 

**Security Administrator** — Access: All Reports within their organization. May generate Incident Reports, Security Operations Reports, and Executive Summary Reports for their organization only. 

**Security Analyst** — Access: Investigation and Security Reports within their organization. May generate Incident Reports and Security Operations Reports for their organization only (excluding Executive Summary). 

### **Multi-Tenant Data Isolation** 

Report access restrictions shall be enforced at: Authentication Layer, Authorization Layer, API Layer, Database Query Layer. User interface restrictions alone shall not be relied upon. Organization data shall remain isolated according to assigned role permissions. 

### **Report Contents** 

**Incident Report Structure** — Incident Information, Incident Severity, Timeline, Related Alerts, Affected Entities, Investigation Notes, Resolution Details, Closure Details. 

**Security Operations Report Structure** — Reporting Period, Alert Statistics, Incident Statistics, Detection Statistics, Endpoint Statistics, Analyst Activity, Trend Analysis. 

**Executive Summary Structure** — Reporting Period, Security Overview, Critical Incidents, Risk Trends, Key Metrics. 

19 

### **Performance Requirements** 

Reports shall be generated within 30 seconds for normal deployment sizes. Large reports may be generated asynchronously. 

### **Audit Logging** 

The following report actions shall be logged: Report Generation, Report Export, Report Download. Log fields: User, Role, Timestamp, Report Type, Export Format. 

### **Design Objectives** 

Reporting shall provide: Actionable Security Insights, Investigation Documentation, Management Visibility, Export Flexibility, Organization-Level Data Isolation. 

## **Section 17 — Authentication & RBAC** 

### **Purpose** 

Provide secure authentication and authorization. 

### **Authentication Platform** 

Keycloak. 

### **Registration Model** 

Sentinel shall not provide Public Registration or Public Signup. All users are created by authorized administrators. 

### **Login Flow** 

User →Login Page →Keycloak Authentication →JWT Access Token →Dashboard Access 

### **Roles** 

**Owner** — Platform-Level Role (Sentinel Company). The Owner exists outside organizational boundaries and is implemented as a platform-level Keycloak role. Responsibilities: Create Organizations, Delete Organizations, Assign First CSO, License Management, Onboarding Approval, System Maintenance, Platform Recovery, Global Configuration, Cross-Organization Analytics. Access: All Organizations, All Users, All Endpoints, All Reports. 

**Support Team** — Platform-Level Role (Sentinel Company), platform-wide permissions. Responsibilities: Review Onboarding Applications, Onboard Organizations, Temporary Time-Limited Support Access to Customer Telemetry. Cannot: Approve 

20 

or Trigger SOAR Actions, Access Organization Data Outside an Active, Time-Boxed Engagement. 

**CSO (Chief Security Officer)** — Organization-Level Executive Role (Customer Organization), above Security Administrator. Responsibilities: Organization Ownership, Executive Oversight, Manage Security Administrators, Licensing/Subscription Point of Contact, Review Reports, Full Visibility Within Own Organization, Approve SOAR Actions **only when the organization is configured for executive-approval mode** (default is Security Administrator — see Section 24.3). Access: Organization Resources Only (own organization). 

**Security Administrator** — Organization-Level Role. Responsibilities: Day-to-Day Platform Configuration — Deploy Sentinel, Manage Security Analysts, Manage Policies, Manage IOC Datasets, Manage Organization Settings, Deploy Deception Assets — and Approve SOAR Actions **by default** . Access: Organization Resources Only. 

**Security Analyst** — Investigation-Level Role. Responsibilities: Monitor Alerts, Investigate Incidents, Use Threat Hunting, Generate Reports, Analyze Attack Graphs, Request SOAR Actions (cannot self-approve, regardless of which role approves). Cannot: Create Users, Modify Policies, Change Organization Settings, Approve Own SOAR Requests. 

### **Session Management** 

Access Token: 15 minutes. Refresh Token: 7 days. Logout invalidates the active session. 

### **Account Lockout** 

⚠ Failed Login Threshold: 5 attempts _(scanned source was ambiguous; reconstructed consistent with the TRD/AFD)_ . Lockout Duration: 15 minutes. Implemented through: Keycloak. 

### **Owner Recovery Procedure** 

Sentinel shall maintain a break-glass recovery procedure. If the Owner account becomes locked, recovery shall occur through direct Keycloak administrative access. Security Administrators shall not have authority to recover Owner accounts. 

### **Password Policy** 

Minimum Length: 12 characters. Requirements: Uppercase Letter, Lowercase Letter, Number, Special Character. Managed through Keycloak password policies. 

### **Password Reset** 

Password reset functionality requires SMTP configuration. Reset links shall be delivered through email. SMTP configuration is mandatory for production deployments. 

21 

### **Multi-Tenant Access Control** 

CSO, Security Administrators, and Security Analysts shall only access data belonging to their organization. The Owner role shall bypass organization boundaries. Support Team access is scoped to a specific organization only for the duration of an active support engagement (⚠ exact enforcement mechanism not confirmed — see Section 24.1) and shall not otherwise bypass organization boundaries. Access control enforcement shall occur at: Authentication Layer, Authorization Layer, API Layer, Database Query Layer. 

## **— Section 18 Sentinel Desktop Application & Public Website** 

**Revision note:** earlier drafts of this section described a browser-based “React Dashboard” as the authenticated interface. That is superseded. The authenticated interface is now the **Sentinel Desktop Application** (Electron + React, installed via SentinelConsole.msi) — a native desktop app, not a web page. The public website is unauthenticated and carries no dashboard functionality of any kind. These are two separate artifacts with two separate installers/deployments (Section 24.11), not one web app split into public and private routes. 

### **Purpose** 

Provide centralized visibility and investigation capabilities to Customer Organization users, and a public-facing presence for prospective customers, entirely separate from that authenticated surface. 

### **Section 18a — Public Website (Unauthenticated)** 

Pages: **Home** , **Features** , **Pricing** , **About** , **Contact** , **Apply** (company application submission), **Privacy Policy** , **Terms & Conditions** , **Download** (installer downloads — see below). No signup, no login, no dashboard content of any kind. The website’s only dynamic behavior is the Apply form submitting to the public application-intake API (Section 24.5). 

**Download Page** — offers two distinct, clearly labeled downloads so nobody installs the wrong artifact on the wrong machine: - **Sentinel Desktop Console (SentinelConsole.msi)** — for Security Analysts, Security Administrators, and CSOs. This is how every Customer Organization user accesses Sentinel. - **Sentinel Agent (SentinelAgent.msi)** — for endpoints being protected. Runs as a background Windows Service; has no user interface. 

There is no “Login” page on the public website — login only happens inside the Desktop Application (Section 18b). A website that redirected to a browser-based dashboard made sense when the dashboard was itself a web page; it is not, so there is nothing on the web to redirect to. 

22 

### **Section 18b — Sentinel Desktop Application (Authenticated)** 

**Primary Navigation — Customer Organization view:** Overview, Alerts, Incidents, Threat Hunting, Attack Graph, Endpoints, SOAR, Deception, Reports, Users, Settings. **Primary Navigation — Sentinel Company view (Owner/Support Team):** Organizations, Support Queue, Onboarding Review, plus cross-organization Analytics (Owner only). Whether the Sentinel Company view ships inside the same Desktop Application binary or as a separate internal tool is an open implementation question, not resolved by this document — nothing here should be read as requiring Owner/Support Team to install the same customer-facing SentinelConsole.msi. 

**Login.** Users enter email and password only — there is no role selector anywhere in the login flow. The backend determines organization, role, and permissions from the validated JWT and the Desktop Application loads the corresponding view automatically. A role is never user-selectable, in this or any other screen. 

### **Overview Dashboard** 

Displays: - **Endpoint Metrics** — Total Endpoints, Healthy Endpoints, Degraded Endpoints, Offline Endpoints. - **Alert Metrics** — Active Alerts, Critical Alerts, High Alerts. - **Incident Metrics** — Open Incidents, Investigating Incidents, Critical Incidents. - **Recent Activity** — Recent Alerts, Recent Incidents. 

### **Alerts Page** 

Capabilities: Search, Filtering, Sorting, Pagination. Columns: Severity, Alert Name, Endpoint, User, Detection Source, MITRE Technique, Timestamp, Status. 

### **Incident Page** 

Displays: Incident Summary, Timeline, Evidence, Notes, Related Alerts, Related Entities, Attack Graph. 

### **Attack Graph Page** 

Powered by: Neo4j + Cytoscape.js, rendered inside the Desktop Application (embedded web content), not a separate browser tab. Capabilities: Node Expansion, Relationship Exploration, Entity Inspection, Attack Path Visualization. 

### **Threat Hunting Page** 

Displays: Search Interface, Filters, Results Table, Pivot Links. 

### **Endpoints Page** 

Displays: Endpoint Status, Agent Version, Policy Version, Last Heartbeat. 

23 

### **Users Page** 

Displays: User Accounts, Assigned Roles, Account Status. Visible to Owner (all organizations), CSO and Security Administrator (own organization). 

**User creation model:** self-service invitation, not manual CRUD-only account setup. A CSO invites Security Administrators; a Security Administrator invites Security Analysts (Section 24.10). Each invitation provisions a Keycloak account and sends an email containing an activation link and temporary password. The Users page is where these invitations are initiated and where account status is subsequently monitored — it is not a form for an administrator to manually populate every field of a new account with no email involved. 

### **SOAR Page** 

Displays: Action Request Form (Security Analyst), Approval Queue (CSO/Security Administrator) with full request context (requester, target, justification), action status and audit history. 

### **Deception Page** 

Displays: Honey File and Canary Token deployment (Security Administrator), trigger history. 

### **Sentinel Company Pages (Owner / Support Team)** 

**Organizations** — organization list, license status, endpoint counts, revenue (⚠ blocked pending a billing data source — see Section 24.9). **Support Queue** — engagement-scoped customer telemetry view (⚠ scope not confirmed — see Section 24.1). **Onboarding Review** — application queue, approve/reject. 

### **Settings Page** 

Displays: Policy Management, IOC Management, Organization Settings, Platform Configuration. 

### **Settings Access Control** 

**Platform Configuration** — Access: Owner Only. Examples: Licensing, Global Platform Settings, Certificate Management, System Configuration. 

**Organization Settings** — Access: Security Administrator. Examples: User Management, IOC Management, Policy Management, Organization Configuration. _(Unchanged: CSO does not manage policies directly — CSO manages organization configuration and Security Administrator accounts; Security Administrator manages policies, IOC, and Security Analyst accounts. An earlier architecture draft briefly suggested CSO policy management; that draft was not adopted.)_ 

24 

### **Frontend Requirements** 

All views shall support: - **Loading State** — Use skeleton loaders instead of spinneronly loading indicators. - **Empty State** — Example: “No Data Available.” - **Error State** — Example: “Unable To Load Data,” with retry capability. 

### **Security Requirements** 

The Desktop Application shall not expose: Stack Traces, Internal Errors, Sensitive Configuration Data. Additionally, the Desktop Application’s embedded web content shall run with context isolation enabled and no direct Node.js/OS integration exposed to rendered content — the same security posture required of any application embedding a web rendering engine, not optional hardening. 

## **— Section 19 Non-Functional Requirements** 

### **Purpose** 

Define performance, scalability, reliability, maintainability, and usability requirements. 

### **Performance Requirements** 

**Detection Latency** — High-severity alerts shall be visible in the Desktop Application within 5 seconds of event occurrence. Standard telemetry shall be visible within 10 seconds of event occurrence. 

**Dashboard Performance** — Page Load Time: under 3 seconds. Threat Hunting Search Response: under 4 seconds for normal search workloads. 

**Agent Resource Usage** — CPU Usage: under 3% during normal operation. Memory Usage: under 200 MB during normal operation. Maximum Local Buffer Size: 1 GB. 

### **Scalability Requirements** 

Sentinel shall support 100 concurrent endpoints without architectural redesign. Future scaling shall be supported through: Horizontal API Scaling, PostgreSQL Optimization, Redis Scaling, Neo4j Scaling. 

### **Reliability Requirements** 

Sentinel shall continue operating during: Temporary Network Failures, Endpoint Connectivity Issues, Redis Restarts, Neo4j Failures — where possible. 

Graceful degradation shall be implemented. Example: Neo4j Failure →Attack Graphs Unavailable →Detection Continues. 

25 

## **— Section 20 Operational Security & Reliability Requirements** 

### **Purpose** 

Define production security and operational requirements. 

### **Input Validation** 

All API requests shall validate: Required Fields, Data Types, Length Constraints, Format Constraints. Invalid requests shall be rejected. 

### **Input Sanitization** 

User-generated content shall be sanitized. Protection required against: Cross-Site Scripting (XSS), HTML Injection, Script Injection. Applicable fields include: Notes, Comments, Organization Names, User Content. 

### **Cross-Origin Resource Sharing (CORS)** 

Production deployments shall use an approved origin allowlist. Wildcard origins shall not be used. 

### **Rate Limiting** 

|API Class|Limit|Scope|
|---|---|---|
|Authentication APIs|5 requests/minute|Per user/IP|
|Standard APIs|100 requests/minute|Per authenticated user|
|Administrative APIs|50 requests/minute|Owner, Security<br>Administrator|



### **Session Security** 

Access Token: 15 minutes. Refresh Token: 7 days. Sessions shall be invalidated upon logout. 

### **Password Reset Security** 

Requirements: Single-Use Tokens, Secure Random Tokens, Expiration Enforcement. ⚠ Default Expiration: 15 minutes _(scanned source was ambiguous; reconstructed consistent with the TRD/AFD)_ . 

### **Logging Requirements** 

Sentinel shall log: Authentication Events, Authorization Events, Alert Generation, Incident Creation, Administrative Actions, System Errors. 

26 

**Log Retention** Default Retention: 90 days. Configurable Options: 30 days, 90 days, 180 days, 365 days. 

Applicable Logs: Authentication Events, Authorization Events, Administrative Actions, Alert Generation, Incident Creation, System Errors, Report Generation, Report Downloads. 

### **Monitoring** 

Monitor: API Latency, Error Rates, Agent Heartbeats, Database Health, Resource Utilization. 

### **Operational Alerting** 

Generate operational alerts for: Backend Failures, Database Failures, Agent Communication Failures, Detection Engine Failures. 

### **Secrets Management** 

Sensitive credentials shall never be stored in source code. Examples: Database Credentials, API Keys, JWT Secrets, Service Tokens. Secrets shall be managed using environment variables or equivalent secure configuration. 

### **Dependency Security** 

Dependencies shall be scanned regularly for known vulnerabilities. Security updates shall be applied according to risk severity. 

### **Backup & Recovery** 

Backups shall be performed regularly. Requirements: Backup Validation, Restoration Testing, Documented Recovery Procedures. 

### **Deployment Safety** 

Deployments shall support rollback to the previous stable version without requiring full platform restoration. 

### **Certificate Strategy** 

Sentinel shall use TLS 1.3 for all communications. Agent certificate pinning shall be implemented. 

Certificate Strategy: Self-Signed Certificate, Embedded During Agent Installation, 10-Year Validity. Deferred: Automatic Certificate Rotation. 

27 

## **Section 21 — Deployment Model** 

### **Deployment Type** 

Sentinel is Self-Hosted, with one exception: PostgreSQL is hosted via Supabase (managed cloud Postgres) rather than run in the customer’s own Docker environment. ⚠ Every other component (backend API, Keycloak, Neo4j, Redis, the agent) remains fully self-hosted. This exception should be reflected in any customer-facing “selfhosted” claim — it is a hybrid model, not a pure self-hosted one, until/unless Supabase is itself self-hosted in the future. 

### **High-Level Architecture** 

- Windows Endpoints 

   - →Sentinel Agents →HTTPS 

   - →ASP.NET Core API 

   - →Redis Streams 

   - →Detection Engine 

   - →PostgreSQL / Neo4j 

   - →Sentinel Desktop Application (Electron + React) 

### **Backend Components** 

**ASP.NET Core API** — Responsibilities: Authentication, Event Ingestion, Detection Processing, Desktop Application APIs. 

**PostgreSQL** — Stores: Telemetry, Alerts, Incidents, Users, Configuration. ⚠ Hosted via Supabase (managed cloud Postgres) rather than self-hosted in Docker. This is a deliberate exception to the self-hosted deployment model (Section 21) — confirm this is acceptable given data-residency/compliance commitments made to customers before treating it as final. 

**Redis Streams** — Responsibilities: Event Pipeline, Event Buffering, Detection Queue. 

**Neo4j** — Stores: Entity Relationships, Attack Graph Data. 

**Keycloak** — Provides: Authentication, Identity Management, RBAC Integration. 

### **Agent Deployment** 

SentinelAgent.msi — Supported Methods: Manual Installation, Group Policy (GPO), SCCM, Microsoft Intune. 

### **Desktop Console Deployment** 

SentinelConsole.msi — downloaded from the public website (Section 18a) or distributed via the same GPO/SCCM/Intune channels, installed on Security Analyst, Se- 

28 

{] Behavioral Oo ML 

0 Endpoint [] Dashboard 

O] Incident 0] Threat 

{] Sigma 

[] IOC f] Alert 

] Telemetry [] Heartbeat 

{] Attack 

- [ Authentication Operational - [] RBAC Operational (5-role model: [] User Operational - [] Reporting Operational - [] Licensing Enforcement Operational - [] Manual Onboarding Workflow Operational - [] Public Website (Apply/Login/Privacy/Terms) 0 SOAR 

[] Deception 

] Detection [] Dashboard ] Agent (] Security ) Monitoring Q] Alerting [] Logging (] Backup [] Rollback [] Certificate [] PRD [] TRD [|] AFD [] UI/UX ] Backend 1 Implementation 

zation and sits above the organization tenant boundary rather than being an implicit role with no data model of its own. 

### **24.2 — Licensing** 

Licensing is endpoint-based. Lifecycle: 

Each Customer Organization has a maximum endpoint count (license cap) set at onboarding and adjustable by the Owner. Endpoint registration beyond the cap is rejected. ⚠ **Renewal trigger and cadence, and the exact enforcement behavior of Suspension** (e.g., whether a suspended organization’s endpoints stop being accepted for telemetry, or the dashboard goes read-only) are not yet specified — these determine real operational behavior and should be confirmed before implementation, not inferred. 

### **24.3 — SOAR Response Engine** 

In scope: **Kill Process** , **Endpoint Isolation** , **Create Incident** as discrete, analysttriggered actions. 

- A Security Analyst may **request** a SOAR action. 

- **Approval authority is organization-configurable:** by default, a **Security Administrator** approves. An organization may instead be configured to require **CSO** (executive) approval — this is the exception path, not the default. ⚠ Who sets this configuration (Owner at onboarding, or CSO self-service) is not specified. 

- An Analyst can never self-approve their own request, regardless of which role approves. 

- Approved actions are dispatched to the target endpoint and executed by the agent. 

- Every request, approval/denial, and execution outcome shall be logged with requester, approver, and timestamps for each state. 

- Full playbooks, automated workflows, and fully autonomous response remain **out of scope** — only these three discrete, human-gated actions are in scope. 

⚠ **Engineering note carried from architecture review:** the agent communicates upload-only (agent →backend). Executing a backend-approved action on an endpoint requires the backend to reach the agent, which is a new capability, not an extension of an existing one. **Endpoint Isolation cannot be considered “in scope and deliverable” independent of this new channel existing.** 

### **24.4 — Deception Technology** 

In scope: **Honey Files** and **Canary Tokens** , deployable per Customer Organization by a Security Administrator. 

Security Administrator deploys decoy files/tokens to selected endpoints via the existing Policy Engine; access to a decoy triggers a Critical-severity alert through the 

31 

existing Detection/Alert pipeline (no new alert pipeline is introduced). Honey Credentials, Active Directory decoys, and network-based canaries are **not** confirmed in scope and are not included here. 

### **24.5 — Manual Onboarding Workflow** 

No organization can self-register. Every customer follows: 

A prospective customer submits a Company Application via the public website (Section 24.6, status: PENDING). Support Team reviews it (status: IN_REVIEW). A human discussion (e.g., a sales/technical call) follows (status: IN_DISCUSSION). Owner gives final approval (status: APPROVED), at which point the Customer Organization is created, its first CSO account is provisioned, a license is assigned per Section 24.2, and the CSO is emailed a username, temporary password/activation link, and the SentinelConsole.msi download link. **Revision note:** email delivery here is no longer conditional on SMTP configuration — it is a required production dependency, since the CSO has no other path to receiving their credentials or the installer link. Earlier drafts of this document phrased this as “via email if SMTP configured”; that conditional framing is retired. 

### **24.6 — Public Website** 

In scope, explicitly: **Apply** (company application submission), **Privacy Policy** , **Terms & Conditions** , a **Contact email** , and **Download** (installer downloads, Section 24.11). **No signup** — there is no self-service account creation path anywhere on the public site; the only path to an active account is Section 24.5’s onboarding workflow, or Section 24.10’s invitation flow for accounts below the first CSO. **Revision note:** there is no “Login” page on the public website. Earlier drafts described one that redirected to the authenticated dashboard — that only made sense when the dashboard was itself a browser page at a URL. The authenticated interface is now the Sentinel Desktop Application (Section 18b), a separate installed artifact; there is nothing on the public website to redirect a login attempt to. 

### **24.7 — Zero Trust Principles** 

Narrow interpretation: every request (agent and user) is validated per-request against current credentials/certificates rather than relying on session-level trust; no implicit data access exists between Customer Organizations regardless of role. **Not included:** continuous device posture scoring, network micro-segmentation, or dynamic trust scoring — those remain listed as out of scope in Section 5 under “Zero Trust — Dynamic Trust Scoring.” 

### **24.8 — Telemetry Sources (“Kernel Telemetry” Clarification)** 

Sentinel leverages Windows-native telemetry sources (ETW, Sysmon, AMSI, and Windows Event Logs) to obtain deep system visibility without developing custom kernelmode drivers. No kernel-mode driver is added. Where “kernel telemetry” has been 

32 

used informally to describe these sources, that usage should not be read as implying a kernel-mode driver — none of the four sources require one. 

### **24.9 — Owner Analytics** 

In scope: organization count, active endpoint count across all organizations, and **revenue** , calculated from active endpoint licenses and stored billing records. ⚠ The specific pricing/rate model (e.g., flat per-endpoint rate, tiered pricing) and the structure of the billing records themselves are not yet specified — the data source is now defined, but the formula is not. 

### **24.10 — User Invitation Flow (New)** 

Beyond the Owner-approval-triggered creation of the first CSO (Section 24.5), every subsequent account is created by **self-service invitation** , not administratorpopulated manual account setup: - A CSO invites Security Administrators for their organization. - A Security Administrator invites Security Analysts for their organization. - Neither role can invite a peer or a superior — a CSO cannot create another CSO or transfer organization ownership; a Security Administrator cannot create another Security Administrator. 

Each invitation provisions a Keycloak account scoped to the inviting user’s organization and role-appropriate permissions, and sends the invitee an email containing a username, temporary password/activation link, and the SentinelConsole.msi download link. This is new scope relative to earlier drafts of this document, which described a Users page as a manual CRUD form with no email step — that model is superseded by the invitation flow described here. If email delivery fails, the inviting user (already authenticated, already trusted) is shown the temporary password directly as a fallback manual-relay path, the same fallback pattern used for the initial CSO’s onboarding email. 

### **24.11 — Two-Installer Strategy (New)** 

Sentinel ships as **two separate installers** , not one bundled package: - **SentinelAgent.msi** — Windows Service only. Installed on every endpoint being protected. No user interface. Has no dependency on, and does not install, the Desktop Application. - **SentinelConsole.msi** — Sentinel Desktop Application only. Installed on Security Analyst, Security Administrator, and CSO workstations. Has no telemetry-collection or endpoint-monitoring function of any kind. 

These are deliberately separate because they serve different install populations: the set of machines an organization wants _monitored_ (potentially every workstation and server in the fleet) is not the same set of machines its _security team_ uses to do their jobs (a handful of analyst/admin/CSO workstations). A single bundled installer would force every analyst’s own workstation to become a monitored endpoint whether wanted or not, or force every monitored endpoint to carry a security dashboard nobody there needs. Do not reintroduce a single combined installer without revisiting this rationale explicitly. 

33 

