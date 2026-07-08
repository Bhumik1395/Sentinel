<div align="center">

# 🛡️ Sentinel

### Modern AI-Assisted Endpoint Detection & Response (EDR/XDR)

*Detect. Investigate. Respond.*

---

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/)
[![React](https://img.shields.io/badge/React-Frontend-61DAFB?style=for-the-badge&logo=react)](https://react.dev/)
[![Supabase](https://img.shields.io/badge/Supabase-PostgreSQL-3ECF8E?style=for-the-badge&logo=supabase)](https://supabase.com/)
[![Redis](https://img.shields.io/badge/Redis-Queue-DC382D?style=for-the-badge&logo=redis)](https://redis.io/)
[![Neo4j](https://img.shields.io/badge/Neo4j-Attack_Graph-4581C3?style=for-the-badge&logo=neo4j)](https://neo4j.com/)
[![Keycloak](https://img.shields.io/badge/Keycloak-Identity-4D4D4D?style=for-the-badge)](https://www.keycloak.org/)
[![License](https://img.shields.io/github/license/Bhumik1395/Sentinel?style=for-the-badge)](LICENSE)

</div>

---

# Overview

Sentinel is a next-generation Endpoint Detection & Response (EDR/XDR) platform designed to help organizations detect, investigate and respond to cyber threats from a unified security console.

Rather than relying solely on signature-based detections, Sentinel combines:

- Behavioral Detection
- Sigma Rule Engine
- IOC Matching
- MITRE ATT&CK Mapping
- AI-assisted Anomaly Detection
- Attack Graph Visualization
- Automated SOAR Actions

into one integrated platform.

---

# Architecture

```
                  Security Dashboard
                         │
                         ▼
                ASP.NET Core API
                         │
 ┌──────────────┬───────────────┬──────────────┐
 ▼              ▼               ▼              ▼
Keycloak   Supabase DB       Neo4j         Redis
   │             │               │             │
   └─────────────┴───────────────┴─────────────┘
                         │
                         ▼
                Sentinel Windows Agent
                         │
                         ▼
              Windows Endpoint Telemetry
```

---

# Features

## Endpoint Security

- Endpoint Registration
- Endpoint Health Monitoring
- Policy Management
- Agent Heartbeats
- Endpoint Isolation

---

## Threat Detection

- Behavioral Detection Engine
- Sigma Rule Engine
- IOC Detection
- AI-assisted Anomaly Detection
- MITRE ATT&CK Mapping
- Threat Correlation

---

## Investigation

- Incident Management
- Alert Correlation
- Attack Timeline
- Attack Graph Visualization
- IOC Management
- Security Reports

---

## SOAR

- Kill Process
- Endpoint Isolation
- Incident Creation
- Organization Approval Workflow
- Audit Logging

---

## Identity & Access

- Keycloak Authentication
- JWT Authorization
- RBAC
- Multi-Organization Isolation
- Organization-based Access Control

---

# Technology Stack

| Layer | Technology |
|--------|------------|
| Backend | ASP.NET Core (.NET 8) |
| Frontend | React + TypeScript |
| Authentication | Keycloak |
| Database | Supabase PostgreSQL |
| Graph Database | Neo4j |
| Cache / Queue | Redis |
| Endpoint Agent | C# Windows Service |
| Containerization | Docker |
| CI/CD | GitHub Actions |

---

# Repository Structure

```
Sentinel
│
├── backend/
│   ├── src/
│   ├── migrations/
│   ├── keycloak/
│   └── docker-compose.yml
│
├── frontend/
│
├── database/
│
├── docs/
│
└── .github/
```

---

# Development Status

| Component | Status |
|-----------|--------|
| Project Architecture | ✅ |
| Database Schema | ✅ |
| Authentication | ✅ |
| Infrastructure | ✅ |
| Backend API | 🚧 |
| Windows Agent | 🚧 |
| Detection Engine | 🚧 |
| Dashboard | 🚧 |
| SOAR | 🚧 |

---

# Security Model

Sentinel separates platform administration from customer organizations.

### Sentinel Company

- Owner
- Support Team

### Customer Organization

- CSO
- Security Administrator
- Security Analyst

Every organization operates in complete isolation while sharing the same platform infrastructure.

---

# Roadmap

- [x] System Architecture
- [x] Database Design
- [x] Authentication Infrastructure
- [x] Docker Environment
- [x] CI/CD Pipeline
- [ ] Backend APIs
- [ ] Windows Endpoint Agent
- [ ] Detection Engine
- [ ] Alert Correlation
- [ ] Incident Response
- [ ] Attack Graph Visualization
- [ ] SOAR Automation
- [ ] Production Deployment

---

# Vision

> Build a security platform that empowers organizations to detect sophisticated attacks, investigate incidents efficiently, and respond with confidence.

Sentinel is being designed with scalability, security, and extensibility at its core, enabling organizations of all sizes to manage endpoint security through a unified platform.

---

<div align="center">

### 🚧 Sentinel is currently under active development.

Built with ❤️ by **Bhumik Joshi**

</div>