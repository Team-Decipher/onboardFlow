# System Architecture & Technical Design

## Overview
**onboardFlow** is designed around a Clean Architecture / Onion Architecture pattern for the backend, and a modern component-driven Next.js App Router frontend.

```
┌────────────────────────────────────────────────────────┐
│                   Next.js Frontend                     │
│         (Client Portal & Admin/Staff Dashboards)       │
└───────────────────────────┬────────────────────────────┘
                            │ HTTPS / REST / JWT
┌───────────────────────────▼────────────────────────────┐
│                  ASP.NET Core Web API                  │
│       Controllers, Auth Filters & Custom Middleware     │
├────────────────────────────────────────────────────────┤
│                 Application Services                   │
│         Workflows, DTO Mappings & Business Rules       │
├────────────────────────────────────────────────────────┤
│                     Core Domain                        │
│          Entities, Enums & Service Interfaces          │
├────────────────────────────────────────────────────────┤
│                   Infrastructure                       │
│    EF Core (PostgreSQL)  │  Azure Blob  │   SendGrid    │
└────────────────────────────────────────────────────────┘
```

## Backend Clean Architecture
- **OnboardFlow.Core**: Zero dependencies. Contains core domain models (`Case`, `ChecklistItem`, `Document`, `User`, `AuditLog`) and contract interfaces (`IRepository`, `IBlobStorageService`).
- **OnboardFlow.Application**: Contains application business logic, DTOs, service implementations, and validation rules.
- **OnboardFlow.Infrastructure**: Handles persistence (Entity Framework Core), external cloud storage adapters (Azure Blob Storage), and third-party notification APIs (SendGrid).
- **OnboardFlow.API**: Entry point, Dependency Injection registration, Swagger OpenAPI configuration, and RESTful route controllers.

## Security & Compliance
- Role-Based Access Control (RBAC) enforced via JWT claim attributes.
- Multi-tier document review pipelines (Legal -> Finance -> Executive Approval).
- Full immutable audit logging for all mutations.
