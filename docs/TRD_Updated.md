# **Contents** 

|**Sentinel Technical Requirements Document (TRD)**<br>**2**|
|---|
|1. Document Control<br>. . . . . . . . . . . . . . . . . . . . . . . . . . . . . .<br>2|
|2. Purpose . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . .<br>4|
|3. System Overview . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . .<br>4|
|4. Architecture Overview . . . . . . . . . . . . . . . . . . . . . . . . . . . .<br>5|
|5. Architectural Principles<br>. . . . . . . . . . . . . . . . . . . . . . . . . . .<br>6|
|6. Technology Stack . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . .<br>6|
|7. Sentinel Agent Architecture . . . . . . . . . . . . . . . . . . . . . . . . .<br>7|
|8. Agent Installation & Deployment<br>. . . . . . . . . . . . . . . . . . . . . .<br>8|
|9. Sentinel Unifed Event Schema (SUES) . . . . . . . . . . . . . . . . . . .<br>9|
|10. Telemetry Collection & Retention<br>. . . . . . . . . . . . . . . . . . . . .<br>11|
|11. Event Processing Pipeline<br>. . . . . . . . . . . . . . . . . . . . . . . . .<br>11|
|12. Detection Engine<br>. . . . . . . . . . . . . . . . . . . . . . . . . . . . . .<br>12|
|13. Sigma Engine . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . .<br>13|
|14. ML Anomaly Detection Engine . . . . . . . . . . . . . . . . . . . . . . .<br>14|
|15. Threat Intelligence Engine (IOC Matching) . . . . . . . . . . . . . . . .<br>15|
|16. Incident Correlation Engine<br>. . . . . . . . . . . . . . . . . . . . . . . .<br>16|
|17. Threat Hunting Engine . . . . . . . . . . . . . . . . . . . . . . . . . . .<br>18|
|18. PostgreSQL Architecture . . . . . . . . . . . . . . . . . . . . . . . . . .<br>19|
|19. Neo4j Architecture (Attack Graph Storage) . . . . . . . . . . . . . . . .<br>20|
|20. Redis Architecture<br>. . . . . . . . . . . . . . . . . . . . . . . . . . . . .<br>21|
|21. Reporting Engine . . . . . . . . . . . . . . . . . . . . . . . . . . . . . .<br>22|
|22. Authentication, Identity & RBAC Architecture<br>. . . . . . . . . . . . . .<br>23|
|23. Multi-Tenant (Organization) Isolation Architecture . . . . . . . . . . . .<br>27|
|24. Sentinel Desktop Application & Public Website Architecture . . . . . . .<br>28|
|25. Security Architecture & Trust Model<br>. . . . . . . . . . . . . . . . . . .<br>29|
|26. Operational Security Requirements<br>. . . . . . . . . . . . . . . . . . . .<br>30|
|27. Backup & Recovery . . . . . . . . . . . . . . . . . . . . . . . . . . . . .<br>32|
|28. Performance Requirements . . . . . . . . . . . . . . . . . . . . . . . . .<br>33|
|29. Deployment Topology . . . . . . . . . . . . . . . . . . . . . . . . . . . .<br>34|
|30. Failure Handling<br>. . . . . . . . . . . . . . . . . . . . . . . . . . . . . .<br>34|
|31. Upgrade Strategy . . . . . . . . . . . . . . . . . . . . . . . . . . . . . .<br>35|
|32. Deferred Features (Out of Scope)<br>. . . . . . . . . . . . . . . . . . . . .<br>35|
|33. Technical Success Criteria . . . . . . . . . . . . . . . . . . . . . . . . .<br>36|
|34. SOAR, Deception, Licensing & Platform Operations Architecture . . . .<br>37<br>End of TRD . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . .<br>42|



1 

# **Sentinel Technical Requirements Document (TRD)** 

## **1. Document Control** 

|Field|Value|
|---|---|
|Document<br>Revision|Sentinel Technical Requirements Document (TRD)<br>5 (Desktop Application Architecture Update)|
|Date|2026-07-12|
|Status|Draft for Engineering Review|
|Source PRD|Sentinel PRD|
|Supersedes|TRD Revision 4|



### **1.1 Summary of Changes from Revision 4** 

This revision reflects a confirmed architecture change: the authenticated interface is no longer a browser-based React dashboard — it is the **Sentinel Desktop Application** (Electron + React), distributed as SentinelConsole.msi, separate from SentinelAgent.msi (the Windows Service). Changes: 

- **Section 3/4/6 component and tech-stack lists** updated: “Web Dashboard (React)” replaced with the Desktop Application (Electron + React) and a separatelylisted, unauthenticated Public Website. 

- **Section 8 installation method** split into two named installers; single “MSI Package” language removed. 

- **Section 24 renamed and restructured** (“Dashboard Architecture” →“Sentinel Desktop Application & Public Website Architecture”), split into 24a (public, unauthenticated) and 24b (Desktop Application, authenticated). Electron security requirements (contextIsolation, nodeIntegration: false, sandbox) added — not present in any prior revision, because there was no Electron shell to secure before now. 

- **Section 29 containerized services list** — dashboard removed as a container; the Desktop Application is an installed native artifact, not a hosted web service. 

2 

- **Section 34.4** — the public site’s “Login (redirect)” page is removed; there is nothing on the web for a login attempt to redirect to anymore. SMTP-conditional language (“if configured”) is retired — email delivery is now a required production dependency for onboarding to function. 

- **Section 34.8 (new) — Two-Installer Strategy.** Documents why SentinelAgent.msi and SentinelConsole.msi are separate, not bundled. 

- **Section 34.9 (new) — User Invitation Flow.** Documents the self-service CSO→Security Administrator→Security Analyst invitation flow, which supersedes an earlier, less specified “Users page is a manual CRUD form” model. 

- **Confirmed unchanged:** CSO does not manage policies — Security Administrator does. An earlier architecture draft briefly suggested otherwise; that draft was not adopted, and no RBAC matrix in this document required correction. 

- **Section heading updated from “Revision 4” to “Revision 5” throughout.** 

### **1.1a Prior: Summary of Changes from Revision 3 (Historical)** 

This revision resolves all remaining gaps and conflicts identified in the Revision 3 PRD→TRD audit. Changes are: 

- **Post-correlation-window conflict resolved.** The Revision 3 carry-forward exception (allowing late alerts to join an existing incident on strong indicator match) is removed. The PRD is unambiguous: alerts arriving after the correlation window create a new incident. A product decision note is added in place of the exception, with a path for future exception via PRD amendment. 

- **Incident Correlation Criteria corrected.** PRD §13 lists five grouping factors: Endpoint, User, Process, Time Window, and Shared Indicators. Revision 3 omitted Process and Shared Indicators, and incorrectly listed “Detection Category” as a criterion. All five PRD factors are now present. 

- **Analyst Activity added to Security Operations Report.** PRD §16 includes “Analyst Activity” as a required field; it was absent from the Revision 3 report spec. 

- **Password reset token expiration added to Open Items (⚠).** The 15-minute value is correct per PRD but the OCR source was ambiguous; the ⚠ flag is now applied consistently. 

- **Log retention configurable tiers clarified.** 90 days is now explicitly listed as a configurable option in addition to being the default, resolving the ambiguity from Revision 3. 

- **Investigation workflow SLA added (PRD Goal 3).** A testable end-to-end target of ≤10 minutes from alert generation to root-cause identification via the investigation workflow is added to the Performance Requirements section. 

- **Section heading updated from “Revision 3” to “Revision 4” throughout.** 

### **1.2 Open Items Requiring Source Confirmation** 

These values were not cleanly legible in the OCR’d PRD source. Best-effort values are used below and marked ⚠. Confirm against the original PRD before treating as committed requirements: 

- **IOC default expiration period** (Section 15) — best-effort: 180 days. 

3 

- **Telemetry retention default and configurable tiers** (Section 10) — besteffort: default 90 days; options 30 / 60 / 90 / 365 days. 

- **Failed login attempt threshold** (Section 22) — best-effort: 5 attempts (PRD OCR renders ambiguously). 

- **Password reset token expiration** (Section 22) — best-effort: 15 minutes (PRD OCR was ambiguous at this point; value is consistent with session-security context but requires source confirmation). 

## **2. Purpose** 

This document defines the technical architecture, infrastructure, services, data flow, security controls, operational requirements, and implementation requirements for Sentinel. 

The TRD serves as the primary engineering reference for development and deployment, and translates the PRD into a complete technical design. Every section traces to a PRD requirement. 

## **3. System Overview** 

Sentinel is a self-hosted, AI-assisted Windows Endpoint Detection and Response (EDR) platform with investigation, approval-gated response, and deception capabilities. ⚠ “AI-assisted” refers specifically to the ML Anomaly Detection Engine (Isolation Forest, Section 14) — it does not imply LLM-based investigations, AI copilots, autonomous reasoning, natural-language threat hunting, or any other AI capability not otherwise specified in this document. It provides: 

- Endpoint visibility 

- Threat detection (rule-based, signature-based, and anomaly-based) 

- Incident correlation 

- Threat hunting 

- Attack graph visualization 

- Security reporting 

The platform consists of: 

- Windows Endpoint Agent 

- Event Normalization Layer (SUES) 

- Event Streaming Layer (Redis Streams) 

- Detection Engine (Behavioral Rules, Sigma Engine, IOC Matching) 

- ML Anomaly Detection Engine 

- Incident Correlation Engine 

- Threat Hunting Engine 

- Graph Engine (Neo4j) 

- Reporting Engine 

4 

- Backend API (ASP.NET Core) 

- Sentinel Desktop Application (Electron + React) — the authenticated interface 

- Public Website (React, unauthenticated — marketing, Apply, Download only) 

Sentinel supports Windows endpoints only. It leverages Windows-native telemetry sources (ETW, Sysmon, AMSI, and Windows Event Logs) to obtain deep system visibility without developing custom kernel-mode drivers. 

## **4. Architecture Overview** 

### **High-Level Data Flow** 

Windows Endpoint ↓ Sentinel Agent (Telemetry Collector →Policy Engine →Normalization Engine →Local Buffer) ↓HTTPS (TLS 1.3 + Certificate Pinning) 

ASP.NET Core API 

↓ Redis Streams (sentinel-events) ↓ Detection Pipeline (Behavioral Rules + Sigma Engine + IOC Matching + ML Anomaly Engine) ↓ PostgreSQL — Supabase-hosted (telemetry, alerts, incidents) + Neo4j — selfhosted (entity relationships) ↓ 

Incident Correlation Engine →Threat Hunting Engine →Reporting Engine ↓ 

Desktop Application API ↓ Sentinel Desktop Application (Electron + React) 

### **Primary Components** 

**Endpoint Layer** — telemetry collection, policy-based collection control, event normalization (SUES), offline buffering, secure transmission. 

**Processing Layer** — event streaming, detection execution (behavioral rules, Sigma, IOC, ML), incident correlation, graph relationship extraction. 

**Storage Layer** — event storage, alert storage, incident storage, relationship storage, report artifacts. 

**Presentation Layer** — alert monitoring, incident investigation, threat hunting, attack graph visualization, reporting. 

5 

## **5. Architectural Principles** 

**Principle 1 — Normalize Before Processing.** No component consumes raw telemetry. All telemetry must first be transformed into the Sentinel Unified Event Schema (SUES). 

**Principle 2 — Decouple Ingestion From Detection.** Event ingestion is independent from detection execution. Redis Streams provide queue separation. 

**Principle 3 — Store Relationships Separately.** PostgreSQL (Supabase-hosted) stores telemetry and incidents. Neo4j (self-hosted) stores entity relationships. Raw telemetry is never stored in Neo4j. 

**Principle 4 — Security By Default.** All communications use TLS 1.3, certificate pinning, JWT authentication, and role-based access control. 

**Principle 5 — Graceful Degradation.** Failure of non-critical services shall not stop the platform. Example: if Neo4j becomes unavailable, alert and incident processing continue; only the graph view becomes unavailable. 

**Principle 6 — Detection Confidence Tiering.** ML-based detection may independently raise only Low/Medium severity alerts. High/Critical severity requires confirmation from a deterministic detection source (Behavioral Rules, Sigma Rules, or IOC Matching). 

## **6. Technology Stack** 

**Endpoint** — Language: C#. Framework: .NET 8. Application Type: Windows Service. Local Storage: SQLite. Telemetry Sources: ETW, AMSI, Sysmon, Windows Event Logs. 

**Backend** — Language: C#. Framework: ASP.NET Core. Logging: Serilog. API Style: REST. 

**Sentinel Desktop Application** — Shell: Electron. UI Framework: React. Language: TypeScript. Styling: Tailwind CSS. Graph Visualization: Cytoscape.js (rendered inside the Electron app’s embedded web content, not a browser tab). Auth: systembrowser + PKCE + loopback redirect (RFC 8252) — never an embedded-webview login form. Distributed as SentinelConsole.msi. 

**Public Website** — Framework: React. Language: TypeScript. Styling: Tailwind CSS. Unauthenticated only — no dashboard routes, no auth code, no Cytoscape.js. Deployed independently of the Desktop Application, with its own release cycle. 

**Data Layer** — Relational: PostgreSQL, hosted via Supabase (managed cloud Postgres). ⚠ This is an exception to the otherwise self-hosted deployment model (Section 29) — confirm acceptable given data-residency/compliance commitments before treating as final. Graph: Neo4j Community Edition (self-hosted). 

**Queue Layer** — Redis Streams. 

6 

#### — **Identity Layer** Keycloak. 

**ML Layer** — Recommended: a dedicated scoring worker (Python + scikit-learn Isolation Forest) consuming SUES events from Redis Streams, persisting per-endpoint baselines and anomaly scores to PostgreSQL. The PRD does not mandate an implementation language for this component; this is a TRD-level recommendation based on library maturity for Isolation Forest, not a hard requirement. 

**Background Jobs (recommended)** — A scheduled/async job mechanism (e.g., Hangfire on PostgreSQL, or a dedicated Redis Streams consumer) for asynchronous report generation when reports exceed normal size thresholds. 

**Deployment** — Docker, Docker Compose. 

## **7. Sentinel Agent Architecture** 

### **Responsibilities** 

The Sentinel Agent is responsible for: telemetry collection, policy-based collection control, event normalization, offline buffering, secure communication, and health monitoring. 

### **Module Architecture** 

**Telemetry Collector** — collects ETW events, AMSI events, Sysmon events, Windows Event Logs. 

**Policy Engine** — receives collection policy from the backend and enables/disables telemetry categories accordingly. Supported toggles: Collect Process Events, Collect Network Events, Collect Registry Events, Collect File Events. Recommended sync mechanism: the agent’s heartbeat response carries the current policyVersion; if it differs from the agent’s locally cached version, the agent fetches the full policy document from a dedicated policy endpoint. The active policy version is reported in every heartbeat. 

**Normalization Engine** — converts raw telemetry into SUES events. All downstream components consume only SUES events. 

**Local Buffer** — Technology: SQLite. Stores pending events, agent metadata, agent configuration, last synchronization state. Purpose: offline operation and networkfailure recovery. 

- Maximum Buffer Size: 1 GB. 

- **Telemetry Priority Levels:** 

   - **High Priority** — Detection Alerts, Authentication Events, AMSI Events, Process Creation Events, Security Events. 

   - **Medium Priority** — Network Events, Registry Events, File Events. 

   - **Low Priority** — Informational System Events, Diagnostic Events, Performance Events. 

7 

- **Buffer Eviction Policy** — when buffer utilization exceeds 10%, the agent shall: (1) retain security-relevant (High Priority) telemetry, and (2) evict telemetry in order Low Priority →Medium Priority →High Priority. High Priority telemetry is retained whenever possible. Telemetry collection continues uninterrupted during eviction. 

**Communication Module** — responsibilities: HTTPS communication, certificate validation, event upload, heartbeats, retry logic. 

- **Delivery Retry Policy** — failed event deliveries are retried automatically. Retry Interval: 30 seconds. Retry Attempts: unlimited, while local buffer space remains available. Failures are logged locally. 

- **Certificate Validation Failure Behavior** — if certificate pinning validation fails: telemetry transmission is blocked; the failure event is logged locally; agent health is marked DEGRADED; local buffering continues. This protects against MITM attacks. 

**Health Monitoring Module** — responsibilities: service health monitoring, API connectivity monitoring, telemetry collection monitoring, SQLite buffer monitoring. 

- Heartbeat Interval: 60 seconds. 

- **Heartbeat Payload:** 

##### **{** 

"endpointId" **:** "endpoint-001" **,** "agentVersion" **:** "1.0.3" **,** "policyVersion" **:** "4" **,** "status" **:** "HEALTHY" **,** "timestamp" **:** "2026-06-18T10:00:00Z" 

**}** 

#### • **Supported States and Thresholds:** 

- HEALTHY — heartbeat received normally; all agent components operational. 

- DEGRADED — partial functionality loss (e.g., API unreachable, telemetry failure, buffer nearing capacity, certificate validation failure); triggered after missing 3 consecutive heartbeats. 

- OFFLINE — triggered after missing 10 consecutive heartbeats. 

## **8. Agent Installation & Deployment** 

- **Installation Method:** SentinelAgent.msi — a dedicated installer distinct from the Desktop Application’s SentinelConsole.msi (Section 22a). See Section 34.8 for the two-installer rationale. 

- **Supported Deployment Methods:** Manual Installation, Group Policy (GPO), SCCM, Microsoft Intune. 

- **Execution Mode:** Windows Service. 

- **Startup Type:** Automatic — the agent starts whenever the endpoint boots. 

8 

- **Upgrade Method:** Manual MSI redeployment, per installer — SentinelAgent.msi and SentinelConsole.msi are upgraded independently of each other (see Section 30, Upgrade Strategy). 

## **9. Sentinel Unified Event Schema (SUES)** 

### **Purpose** 

SUES is the canonical event model for Sentinel. Every telemetry source must be transformed into SUES before storage, detection, threat hunting, or graph processing. 

### **Mandatory Fields** 

|Field|Description|
|---|---|
|eventId|Unique Event ID|
|schemaVersion|Schema Version — every SUES event<br>must declare its schema version,<br>e.g. "schemaVersion": "1.0"|
|eventType|Event Type|
|source|Telemetry Source|
|timestamp|Event Timestamp|
|organizationId|Organization Identifer|
|endpointId|Endpoint Identifer|
|hostname|Hostname|
|userId|Associated User|
|severity|Event Severity — one of: INFORMATIONAL,<br>LOW, MEDIUM, HIGH, CRITICAL|



### **Optional Fields** 

processName, processId, parentProcessName, parentProcessId, commandLine, domain, ipAddress, destinationPort, protocol, filePath, registryKey, alertId, incidentId. 

Unknown fields shall be preserved but ignored by processing components. 

Future schema versions shall maintain backward compatibility whenever possible. 

### **Supported Event Types** 

PROCESS_START, PROCESS_STOP, NETWORK_CONNECTION, DNS_QUERY, FILE_ACCESS, FILE_MODIFICATION, REGISTRY_MODIFICATION, POWERSHELL_EXECUTION, ALERT, INCIDENT. 

9 

### **Common Event Type Schemas** 

**{** "eventType" **:** "PROCESS_START" **,** "processName" **:** "powershell.exe" **,** "processId" **:** 1234 **,** "parentProcessName" **:** "winword.exe" **,** "parentProcessId" **:** 567 **,** "commandLine" **:** "powershell -enc ..." **} {** "eventType" **:** "DNS_QUERY" **,** "domain" **:** "evil.com" **,** "resolvedIp" **:** "1.2.3.4" **}** 

**{** "eventType" **:** "NETWORK_CONNECTION" **,** "sourceIp" **:** "10.0.0.5" **,** "destinationIp" **:** "1.2.3.4" **,** "destinationPort" **:** 443 **,** "protocol" **:** "TCP" **} {** "eventType" **:** "FILE_ACCESS" **,** "filePath" **:** "C:\\Users\\User\\Downloads\\payload.exe" **,** "accessType" **:** "READ" **}** 

**{** "eventType" **:** "REGISTRY_MODIFICATION" **,** "registryKey" **:** "HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Run" **,** "operation" **:** "CREATE" **}** 

**{** "eventType" **:** "ALERT" **,** "alertId" **:** "alert-001" **,** "ruleId" **:** "DET-006" **,** "severity" **:** "HIGH" **,** "mitreTechnique" **:** "T1059.001" **}** 

10 

### **Data Consumers** 

SUES events are consumed by: Detection Engine, ML Anomaly Engine, Incident Correlation Engine, Attack Graph Engine, Threat Hunting Engine, Reporting Engine. 

## **10. Telemetry Collection & Retention** 

### **Sources** 

ETW (process activity, network activity, security events); AMSI (script content, script execution, PowerShell activity); Sysmon (process creation, network connections, registry activity, file activity); Windows Event Logs (authentication events, security events, system events). 

### **Policy-Based Collection** 

Collection is configurable per organization via the Policy Engine (Section 7): toggles for Process Events, Network Events, Registry Events, and File Events. Changes are versioned and propagated to agents as described in Section 7. 

### **Retention** 

⚠ Default Retention: 90 days (best-effort reconstruction — confirm against PRD source). Configurable Options: 30 / 60 / 90 / 365 days (best-effort reconstruction — confirm against PRD before implementing as a hard requirement). 

## **11. Event Processing Pipeline** 

1. **Collection** — telemetry collected from ETW, AMSI, Sysmon, Windows Event Logs. 

2. **Normalization** — raw telemetry converted to SUES format. 

3. **Local Buffering** — events written to SQLite for offline operation and reliability. 

4. **API Ingestion** — events securely transmitted to the backend API. 

5. **Redis Streams** — events published to sentinel-events. 

6. **Detection Processing** — Detection Engine, Sigma Engine, IOC Matching, and ML Anomaly Engine consume events. 

7. **Storage** — results stored in PostgreSQL and Neo4j. 

8. **Presentation** — processed data available through Alerts, Incidents, Threat Hunting, Attack Graphs, and Reports. 

11 

## **12. Detection Engine** 

### **Purpose** 

Identify malicious or suspicious activity from normalized SUES events. The engine consumes events from Redis Streams and generates alerts. 

### **Responsibilities** 

Behavioral detection, IOC matching, MITRE ATT&CK mapping, alert generation, alert deduplication. 

### **Target Coverage** 

15 native Behavioral Rules operational at release (see categories below); 25–40 Sigma Rules operational at release (Section 13). 

### **Detection Categories** 

- **Initial Access** — Office →PowerShell, Office →CMD, Office →WScript, Office →MSHTA, suspicious PDF reader child process. 

- **Execution** — Encoded PowerShell, PowerShell download cradle, Rundll32 abuse, Regsvr32 abuse, Certutil abuse, MSHTA abuse, WMI execution. 

- **Persistence** — Run registry keys, startup folder persistence, scheduled tasks, service creation. 

- **Defense Evasion** — AMSI tampering, PowerShell obfuscation, Sysmon disabled, Sentinel service disabled. 

- **Credential Access** — LSASS access, Mimikatz indicators. 

- **Discovery** — User enumeration, group enumeration, host discovery. 

- **Lateral Movement** — Pass-the-hash indicators, remote service creation, admin share access, PsExec-style remote execution. 

- **Command & Control** — DNS tunneling indicators, beaconing indicators, known malicious domains/IPs. 

### **Rule Execution Model** 

Execution strategy: sequential, priority-ordered, multi-match enabled (priority order: Critical →High →Medium →Low). A single event may trigger multiple detections — e.g., Office →PowerShell + Encoded PowerShell + connection to a malicious domain may generate three alerts. 

### **Alert Deduplication** 

Duplicate alerts are suppressed within a 60-second window. Criteria: same rule, same endpoint, same user. Instead of generating multiple alerts, the occurrence counter on the existing alert is incremented. The occurrence count shall be visible to analysts on the Alert Details page. 

12 

### **Alert Generation SLA** 

**End-to-end target (PRD Goal 1):** High-severity alerts shall be visible in the Desktop Application within **<5 seconds** from event occurrence. This is the product-level commitment. 

**Technical sub-boundary:** Alert Generation (Normalized Event →Redis Stream → Detection Evaluation →Alert Persisted) shall complete within <5 seconds. Desktop Application rendering time is an additional budget; the polling interval (see Section 24) is set to 5 seconds for alerts to support the end-to-end target. 

### **Alert Output Schema** 

Generated alerts contain: Alert ID, Rule ID, Severity, Endpoint, User, MITRE Technique, Timestamp, Evidence, Occurrence Count. 

## **13. Sigma Engine** 

### **Purpose** 

Provide community-driven detection coverage using Sigma rules. Target: 25–40 Sigma Rules operational at release. 

### **Architecture** 

Sigma rules are converted into C# detection predicates during build time using a Sigma-to-Sentinel mapping tool. Runtime Sigma compilation is not supported. 

### **Processing Flow** 

Sigma Rule →Build-Time Conversion →C# Predicate →Detection Engine 

### **Rule Sources** 

Internal rule repository, community Sigma rules. 

### **Future Enhancements** 

Deferred to a future release: dynamic (runtime) rule updates. 

13 

## **14. ML Anomaly Detection Engine** 

### **Purpose** 

Identify suspicious activity not detected by traditional rule-based or signature-based detection. 

### **Role in the Detection Pipeline** 

The ML Anomaly Engine acts as: (1) a standalone Low/Medium severity detector, and (2) a secondary confidence signal that enriches alerts generated by other engines. It supplements, but never replaces, deterministic detection. 

### **Detection Model** 

Initial model: Isolation Forest, trained per endpoint. 

### **Feature Inputs (examples)** 

Process execution frequency, parent-process frequency, process execution time patterns, network activity volume, domain diversity (distinct domains contacted), login activity patterns. 

### **Baseline / Learning Period** 

New endpoints enter a learning period. Default Learning Period: 7 days. During learning: 

- Anomaly scores are generated and recorded. 

- Only Informational-severity alerts are produced. 

- Informational alerts are visible on the Alerts page. 

- Informational alerts do **not** participate in Incident Correlation. 

- Informational alerts do **not** affect incident severity. 

- Purpose: model visibility and tuning, not actionable detection. 

### **Endpoint Reimaging Handling** 

If an endpoint is reimaged: a new Endpoint ID is generated; the previous ML baseline is **not** reused; a new 7-day learning period begins automatically under the new Endpoint ID. 

### **Alert Generation Constraints** 

The ML Anomaly Engine may independently generate Low or Medium severity alerts. It shall **not** independently generate High or Critical severity alerts. High/Critical severity requires confirming signal from Behavioral Rules, Sigma Rules, or IOC Matching — the ML engine’s anomaly score may attach to such alerts as supporting evidence, but cannot itself elevate severity above Medium. 

14 

### **Output** 

An Anomaly Score, used for: alert prioritization within a severity tier, confidence enhancement on rule-based alerts, and surfacing of unknown/novel threats. 

### **Implementation Notes (recommended, not PRD-mandated)** 

Run as a dedicated scoring worker (see Section 6) consuming SUES events from Redis Streams. Persist per-endpoint baselines and rolling feature statistics in PostgreSQL. Re-score on a defined cadence (e.g., per batch of new events) rather than per-event, to bound CPU cost. 

## **15. Threat Intelligence Engine (IOC Matching)** 

### **Purpose** 

Provide IOC-based detection capabilities. 

### **Supported IOC Types** 

IP Address, Domain, URL, File Hash. 

### **Upload Methods** 

#### **JSON Upload:** 

[ **{** "iocType" **:** "DOMAIN" **,** "value" **:** "evil.com" **,** "severity" **:** "HIGH" **}** ] 

#### **CSV Upload:** 

iocType,value,severity DOMAIN,evil.com,HIGH IP,1.2.3.4,CRITICAL 

### **Validation Rules** 

Reject malformed entries; reject unsupported IOC types; deduplicate identical IOCs. 

### **Matching Strategy** 

Incoming telemetry is compared against IOC records. A match generates an alert. 

15 

### **IOC Lifecycle** 

⚠ Default Expiration: 180 days (best-effort reconstruction — confirm against PRD). Expired indicators are ignored automatically. 

**Expiration Notifications** — Security Administrators receive a notification when an IOC’s expiration is approaching (Default Warning Period: 30 days before expiration) and when expiration has occurred. 

### **Future Enhancements** 

Deferred: MISP integration, STIX/TAXII, Abuse.ch, AlienVault OTX, automatic threat intelligence feeds (manual IOC upload only, currently). 

## **16. Incident Correlation Engine** 

### **Purpose** 

Group related alerts into incidents. 

### **Correlation Criteria** 

An alert may join an existing incident when all of the following factors align: 

- **Endpoint** — the alert originates from the same endpoint as an existing open incident. 

- **User** — the alert involves the same user as the existing incident. 

- **Process** — the alert involves the same process (by name or process ID) as the existing incident. 

- **Time Window** — the alert falls within the correlation window of the existing incident. 

- **Shared Indicators** — the alert shares one or more indicators (IOC match, MITRE technique, domain, IP, file hash) with the existing incident. 

All five factors are evaluated together; the correlation engine uses the combination to determine whether an alert belongs to an existing incident or should seed a new one. 

### **Correlation Window** 

Default: 15 minutes. 

### **Post-Window Handling** 

An alert arriving after the correlation window **shall create a new incident** . This is the unconditional PRD-mandated behavior (PRD §13). 

16 

_Engineering note: a prior TRD revision carried a technical exception allowing late alerts to join an existing incident when endpoint, user, and attack-chain indicators strongly matched. That exception is removed here because it contradicts the PRD. If product determines that such an exception is desirable, it must be introduced via a formal PRD amendment before being implemented. Until that amendment exists, the implementation shall follow the PRD: late alert →new incident, always._ 

### **Manual Operations** 

Analysts may merge incidents, split incidents, and reassign alerts. 

### **Incident Merge Rules** 

- **Severity** — highest severity wins. 

- **Status** — most active status wins, priority order: Investigating →Open →Resolved →Closed. 

- **Assignee** — primary incident assignee is retained. 

- All alerts, notes, evidence, entities, and timelines from merged incidents are preserved. 

### **Incident Assignment** 

Incidents are unassigned by default. Security Analysts may self-assign incidents. Security Administrators may assign incidents to analysts within their organization. Only one primary assignee may exist at a time. 

### **Incident Alert Capacity** 

An incident may contain an unlimited number of alerts. The Incident Details page paginates at 100 alerts per page. 

### **Escalation Logic** 

Alerts mapped to Credential Access or Defense Evasion categories carry a minimum severity of High. Multiple High-severity alerts within an incident may escalate the incident severity to Critical. 

### **Incident States** 

Open, Investigating, Resolved, Closed. 

### **Incident Severities** 

Low, Medium, High, Critical. 

17 

## **17. Threat Hunting Engine** 

### **Purpose** 

Enable analysts to proactively search historical telemetry for suspicious activity — known threats, emerging threats, suspicious behavior, and incident-related activity — without waiting for alerts. 

### **Supported Search Targets** 

Users, Endpoints, Processes, Files, Domains, IP Addresses, Alerts, Incidents. 

### **Filtered Search** 

Support filtering by: Time Range, Endpoint, User, Process, File, Domain, IP Address, Severity. 

Results support: Sorting, Filtering, Pagination, Entity Pivoting. 

### **Threat Hunting Results Table** 

|Column|Description|
|---|---|
|Timestamp|Event Time|
|Entity Type|User, Endpoint, Process, File, Domain, IP,<br>Alert, Incident|
|Entity Name|Entity Identifer|
|Endpoint|Associated Endpoint|
|User|Associated User|
|Severity|Event Severity|
|Related Alert Count|Linked Alerts|



### **Entity Pivoting** 

Analysts shall be able to pivot between related entities without manual searching. Example pivot path: 

User →Endpoint →Process →File →Domain/IP →Alert →Incident 

### **Named Investigation Workflows** 

#### **Workflow 1 — Alert-Based Investigation** 

Alert →Endpoint →Process →File →Domain/IP →Incident 

#### **Workflow 2 — User-Based Investigation** 

User →Endpoints →Processes →Alerts →Incidents 

#### **Workflow 3 — IOC-Based Investigation** 

18 

#### **Workflow 4 — File Investigation** 

File →Creating Process →Parent Process →Endpoint →User →Related Alerts 

### **Search Performance** 

Normal hunting queries shall return results within **<4 seconds** under supported deployment scale. 

### **Access Control** 

|Role|Search Scope|
|---|---|
|Owner|All organizations|
|Security Administrator|Own organization only|
|Security Analyst|Own organization only|



### **Design Objectives** 

Threat Hunting shall provide: fast investigation workflows, historical visibility, efficient entity pivoting, minimal navigation depth, context-rich investigations. 

### **Deferred Features** 

Deferred to a future release: Saved Hunts, Scheduled Hunts, Advanced Query Language, Boolean Operators, Query Templates. 

## **18. PostgreSQL Architecture** 

### **Purpose** 

Primary operational datastore. ⚠ Hosted via Supabase (managed cloud Postgres) rather than self-hosted — see Section 6/Appendix A. The backend connects via a standard Postgres connection string (Npgsql/EF Core); Supabase’s built-in Auth, PostgREST API layer, and Row-Level-Security policies are not used, since Keycloak remains the identity provider and the backend talks to Postgres directly, not through Supabase’s client SDK. 

**Connection pooling requirement:** use Supabase’s pooled connection string (transaction pooler, typically port 6543) for the backend’s connection pool rather than the direct connection (port 5432). Supabase enforces a hard cap on direct/concurrent connections per project tier; a backend service opening its normal EF Core connection pool against the direct port can exhaust that cap under load. This is an operational requirement, not an optimization — confirm the pooled connection string is what’s configured before load testing. 

19 

### **Stored Data** 

- **Organizations** — tenant records; company_scope flag distinguishes SentinelCompany-level data from tenant data. 

- **Users** — accounts, roles, authentication metadata. 

- **Licenses** — endpoint cap and current count per organization. 

- **Applications** — onboarding application intake records. 

- **Endpoints** — inventory, agent versions, status. 

- **Events** — normalized events, event metadata. 

- **Alerts** — detection results, alert metadata, occurrence counts. 

- **Incidents** — correlated alerts, investigation data. 

- **SOAR Actions** — request/approval/execution state for Kill Process, Endpoint Isolation, Create Incident. 

- **Deception Assets** — Honey File and Canary Token definitions, linked to endpoints. 

- **Policies** — agent collection policies, configuration profiles. 

- **Threat Intelligence** — IOC records, expiration metadata. 

- **Reports** — generated report metadata and (for async jobs) artifact references. 

- **ML Baselines** — per-endpoint feature statistics and model parameters for the ML Anomaly Engine. 

- **Audit Logs** — authentication events, authorization events, administrative actions, report actions, SOAR action transitions. 

### **Requirements** 

Indexed queries, foreign keys, backup support, organization-level isolation. Every tenant-scoped table includes organization_id. Sentinel-Company-level data (e.g., pre-approval applications, cross-organization analytics aggregates) instead carries a company_scope flag — this is a second isolation dimension not present in the singletenant-type model and requires its own enforcement check, distinct from organization_id filtering, to prevent a Customer Organization from ever querying platformlevel data. 

## **19. Neo4j Architecture (Attack Graph Storage)** 

### **Purpose** 

Store entity relationships and attack paths. Neo4j shall never store raw telemetry. 

### **Node Types** 

User, Endpoint, Process, File, Domain, IP Address, Alert, Incident. 

### **Relationship Types** 

20 

|Relationship|Example|
|---|---|
|LOGGED_INTO (User →Endpoint)|Alice LOGGED_INTO PC-001|
|EXECUTED (Endpoint →Process)<br>CREATED (Process →Process)|PC-001 EXECUTED powershell.exe<br>WINWORD.EXE CREATED<br>powershell.exe|
|ACCESSED (Process →File)|powershell.exe ACCESSED salary.xlsx|
|CREATED (Process →File)<br>MODIFIED (Process →File)|powershell.exe CREATED payload.exe<br>ransomware.exe MODIFIED<br>document.docx|
|DELETED (Process →File)|malware.exe DELETED backup.zip|
|LOADED (Process →File)|rundll32.exe LOADED malicious.dll|
|CONNECTED_TO (Process →Domain)|powershell.exe CONNECTED_TO<br>evil.com|
|RESOLVES_TO (Domain →IP Address)<br>TRIGGERED_BY (Alert →|evil.com RESOLVES_TO 185.66.22.11<br>Encoded PowerShell Alert|
|Process/File/Domain/IP)|TRIGGERED_BY powershell.exe|
|AFFECTS (Alert →Endpoint)|Credential Dumping Alert AFFECTS<br>PC-001|
|PART_OF (Alert →Incident)|PowerShell Alert PART_OF Incident-123|
|INVOLVES (Incident →User)|Incident-123 INVOLVES Alice|



_Note: CREATED is overloaded across Process→Process (spawning) and Process→File (file creation); both are valid uses of the same relationship type and disambiguated by the node types on either end._ 

### **Graph Generation Workflow** 

Graph updates are synchronous. 

### **Failure Handling** 

If Neo4j becomes unavailable: alert processing continues, incident processing continues, graph features become unavailable. 

## **20. Redis Architecture** 

### **Purpose** 

Decouple ingestion from detection processing. 

### **Technology** 

Redis Streams. 

21 

### **Stream Configuration** 

Stream Name: sentinel-events. Consumer Group: sentinel-detection-group. Consumers: 1 detection worker. 

### **Acknowledgement Strategy** 

Manual acknowledgement. 

### **Message Retention** 

24 hours, with automatic stream trimming after the retention period. 

### **Failure Handling** 

Unacknowledged messages remain pending and are reprocessed when workers recover. 

### **Future Enhancements** 

Deferred: multiple consumers, distributed detection workers, multiple consumer groups. 

## **21. Reporting Engine** 

### **Purpose** 

Provide security reporting for analysts, administrators, management, and platform owners: incident documentation, operations visibility, trend analysis, compliance support, executive communication. 

### **Report Types** 

**Incident Report** — incident summary, severity, timeline, related alerts, related entities, investigation notes, resolution actions, closure details. 

**Security Operations Report** — reporting period, alert statistics, incident statistics, detection statistics, endpoint statistics, analyst activity, trend analysis. 

**Executive Summary Report** — reporting period, security overview, critical incidents, incident/detection trends, risk overview, key metrics. 

### **Report Filters** 

Reports shall support filtering by: Time Range, Severity, Endpoint, User, Incident Status, Detection Source. 

22 

### **Generation & Performance** 

Manual generation only (analysts generate on demand); scheduled report generation is deferred to a future release. Target: reports generated within **30 seconds** for normal deployment sizes. Larger reports may be generated asynchronously via the background job mechanism described in Section 6. 

### **Export Formats** 

Supported: CSV, JSON. Deferred: PDF export. 

### **Access Control Matrix** 

|Role|Scope|May Generate|
|---|---|---|
|Owner|All organizations|Incident, Security<br>Operations, Executive<br>Summary|
|Security Administrator|Own organization only|Incident, Security<br>Operations, Executive<br>Summary|
|Security Analyst|Own organization only|Incident, Security<br>Operations _(not Executive_<br>_Summary)_|



Report access restrictions are enforced at the authentication layer, authorization layer, API layer, and database query layer — UI restrictions alone are never relied upon. 

### **Audit Logging** 

Logged report actions: Report Generation, Report Export, Report Download. Logged fields: User, Role, Timestamp, Report Type, Export Format. 

## **22. Authentication, Identity & RBAC Architecture** 

### **Identity Provider** 

Keycloak. Authentication: JWT-based. Authorization: Role-Based Access Control (RBAC). Registration Model: no public registration/signup — all users are created by authorized administrators. 

### **Login Flow** 

User →Login Page →Keycloak Authentication →JWT Access Token →Dashboard Access 

23 

### **Roles** 

**Owner** — Platform-level role, exists outside organizational boundaries, part of the persisted **Sentinel Company** entity. Responsibilities: create organizations, delete organizations, assign the first CSO for a new organization, license management, system maintenance, platform recovery, global configuration, onboarding approval, cross-organization analytics. Access: all organizations, all users, all endpoints, all reports. The Owner is _not_ responsible for day-to-day investigations. 

**Support Team** — Sentinel-Company-tier role, platform-wide (not per-organization) permissions scoped to: reviewing onboarding applications, onboarding organizations, and providing temporary, time-limited support access to a customer organization’s telemetry. No SOAR trigger/approval authority. No unrestricted or standing crossorganization access — access to a specific organization’s data is limited to an active, time-boxed support engagement. 

**CSO** — Customer-Organization-tier executive role, above Security Administrator within the organization. Responsibilities: organization ownership, executive oversight, **manage Security Administrators** (create/deactivate Security Administrator accounts), licensing/subscription point of contact, report review, and SOAR approval **only when the organization is configured for executive-approval mode** (Section 34.1) — this is the exception path, not the default. **Security Administrator** — Organization-level role. Responsibilities: day-to-day platform configuration — deploy Sentinel, **manage Security Analysts** (create/deactivate Analyst accounts), manage policies, manage IOC datasets, manage organization configuration, deploy deception assets — and SOAR approval **by default** for organizations not configured for executive-approval mode. Access: organization resources only. 

**Security Analyst** — Investigation-level role. Responsibilities: monitor alerts, investigate incidents, use threat hunting, generate reports, analyze attack graphs, request SOAR actions (cannot self-approve, regardless of which role approves). Cannot create users, modify policies, or change organization settings. 

_The prior TRD’s “Viewer” (read-only) role is removed — it is not present in the updated PRD’s role model. Confirm with product before retiring it from any already-deployed Keycloak realm._ 

### **Technical Modeling in Keycloak** 

- **Owner** modeled as a realm-level role with no organization-group membership. JWTs issued to Owner accounts carry no organizationId claim (or a wildcard), enforced at the API layer to permit cross-organization access. 

- **Support Team** modeled as a realm-level role, distinct from Owner, with no organization-group membership by default. Access to a specific organization’s data is granted per active, time-boxed support engagement rather than standing group membership. 

- **Organizations** modeled as Keycloak Groups, one group per organization. 

- **CSO, Security Administrator,** and **Security Analyst** modeled as realm roles 

24 

assigned in the context of group membership; a user’s JWT carries both their role and their organizationId (derived from group membership). CSO sits above Security Administrator in the same organization’s group. 

### **Keycloak Bootstrap** 

Automatic provisioning on first deployment creates: Sentinel Realm, Dashboard Client, API Client, owner role, security-administrator role, security-analyst role. Realm configuration is imported automatically through Docker startup scripts — no manual Keycloak configuration is required for initial setup. 

### **Bootstrap Owner** 

A platform Owner account is created automatically during first deployment. The Owner then creates the first Organization and assigns its first Security Administrator (fulfilling the PRD’s “Initial Administrator Assignment” responsibility). 

### **Owner Recovery Procedure** 

Sentinel maintains a break-glass recovery procedure for the Owner role. If the Owner account becomes locked, recovery occurs through direct Keycloak administrative access (Keycloak Admin Console or kcadm.sh), bypassing the Sentinel application layer entirely. Security Administrators have no authority to recover Owner accounts — this is enforced by the absence of any in-application recovery path for the Owner role. 

### **Session Management** 

Access Token: 15 minutes. Refresh Token: 7 days. Logout invalidates the active session. 

### **Account Lockout** 

⚠ Failed Login Threshold: 5 attempts (best-effort reconstruction — confirm against PRD). Lockout Duration: 15 minutes. Implemented through Keycloak brute-force detection. 

### **Password Policy** 

Minimum Length: 12 characters. Requirements: uppercase letter, lowercase letter, number, special character. Managed through Keycloak password policies. Password storage algorithm: Argon2id (Keycloak default credential hashing). 

### **Password Reset** 

Requires SMTP configuration — reset links are delivered by email. SMTP configuration is mandatory for production deployments (without it, the password reset feature is unavailable). Reset tokens are single-use, securely random, and ⚠ expire after 15 minutes (best-effort reconstruction — confirm against PRD source; see Section 1.2). 

25 

### **User Lifecycle Management** 

All user accounts are created by authorized administrators (no public selfregistration). 

#### **User States:** 

|State|Description|
|---|---|
|Active|User can authenticate and access<br>Sentinel.|
|Deactivated|User cannot authenticate. Account data<br>and audit history are retained. Role<br>assignment is preserved.|
|Reactivated|A deactivated user is restored to Active<br>state by a Security Administrator (own<br>organization) or Owner (any<br>organization).|



#### **State Transitions:** 

- **Create** — Security Administrator creates a user within their organization; Owner may create users in any organization. User starts in Active state. 

- **Deactivate** — Security Administrator deactivates a user within their organization; Owner may deactivate any user. Implemented via Keycloak account disable. 

- **Reactivate** — Security Administrator reactivates a user within their organization; Owner may reactivate any user. Implemented via Keycloak account reenable. 

- **Delete** — User deletion is not a required operation; deactivation is the standard lifecycle action. If deletion is implemented, audit log entries for that user must be preserved. 

### **Audit Logging** 

Events logged: login, logout, failed authentication, user creation, user modification, user deactivation, user reactivation, policy changes, IOC uploads, administrative actions. 

### **Platform Administration (Owner Only)** 

The Owner is responsible for platform-level administration including: 

- Organization creation and deletion 

- Initial Security Administrator assignment per organization 

- License management 

- Platform configuration (global settings, certificate management, system configuration) 

- System maintenance and platform recovery 

26 

These functions are accessible only to the Owner role and are enforced at the API and database layers — not only at the UI layer. 

## **23. Multi-Tenant (Organization) Isolation Architecture** 

### **Tenant Model** 

Every customer organization is represented as an isolated tenant (“Organization” in product terminology). 

Organization A ├──Users ├──Endpoints ├──Alerts └──Incidents 

Organization B ├──Users ├──Endpoints ├──Alerts └──Incidents 

### **Tenant Identifier** 

Every business object must contain organizationId — e.g., User, Endpoint, Event, Alert, Incident, Policy, IOC, Report. 

### **Isolation Enforcement** 

- **API Layer** — every request is scoped to the authenticated organization. 

- **Database Layer** — every query includes organization filtering, e.g. WHERE organization_id = ?. 

- **Search Layer** — threat hunting results are restricted to the requesting organization. 

### **Cross-Organization Access** 

Cross-organization access is prohibited, with two exceptions: the **Owner** role, which by design operates above organizational boundaries, and **Support Team** , which may access a single organization’s data on a scoped, engagement-limited basis (⚠ exact enforcement mechanism not confirmed — see Section 34, Open Decision #2). CSO, Security Administrator, and Security Analyst searches/access are restricted to their own organization; Owner-scoped threat hunting searches across all organizations. 

27 

## **24. Sentinel Desktop Application & Public Website Architecture** 

**Revision note:** superseded from “Dashboard Architecture” (browserbased React dashboard). The authenticated interface is now the Sentinel Desktop Application — Electron + React, distributed as SentinelConsole.msi. The public website is a fully separate, unauthenticated artifact with its own release cycle. See Section 34.8 for why these are two installers and two deployments, not one. 

### **24a. Public Website (Unauthenticated)** 

Pages: Home, Features, Pricing, About, Contact, Apply, Privacy Policy, Terms & Conditions, Download. No login page — login only happens inside the Desktop Application (24b). Download page links to both installers (SentinelConsole.msi, SentinelAgent.msi), clearly labeled so neither is installed on the wrong machine type. No authenticated code, no Cytoscape.js, no OIDC client of any kind ships in this artifact. 

### **24b. Sentinel Desktop Application (Authenticated)** 

**Pages — Customer Organization view:** - **Overview** — active endpoints, active alerts, open incidents, critical incidents, detection trends. Displays: Total Endpoints, Healthy Endpoints, Degraded Endpoints, Offline Endpoints, Active Alerts, Critical Alerts, High Alerts, Open Incidents, Investigating Incidents, Critical Incidents, Recent Alerts, Recent Incidents. - **Endpoints** — inventory, health status, agent version, policy version, last heartbeat. - **Alerts** — severity, alert name, endpoint, user, detection source, MITRE technique, timestamp, status; supports search, filtering, sorting, pagination. - **Incidents** — summary, timeline, evidence, notes, related alerts, related entities, attack graph. - **Threat Hunting** — search interface, filters, results table (see Section 17), pivot links. - **Attack Graph** — powered by Neo4j + Cytoscape.js, rendered inside the Desktop Application’s embedded web content; supports node expansion, relationship exploration, entity inspection, attack path visualization. - **SOAR** — action request form (Security Analyst), approval queue (CSO/Security Administrator), audit history (Section 34.1). - **Deception** — Honey File / Canary Token deployment (Security Administrator), trigger history (Section 34.2). - **Reports** — Incident, Security Operations, and Executive Summary reports (Section 21). - **Users** — user accounts, assigned roles, account status. Visible to Owner (all organizations), CSO and Security Administrator (own organization). Account creation is by self-service invitation (Section 34.9), not manual CRUD-only setup. - **Settings** _(split by scope)_ : - **Platform Configuration** — Owner only. Licensing, global platform settings, certificate - management, system configuration. **Organization Configuration** — CSO/Security Administrator. User management, IOC management, policy management, organization settings. _(Unchanged: policy management is Security Administrator’s responsibility, not CSO’s — CSO manages organization configuration and Security Administrator accounts.)_ 

**Pages — Sentinel Company view (Owner / Support Team):** - **Organizations** — organization list, license status, endpoint counts, revenue (⚠ blocked pending 

28 

- billing data source, Section 34.7). **Support Queue** — engagement-scoped customer telemetry view (⚠ scope not confirmed, Section 34, Open Decision #2). - **Onboarding Review** — application approval queue (Section 34.4). 

Whether the Sentinel Company view ships as part of the same SentinelConsole.msi binary or as an internal-only tool is an open implementation question, not resolved by this document. 

**Login:** email and password only, no role selector at any point in the flow — the backend resolves organization, role, and permissions from the validated JWT, and the Desktop Application loads the corresponding view automatically. 

### **Application Data Flow** 

### **Polling Strategy** 

Alerts: 5 seconds. Incidents: 30 seconds. Endpoints: 60 seconds. Threat Hunting: on demand. Reports: on demand. Real-time WebSockets and Server-Sent Events are deferred to a future release. **Note:** these intervals were originally reasoned about in a browser-tab context (a tab that might be backgrounded or closed). An Electron app’s polling behavior when minimized/backgrounded should be revisited during implementation — polling on the same cadence indefinitely in the background has a different resource-usage profile than a browser tab a user might just close. 

### **Frontend Requirements** 

- **Loading State** — skeleton loaders, not spinner-only indicators. 

- **Empty State** — e.g., “No Data Available.” 

- **Error State** — e.g., “Unable to Load Data,” with retry capability. 

- **Security** — the Desktop Application shall not expose stack traces, internal error detail, or sensitive configuration data. Its embedded web content shall run with contextIsolation: true, nodeIntegration: false, and sandbox: true — no direct Node.js or OS-level access exposed to rendered content. This is a mandatory security posture for any Electron-based application, not optional hardening, and doubly so for a security product. 

## **25. Security Architecture & Trust Model** 

### **Transport Security** 

All communications use TLS 1.3. 

29 

### **Certificate Pinning** 

Agent communication requires both TLS validation and certificate fingerprint validation; both checks must succeed. Failure behavior is defined in Section 7. 

### **Certificate Trust Model** 

The agent installer embeds a trusted server identity and server fingerprint. Connection requires both TLS validation success and fingerprint validation success. 

### **Certificate Strategy** 

Self-signed certificate, embedded during agent installation, 10-year validity. 

### **Certificate Compromise Recovery** 

If a pinned certificate becomes compromised, recovery requires: (1) new certificate generation, (2) agent redeployment, (3) manual trust replacement. Automatic certificate rotation and automated trust updates are deferred to a future release. 

### **Authentication & Authorization** 

JWT via Keycloak; RBAC with the Owner / Security Administrator / Security Analyst model (Section 22). 

## **26. Operational Security Requirements** 

### **Input Validation** 

All API requests validate required fields, data types, length constraints, and format constraints. Invalid requests are rejected. 

### **Input Sanitization** 

User-generated content is sanitized against Cross-Site Scripting (XSS), HTML injection, and script injection. Applicable fields: notes, comments, organization names, user content. Recommended approach: server-side HTML sanitization library plus React’s default output encoding as defense in depth. 

### **Cross-Origin Resource Sharing (CORS)** 

Production deployments use an approved origin allowlist. Wildcard origins (*) are not permitted in production; recommend a startup-time check that fails fast if a wildcard CORS origin is configured when running in a production environment. 

30 

### **Rate Limiting** 

|API Class|Limit|Scope|
|---|---|---|
|Authentication APIs|5 requests/minute|Per user/IP|
|Standard APIs|100 requests/minute|Per authenticated user|
|Administrative APIs|50 requests/minute|Owner, Security<br>Administrator|



Recommended implementation: ASP.NET Core rate-limiting middleware or a reverseproxy layer (e.g., Nginx), keyed by user ID for standard/admin tiers and by IP+user for the pre-authentication auth tier. 

### **Logging Requirements** 

Logged: authentication events, authorization events, alert generation, incident creation, administrative actions, system errors, report generation, report downloads. 

Log Retention — Default: 90 days. Configurable Options: 30 / 90 / 180 / 365 days. (90 days is listed as both the default and an explicit selectable tier so that deployments that have changed the retention setting can return to the default without manual intervention.) 

### **Monitoring** 

Monitor: API latency, error rates, agent heartbeats, database health, resource utilization. Tooling is an implementation choice (e.g., Prometheus/Grafana or equivalent self-hosted stack); the requirement is functional coverage of these signals, not a specific product. 

### **Operational Alerting** 

Generate operational alerts for: backend failures, database failures, agent communication failures, detection engine failures. 

### **Secrets Management** 

Sensitive credentials (database credentials, API keys, JWT secrets, service tokens) shall never be stored in source code. Managed via environment variables or equivalent secure configuration (e.g., Docker secrets, excluded from version control). 

### **Dependency Security** 

Dependencies are scanned regularly for known vulnerabilities (recommended: dotnet list package --vulnerable, npm audit, or an equivalent CI-integrated scanner). Security updates are applied according to risk severity. 

31 

## **27. Backup & Recovery** 

### **Purpose** 

Protect operational and investigative data. 

### **PostgreSQL Backup** 

⚠ **Changed from self-hosted assumption.** With PostgreSQL hosted on Supabase, backup responsibility is split: - **Supabase-managed backups** (tier-dependent — confirm the project’s plan includes daily backups and point-in-time recovery; freetier projects may have minimal or no automated backup coverage). - **Independent pg_dump as a second line of defense** , run externally against the Supabase connection string on the same daily cadence and 30-day retention as before — pg_dump is a client tool and works against any reachable Postgres endpoint, so this does not require self-hosting Postgres to keep doing. 

Do not rely on Supabase’s backups alone without confirming the plan tier actually includes them — this is a real gap if assumed silently. 

### **Neo4j Backup** 

Technology: neo4j-admin database dump. Frequency: daily. Retention: 30 days. Unaffected by the Supabase change — Neo4j remains self-hosted. 

### **Backup Storage** 

Neo4j: Docker-mounted backup volume (self-hosted, unchanged). PostgreSQL: pg_dump output stored wherever the operations team keeps backups (can still be a local Docker-mounted volume on your own infrastructure even though the source database itself is Supabase-hosted). 

### **Backup Validation & Restoration Testing** 

Backups shall be validated, and restoration shall be tested on a documented, recurring schedule, with documented recovery procedures — not validated only at backupcreation time. 

### **Recovery Objective** 

Target Recovery Time: 24 hours. 

### **Recovery Scope** 

Recoverable data: users, endpoints, events, alerts, incidents, policies, IOCs, graph relationships. 

32 

## **28. Performance Requirements** 

### **Endpoint Agent** 

CPU Usage: < 3%. RAM Usage: < 200 MB. Startup Time: < 10 seconds. Maximum Local Buffer Size: 1 GB. 

### **Platform Latency** 

High-Severity Alert Visibility: < 5 seconds end-to-end from event occurrence to dashboard visibility. Standard Telemetry Visibility: < 10 seconds from event occurrence to dashboard visibility. Dashboard Load: < 3 seconds. 

### **Investigation Workflow SLA** 

**End-to-end investigation workflow target (PRD Goal 3):** An analyst following a standard investigation workflow — Alert →Incident →Root Cause — shall be able to complete root-cause identification within **≤10 minutes** from the moment the alert is visible on the dashboard, under normal deployment conditions and assuming the relevant telemetry is present. 

This target is achieved through the combination of: - Alert visibility latency: < 5 seconds. - Incident load time: < 3 seconds. - Threat hunting query time: < 4 seconds. - Attack graph load time: < 5 seconds. - Navigation and pivot overhead: minimised by entity pivoting and the four named investigation workflows (Section 17). 

This is a system-design target, not a contractual SLA. It assumes the analyst is following a trained workflow and that the platform is operating within its validated scale (100 concurrent endpoints). 

### **Database Performance** 

Threat Hunting Queries: **< 4 seconds** for normal search workloads under supported deployment scale. Incident Loading: < 3 seconds. Attack Graph Loading: < 5 seconds. 

### **Scalability** 

Validated Endpoint Count: 100+ concurrent endpoints without architectural redesign. 

**Future Scaling Plan** — beyond 100 endpoints, scale via: horizontal API scaling, PostgreSQL optimization, Redis scaling, Neo4j scaling. 

33 

## **29. Deployment Topology** 

### **Containerized Services (Docker Compose)** 

- **sentinel-api** — event ingestion, detection processing, Desktop Application APIs. 

- **postgres** — operational data storage. 

- **neo4j** — relationship storage. 

- **redis** — event streaming. 

- **keycloak** — authentication, authorization. 

- **website** — public marketing site (static build, or a lightweight container serving it); unauthenticated only, no dashboard code. 

- **ml-worker** _(recommended)_ — ML Anomaly Engine scoring process (Section 14). 

**Revision note:** dashboard is removed from this list — the Sentinel Desktop Application is not a containerized web service. It’s a native Electron application, packaged as SentinelConsole.msi and distributed via the website’s Download page or GPO/SCCM/Intune, installed directly on user workstations. 

### **Endpoint Agent** 

Runs independently as a Windows Service, packaged as SentinelAgent.msi; not containerized (Sections 7–8). 

### **Sentinel Desktop Application** 

Runs independently as a native Electron application on user workstations, packaged as SentinelConsole.msi; not containerized, and — per Section 34.8 — never bundled with the Agent installer above. 

### **Deployment Safety** 

Deployments support rollback to the previous stable version without requiring full platform restoration. 

## **30. Failure Handling** 

|Component|Behavior on Failure|
|---|---|
|Agent Ofine|Events bufered in SQLite; upload<br>resumes after connectivity returns.|
|API Failure|Agent retries uploads; events remain<br>bufered.|
|Redis Failure|Events retried until the queue recovers.|



34 

|Component|Behavior on Failure|
|---|---|
|PostgreSQL Failure|Events remain queued; processing<br>resumes after recovery. Since<br>PostgreSQL is Supabase-hosted,<br>recovery time now depends on<br>Supabase’s status, not on-premises<br>restart control.|
|Neo4j Failure|Alert and incident processing continue;<br>graph features are disabled.|
|Keycloak Failure|Existing sessions remain valid; new<br>authentication requests fail.|



## **31. Upgrade Strategy** 

### **Upgrade Approach** 

Agent Upgrade Method: manual MSI deployment — administrators deploy newer agent versions manually. Backend Upgrade Method: Docker Compose redeployment (paired with the rollback capability described in Section 29). 

### **Future Release (Planned)** 

Automatic update checks, signed package verification, staged rollouts, rollback support, automatic certificate rotation, dynamic Sigma rule updates, scheduled report generation, PDF export, real-time WebSockets/SSE, automatic threat intelligence feed ingestion (MISP/STIX-TAXII/Abuse.ch/AlienVault OTX). 

## **32. Deferred Features (Out of Scope)** 

The architectural principle stated elsewhere in this document restricting Sentinel to “detection, investigation, and visibility” is retired — SOAR (discrete, approval-gated actions) and Deception Technology are in scope; see Section 34. 

|Feature Area|Examples|Target|
|---|---|---|
|Full Autonomous SOAR /|Fully automated|Out of scope. Discrete|
|Playbooks|remediation, multi-step<br>automated workfows|approval-gated actions<br>(Kill Process, Endpoint|
|||Isolation, Create Incident)<br>are in scope — see Section|
|||34.1.|



35 

|Feature Area|Examples|Target|
|---|---|---|
|Honey Credentials / AD<br>Decoys|Active Directory decoy<br>accounts, decoy<br>credentials|Not confrmed in scope.<br>Honey Files and Canary<br>Tokens are in scope — see<br>Section 34.2.|
|Advanced Threat Hunting|Saved hunts, scheduled<br>hunts, advanced query<br>language, boolean<br>operators, query<br>templates|Deferred|
|Advanced Risk Scoring /<br>UEBA|XGBoost confdence<br>scoring, user risk scores,<br>insider threat scoring,<br>behavioral profling|Deferred|
|Advanced Graph Analytics|Blast radius analysis, path<br>scoring, automated attack<br>chain discovery|Deferred|
|Cross-Platform Agents<br>Automatic Agent Updates|Linux, macOS<br>Automatic update checks,<br>signed packages, staged<br>rollouts|Deferred<br>Deferred|
|Automatic Threat|MISP, STIX/TAXII,|Deferred|
|Intelligence Feeds|Abuse.ch, AlienVault OTX||
|Automatic Certifcate<br>Rotation|Automated trust updates|Deferred|
|Zero Trust (dynamic trust<br>scoring)|Continuous device posture<br>scoring, dynamic access<br>decisions|⚠Not scheduled — see<br>Section 34.5 for the<br>narrower Zero Trust<br>interpretation that _is_ in<br>scope|
|Kernel Drivers|Custom kernel-level<br>telemetry drivers|Not scheduled|



## **33. Technical Success Criteria** 

Sentinel is technically complete when: 

**Endpoint Layer** — telemetry collection functions correctly from ETW, AMSI, Sysmon, and Windows Event Logs; SUES normalization functions correctly; SQLite buffering and eviction policy function correctly; heartbeats function correctly with correct HEALTHY/DEGRADED/OFFLINE transitions at the defined thresholds (3 and 10 missed heartbeats). 

**Processing Layer** — Redis Streams function correctly; Detection Engine generates alerts meeting the 15-rule behavioral target including Lateral Movement coverage; 

36 

Sigma Engine generates alerts meeting the 25–40 rule target; ML Anomaly Engine generates Low/Medium alerts and contributes confidence scores correctly; IOC Matching is operational; Incident Correlation functions correctly including all five correlation criteria (Endpoint, User, Process, Time Window, Shared Indicators), merge/assignment/escalation rules, and post-window behavior (late alerts create new incidents without exception). 

**Data Layer** — PostgreSQL stores data correctly with organization isolation enforced on all business objects; Neo4j stores all 14 defined relationship types correctly; backups function correctly and have been restoration-tested per documented schedule. 

**Dashboard Layer** — Overview, Alerts, Incidents, Threat Hunting (including all 4 investigation workflows and entity pivoting), Attack Graphs, SOAR (request/approval queue), Deception, Users, Reports (including Analyst Activity in Security Operations Reports), Sentinel Company pages (Organizations, Support Queue, Onboarding Review), and Settings (platform/organization split) render correctly; all pages implement loading, empty, and error states; frontend exposes no stack traces, internal errors, or sensitive configuration data. 

**Security Layer** — TLS 1.3 and certificate pinning (including failure and compromiserecovery behavior) function correctly; JWT authentication and the 5-role RBAC model (Owner, Support Team, CSO, Security Administrator, Security Analyst) function correctly; rate limiting, input validation/sanitization, and CORS allowlisting are enforced; account lockout, password policy, password reset (with SMTP requirement), and Owner break-glass recovery are verified; user lifecycle (create/deactivate/reactivate) is operational; SOAR actions cannot execute without a recorded, non-self approval; agent command channel authenticity is verified to the same standard as certificate pinning. 

**Operational Targets** — all performance targets achieved (high-severity alert <5s end-to-end; standard telemetry <10s; dashboard load <3s; threat hunting <4s; incident load <3s; attack graph <5s; investigation workflow root-cause ≤10 minutes); scalability target achieved (100 concurrent endpoints); recovery objective achieved (24-hour RTO); rollback procedure tested; backup restoration tested. 

**Platform Administration** — license management, organization management, onboarding approval, and platform configuration accessible to Owner only; Support Team access correctly scoped once Section 34 Open Decision #2 is resolved; operational monitoring and alerting verified; log retention configurable across all four tiers (30 / 90 / 180 / 365 days); Owner Analytics shows organization/endpoint counts (revenue metric explicitly excluded pending Section 34 Open Decision #4). 

## **34. SOAR, Deception, Licensing & Platform Operations Architecture** 

Remaining ⚠-marked items are genuinely unspecified and block correct implementation until resolved — see the Open Decisions list at the end of this section. Items not marked ⚠ are settled. 

37 

### **34.1 — SOAR Response Engine** 

**Scope:** Three discrete, human-gated actions — Kill Process, Endpoint Isolation, Create Incident. No playbooks, no automated workflows, no autonomous execution. 

**Approval policy:** organization-configurable. By default, a **Security Administrator** approves. An organization may be configured to require **CSO** approval instead (executive-approval mode) — this is the exception path, not the default. ⚠ The mechanism for setting this per-organization configuration (Owner-set at onboarding vs. CSO self-service toggle) is not specified. 

#### **Flow:** 

Security Analyst requests action (tied to an alert/incident) ↓ Action recorded, status: PENDING_APPROVAL ↓ Approver determined by organization's approval policy: ├──Default →Security Administrator reviews and approves/denies └──Executive-approval mode →CSO reviews and approves/denies ↓ ├──DENIED →action closed, no dispatch, logged 

└──APPROVED →dispatched to target agent via command channel (Section 34.6) ↓ 

Agent executes →result reported back →action status: EXECUTED / FAILED An Analyst can never self-approve their own request, regardless of which role approves. 

**Data model:** soar_actions table — id, organization_id, requested_by, approved_by, action_type, target_endpoint_id, status, requested_at, decided_at, executed_at, result. organizations table gains an approval_policy field (SECURITY_ADMIN_DEFAULT | EXECUTIVE_REQUIRED). Every field populated for every transition; no destructive action may exist in the table without a requested_by and, if executed, an approved_by. 

**Constraint (architectural, not optional):** approval is mandatory in code, not just enforced by UI — the execution API must reject any dispatch attempt for an action not in APPROVED status, at the API layer, matching the existing pattern used for RBAC enforcement elsewhere in this document (never UI-only). 

### **34.2 — Deception Technology** 

**Scope:** Honey Files and Canary Tokens only. 

#### **Flow:** 

Security Administrator defines deception asset (file path / token value) via policy ↓ Policy Engine pushes asset definition to target endpoint(s) (reuses existing policy ↓ Agent places decoy file / registers token locally 

38 

↓ 

Sysmon file-access telemetry detects access to decoy path (no new collector required) ↓ 

No new alert pipeline, no new telemetry source. This is a policy + detection-rule extension of existing components. 

### **34.3 — Licensing** 

licenses table (organization_id, endpoint_cap, current_endpoint_count, status, renewed_at, suspended_at). Lifecycle: 

Enforced at /endpoints/register — registration request rejected with an explicit error when current_endpoint_count >= endpoint_cap. Endpoint cap Owner-adjustable via Platform Configuration. ⚠ **Renewal trigger/cadence and Suspension’s enforcement behavior are not specified** — e.g., whether a suspended organization’s endpoints continue to be accepted for telemetry ingestion or are rejected outright, and whether the dashboard goes read-only. This must be resolved before status transitions are implemented, since it changes what the enforcement code actually does at the API layer, not just what state is recorded. 

### **34.4 — Manual Onboarding & Public Website** 

**Public site** (separately deployed from the Desktop Application, no shared release cycle): Apply, Privacy Policy, Terms & Conditions, Contact email, Download (Section 34.8). No signup path exists anywhere on the site. **Revision note:** there is no “Login (redirect)” page — that only made sense when the dashboard was itself a web page. Login happens exclusively inside the Sentinel Desktop Application. The Apply form submits to POST /applications — public, unauthenticated, and rate-limited more aggressively than the existing Authentication API tier given it has no credential gate at all. 

#### **Review flow:** 

Public Apply form submitted →application stored (status: PENDING) ↓ Support Team reviews (status: IN_REVIEW) ↓ Human discussion — sales/technical call (status: IN_DISCUSSION) ↓ 

SentinelConsole.msi download link 

This enforces “no organization can self-register” — every account traces back through this full status chain; there is no shortcut from Apply directly to an active organization. **Revision note:** email delivery here is a required production dependency, not condi- 

39 

tional on SMTP being configured — the CSO has no other path to their credentials or installer link. Earlier revisions phrased this as optional; that framing is retired. 

### **34.5 — Zero Trust (Narrow Interpretation)** 

In scope: per-request credential/certificate validation (no session-level implicit trust extended beyond token/cert expiry already defined in Section 22/25); strict noimplicit-access between Customer Organizations regardless of role, enforced at the same API+DB layer already mandated for RBAC. Out of scope, per Section 32: continuous device posture scoring, dynamic trust scoring, network micro-segmentation. 

### **34.6 — Agent Command Channel (Highest-Risk Item in This Document)** 

The agent architecture (Section 7) is **upload-only** : agent initiates all communication; the backend never initiates contact with an agent. SOAR execution (34.1) requires the reverse. 

⚠ **Not yet decided — options, with tradeoffs:** - **Command polling:** agent checks for pending commands during its existing 60-second heartbeat cycle. Lower risk (no new listening surface on the endpoint), higher latency (up to 60s before an isolation command is picked up). - **Persistent connection:** agent maintains an open channel for near-immediate command delivery. Lower latency, new attack surface, and a materially different threat model than the cert-pinned, agent-initiated-only design in Section 25. 

**Mandatory regardless of mechanism chosen:** - Command authenticity must be verified with the same rigor as the existing cert-pinning model — a compromised or spoofed command source must not be able to issue destructive commands. - Endpoint Isolation must have a defined, tested recovery path (e.g., a local allowlist for the management channel itself, so isolating an endpoint does not also cut off the ability to de-isolate it remotely). This is a design requirement, not an operational afterthought. - All command dispatch and execution must flow through the same audit logging model as Section 22’s existing audit requirements. 

**This is flagged as blocking:** Section 34.1 (SOAR) cannot be considered fully specified, let alone implemented, until this decision is made. 

### **34.7 — Owner Analytics** 

In scope: organization count, active endpoint count (both aggregable from existing organizations/endpoints tables), and revenue, calculated from active endpoint licenses and stored billing records. New billing_records table required (organization_id, license_id, amount, currency, period_start, period_end, recorded_at). ⚠ **The pricing/rate model that populates amount is not specified** — flat per-endpoint rate vs. tiered pricing vs. negotiated per-contract rate all imply different schema and calculation logic. 

40 

### **34.8 — Two-Installer Strategy (New)** 

Sentinel ships as two separate installers, deliberately, not one bundled package: - **SentinelAgent.msi** — Windows Service only (Section 7/8). Installed on every endpoint being protected. No user interface, no dependency on the Desktop Application. - **SentinelConsole.msi** — Sentinel Desktop Application only (Section 24b). Installed on Security Analyst, Security Administrator, and CSO workstations. No telemetry collection, no endpoint registration, no heartbeat participation of any kind. 

**Rationale:** the population of machines an organization wants monitored is not the same population its security team works from. Bundling would force every analyst’s own workstation to become a monitored endpoint, or force every monitored endpoint to carry a dashboard application nobody there uses. Both installers are built and versioned independently; upgrading one never requires touching the other. 

### **34.9 — User Invitation Flow (New)** 

Beyond the Owner-approval-triggered creation of the first CSO (Section 34.4), every subsequent account is created by self-service invitation: 

- CSO, inside Desktop Application →Users →Invite Security Administrator →POST /users/invite { email, role: "security-administrator" } 

   - →authorization check: can the caller's role invite the requested role? (CSO →security-administrator: yes. CSO →security-analyst: no.) 

   - →Keycloak account provisioned, scoped to caller's organization 

   - →Postgres users row inserted (Keycloak provisioning alone is not sufficient — every other org-scoped query joins against this table) 

   - →invitee emailed: username, temporary password/activation link, SentinelConsole.msi download link 

The same pattern applies one level down: a Security Administrator invites Security Analysts, never another Security Administrator. Neither role can invite a peer or superior — this is an authorization check enforced at the API layer against the caller’s own role, not a client-side restriction. If the invitation email fails to deliver, the inviting user (already authenticated) is shown the temporary password directly, the same fallback pattern as 34.4’s CSO onboarding email. 

**This supersedes** any earlier description of a Users page as a manual CRUD form with no email step — that model does not reflect how accounts are actually created below the first CSO. 

### **Open Decisions (Blocking)** 

1. _(Resolved)_ Is Sentinel Company a persisted entity? — **Resolved — see 34.0/Section 22: yes, a persisted platform-level entity.** 

2. _(Resolved)_ Exact Support Team permission boundary — **Resolved: review applications, onboard organizations, time-limited support access; no SOAR authority; no unrestricted cross-org access.** 

3. Command channel mechanism (34.6: polling vs. persistent) — still blocks all of Section 34.1. 

41 

4. Approval-policy configuration mechanism (34.1) — who sets EXECUTIVE_REQUIRED per organization, and when. 

5. Licensing Renewal/Suspension enforcement behavior (34.3) — what actually happens operationally on suspension. 

6. Billing rate/pricing model (34.7) — data source is now defined; the formula is not. 

7. CSO Dashboard content vs. existing Overview page (see AFD §16 equivalent) — not yet differentiated. 

## **End of TRD** 

42 

