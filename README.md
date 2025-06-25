# Azure Platform Support Workflow Management System

## Overview

This repository contains the Azure Platform Support (APS) Workflow Management System (WMS) - a centralized ticketing system designed to streamline and standardize the handling of Azure Monitor alerts and internal support requests for platform engineering teams.

## Purpose & Background

The APS team needs a **single, low-friction system** to capture two primary work types:

1. **Azure Monitor alerts** that require engineering intervention.
2. **Ad-hoc internal requests** (access, new resources, standard changes).

Current ad-hoc channels (email, Teams chat, spreadsheets) lack SLA tracking, ownership visibility, and audit trail. This system centralizes ticket intake, triage, assignment, resolution, and reporting while remaining lightweight to enable rapid iteration.

## Key Features

- **Multiple Intake Channels**
  - Azure Monitor alerts via webhook
  - Teams bot (/ticket command)
  - Web portal form

- **Complete Ticket Lifecycle**
  - Create → Triage → Assign → Work → Resolve → Close

- **SLA Management & Monitoring**
  - Configurable SLA engine per priority and category
  - Visual indicators for imminent SLA breaches

- **Role-Based Experience**
  - Kanban board for engineers
  - SLA dashboards for team leads
  - Comprehensive audit trail

- **Attachment Support**
  - File storage up to 100 MB in Azure Blob Storage

- **Dual Environment Architecture**
  - Development and Production environments
  - Same IaC with parameter overrides

## System Architecture

The system is built on Azure Container Apps with PostgreSQL for data storage, leveraging API Management for routing and security. Both Dev and Prod environments share identical IaC with parameter overrides.

**Core Components:**

- Azure Container Apps (B1 tier, auto-scale 1→3 replicas)
- PostgreSQL (Basic tier, 2 vCores, 10 GB storage)
- Azure API Management (Developer tier)
- Azure Storage Account (Standard LRS for attachments)
- Application Insights (Basic tier for monitoring)
- Azure Key Vault (for secrets management)

## Technology Stack

- **Backend**: API built with modern web frameworks
- **Frontend**: React (TypeScript) + Fluent UI
- **Authentication**: Entra ID SSO
- **Infrastructure**: Bicep templates for IaC
- **CI/CD**: GitHub Actions workflows

## Project Timeline

The MVP development is planned for a 7-week timeline:

| Phase       | Duration | Milestone                                         |
| ----------- | -------- | ------------------------------------------------- |
| Sprint 0    | 1 wk     | Repo, CI/CD, IaC baseline; Dev & Prod provisioned |
| Sprints 1-2 | 2 wks    | API skeleton, DB schema, alert endpoint           |
| Sprints 3-4 | 2 wks    | Teams bot MVP, Kanban UI basic                    |
| Sprint 5    | 1 wk     | SLA engine + countdown UI                         |
| Sprint 6    | 1 wk     | Reporting page, CSV export; Prod pilot            |

## Success Metrics

| Objective                  | KPI                                             | Target             |
| -------------------------- | ----------------------------------------------- | ------------------ |
| **Reduce MTTA**            | Avg. time from ticket creation → acknowledgment | ≤ 10 min           |
| **Reduce MTTR**            | Avg. ticket resolution duration                 | 25% ↓ vs. baseline |
| **Improve SLA Compliance** | % tickets resolved within SLA                   | ≥ 95%              |
| **Increase Transparency**  | % tickets visible in real-time board            | 100%               |

## Repository Structure

- `/docs` - Documentation including PRD and other project documents
- `/infra` - Bicep templates for infrastructure deployment
- `/src` - Source code for the application
  - `/api` - Backend API services
  - `/ui` - Frontend React application
  - `/bot` - Teams bot implementation

## Getting Started

_Coming soon_

## License

_Coming soon_
