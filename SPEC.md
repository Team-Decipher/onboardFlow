# SPEC.md — Checklist Builder (US-13, US-18, US-19)

Project: Client Onboarding & Document Workflow Portal (`Team-Decipher/onboardFlow`)  
Scope of this spec: the **Checklists module** (`backend/src/OnboardFlow.Api/Modules/Checklists` and `frontend/app/admin/checklists`). Do not change other modules.

---

## 0. Instructions for the coding agent (read first)

1. Read this whole file. **Do not write code yet.** First reply with an implementation plan for the ONE user story named by the user, and wait for approval.
2. Work on **one branch / one user story at a time**. Never mix stories in one branch.
3. **Never commit or push to `main`.** All work branches off `dev` and is merged back into `dev` through a pull request (see section 2).
4. Follow the codebase's existing modular design pattern (`OnboardFlow.Api/Modules/Checklists`). Reuse existing entities, repositories, and services without introducing conflicting architectures.
5. After each step: build, run the tests, verify the result, then commit.
6. If something in this spec is unclear or conflicts with the code, ask before implementing. Do not guess.

---

## 1. What we are building & Codebase Context

Administrators define a **document checklist template for a client type**. Each template has **checklist items** (the documents a client must provide). For each item, an Administrator or Onboarding Manager can set a **default reviewer** and a **due time limit**.

| Ticket | Story | Summary | Status in Repo |
|---|---|---|---|
| **CODWP-11** | **US-13 Create Checklist** | Admin creates a checklist template for a client type | **To be implemented** (Branch 1) |
| **CODWP-15** | **US-18 Assign Reviewer** | Admin or Onboarding Manager assigns a default reviewer to a checklist item | **To be implemented** (Branch 2) |
| **CODWP-16** | **US-19 Set Due Date** | Admin sets due days / due date on a checklist item | **To be implemented** (Branch 3) |

### Current Repository State (Already Merged into `dev`)

- **US-14 (Add Checklist Item) & US-15 (Remove Checklist Item)** are already implemented and merged into `dev` (PR #6, commit `64de99e`).
- The database schema for checklist templates and items already exists via migration `20261116164758_AddChecklistItems`.
- `ChecklistTemplate` and `ChecklistItem` entities, repository (`IChecklistItemRepository`), service (`IChecklistItemService`), and controller (`ChecklistItemsController`) already exist.
- Frontend item management UI already exists at `frontend/app/admin/checklist-templates/[id]/page.tsx`.
- Client types (`Enterprise`, `SME`, `Partner`) already exist and link to templates via `ClientType.DefaultChecklistTemplateId`.

### Key Decisions

1. **Reviewer Assignment (US-18):** Can be performed by **both Administrator and Onboarding Manager** (per SRS Section 3.2 and decision log).
2. **Reviewer Role:** A reviewer must be an existing user assigned the `Reviewer` role (`AppRoles.Reviewer`).
3. **Due Time Limits (US-19):** An item supports **both** `dueDays` (rule: N days after case creation, 1–365) and `dueDate` (a specific target calendar date).
4. **Overdue Flag (US-19):** Computed dynamically. When `dueDate` is set and today UTC is past `dueDate`, `isOverdue` evaluates to `true`.
5. **Database Changes:** 
   - US-13 and US-18 require **no new migrations** (existing tables `ChecklistTemplates` and `ChecklistItems` are used).
   - US-19 requires a single migration (`AddDueDaysToChecklistItem`) to add the `DueDays` integer column to `ChecklistItems`.

---

## 2. Git workflow

Base branch for all work: **`dev`**. Never target `main`.

```
main        (protected, do not touch)
 └── dev    (integration branch, all PRs go here)
      ├── feature/CODWP-11-us13-create-checklist
      ├── feature/CODWP-15-us18-assign-reviewer
      └── feature/CODWP-16-us19-set-due-date
```

Branch creation:

```bash
git checkout dev
git pull origin dev
git checkout -b feature/CODWP-11-us13-create-checklist
```

Rules:
- **Execution order:** US-13 first -> then US-18 -> then US-19.
- After US-13 is merged into `dev`: rebase the next branch: `git checkout <branch> && git fetch && git rebase origin/dev`.
- Commit messages: `CODWP-11: add checklist template creation endpoint` (Jira key first, imperative mood, atomic commits).
- Push branch and open PR targeting **`dev`**: `git push -u origin feature/CODWP-11-us13-create-checklist`.
- Never force-push to `dev` or `main`. Never commit secrets (`.env`, credentials, JWT keys).

---

## 3. Architecture

### 3.1 Three-tier Modular Monolith

The codebase is organized as a modular monolith within `backend/src/OnboardFlow.Api`:

| Tier | Path | Responsibility |
|---|---|---|
| **Presentation** | `Controllers/ChecklistTemplatesController.cs`<br>`Controllers/ChecklistItemsController.cs`<br>`frontend/app/admin/checklists/` | HTTP routing, request/response models, input validation, Next.js UI views and modals. |
| **Application** | `Modules/Checklists/Services/`<br>`Modules/Checklists/DTOs/` | Use cases, business validation, template calculations, audit trail dispatching. |
| **Data / Persistence** | `Data/AppDbContext.cs`<br>`Modules/Checklists/Repositories/`<br>`Modules/Checklists/Domain/` | EF Core mapping (PostgreSQL), database queries, entity definitions, migrations. |

### 3.2 Cross-Module Boundaries

1. **Checklists <-> Users & Access:** Check user existence and role (`AppRoles.Reviewer`) using `AppDbContext.Users` or `IUserDirectory` abstraction. Reference reviewers by `Guid AssignedReviewerId`.
2. **Checklists <-> Client Types:** Verify client type existence and link default templates via `ClientType.DefaultChecklistTemplateId`.
3. **Audit Trail:** Record state changes using `AppDbContext.AuditLogs` or audit writer (actions: `ChecklistTemplateCreated`, `ChecklistItemReviewerAssigned`, `ChecklistItemDueUpdated`).

---

## 4. Data Model (Schema: `public`)

### `ChecklistTemplates` Table

| Property / Column | Type | Constraints / Details |
|---|---|---|
| `Id` | `uuid` | Primary Key (`Guid.NewGuid()`) |
| `Name` | `varchar(150)` | Not Null, trimmed |
| `Version` | `varchar(20)` | Not Null, default `"v1"` |
| `Description` | `varchar(500)` | Nullable |
| `RequiredCount` | `int` | Count of mandatory items (`default: 0`) |
| `OptionalCount` | `int` | Count of optional items (`default: 0`) |
| `CreatedAtUtc` | `timestamptz` | UTC timestamp |
| `CreatedBy` | `varchar(100)` | Username or email of creator |
| `ClientTypes` | Navigation | 1-to-many relationship with `ClientType.DefaultChecklistTemplateId` |
| `Items` | Navigation | 1-to-many relationship with `ChecklistItem` (cascade delete) |

### `ChecklistItems` Table

| Property / Column | Type | Constraints / Details |
|---|---|---|
| `Id` | `uuid` | Primary Key |
| `ChecklistTemplateId` | `uuid` | Foreign Key -> `ChecklistTemplates.Id` (Cascade delete) |
| `DocumentName` | `varchar(256)` | Not Null, document label |
| `IsRequired` | `bool` | Default `true` |
| `OrderIndex` | `int` | Sort position (1-based index) |
| `AssignedReviewerId` | `uuid` | Nullable, FK -> `Users.Id` (Restrict/SetNull) |
| `DueDays` | `int` | Nullable, 1–365 days after case inception (**Added in US-19 migration**) |
| `DueDate` | `timestamptz` | Nullable, specific fixed calendar deadline |
| `CreatedAtUtc` | `timestamptz` | UTC timestamp |
| `CreatedBy` | `varchar(100)` | Creator identity |

---

## 5. Branch 1 — `feature/CODWP-11-us13-create-checklist`

**User Story:** As an administrator, I want to create a document checklist for a client type so that the required documents can be defined for that type of client.

### Backend Scope

- **Endpoint:** `POST /api/checklist-templates`
  - **Authorization:** `[Authorize(Roles = AppRoles.Administrator)]`
  - **Request Body (`CreateChecklistTemplateRequest`):**
    ```json
    {
      "name": "Enterprise High-Risk Verification",
      "clientTypeId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      "description": "Standard checklist for high-risk corporate entities",
      "version": "v1"
    }
    ```
  - **Responses:**
    - `201 Created`: Returns `ChecklistTemplateDetailDto` with location header `/api/checklist-templates/{id}`.
    - `400 Bad Request`: Validation failure (empty name, name < 3 or > 150 chars).
    - `401 Unauthorized / 403 Forbidden`: Unauthenticated or non-admin role.
    - `404 Not Found`: `clientTypeId` does not correspond to an existing `ClientType`.
    - `409 Conflict`: A checklist template with the same name already exists for this client type.
- **Endpoint Update:** `GET /api/checklist-templates?clientTypeId={guid?}`
  - Filter list by `clientTypeId` when query parameter is provided.
  - Accessible to `Administrator` and `OnboardingManager`.
- **Audit Log:** Dispatch audit entry `ChecklistTemplateCreated` with creator, timestamp, and template metadata.

### Frontend Scope

- **Template Listing & Management Page (`frontend/app/admin/checklists/page.tsx`):**
  - Replace hardcoded redirect with an administrative dashboard listing all templates.
  - Display cards/table showing: Template Name, Version, Associated Client Type, Required Items count, Optional Items count, Created Date.
  - Action buttons: "Open Builder" (links to `/admin/checklist-templates/[id]`).
- **"Create Checklist" Modal / Form:**
  - Client Type selector (fetches from `/api/client-types`).
  - Template Name input (3–150 characters, validation error feedback).
  - Description input (optional).
  - Submit triggers `POST /api/checklist-templates` and on success redirects to `/admin/checklist-templates/{newId}`.
- Match existing Figma dark-themed design tokens (`WorkspaceLayout`, slate-900 backgrounds, cyan/blue accent buttons).

### Definition of Done

- [ ] Admin can create a new checklist template via the UI and API.
- [ ] Non-admin roles receive `403 Forbidden` on creation attempt.
- [ ] Duplicate template names for the same client type return `409 Conflict`.
- [ ] Querying `/api/checklist-templates?clientTypeId=...` filters correctly.
- [ ] Audit log entry `ChecklistTemplateCreated` recorded in `AuditLogs`.
- [ ] Automated tests in `backend/tests/OnboardFlow.Api.Tests` pass.
- [ ] Pull request opened targeting `dev`.

---

## 6. Branch 2 — `feature/CODWP-15-us18-assign-reviewer`

Starts after US-13 is merged into `dev`.

**User Story:** As an administrator or onboarding manager, I want to assign a reviewer to a checklist item so that each document can be reviewed by the appropriate person.

### Backend Scope

- **Endpoint:** `PUT /api/checklist-templates/{templateId}/items/{itemId}/reviewer`
  - **Authorization:** `[Authorize(Roles = $"{AppRoles.Administrator},{AppRoles.OnboardingManager}")]`
  - **Request Body (`AssignReviewerDto`):**
    ```json
    {
      "reviewerId": "44444444-4444-4444-4444-444444444444"
    }
    ```
    *(Send `"reviewerId": null` to unassign / clear the reviewer).*
  - **Responses:**
    - `200 OK`: Returns updated `ChecklistItemDto` with `assignedReviewerId` and `assignedReviewerName`.
    - `400 Bad Request`: User specified in `reviewerId` is not in the `Reviewer` role or is deactivated.
    - `401 / 403`: Unauthorized or caller lacks Administrator/OnboardingManager role.
    - `404 Not Found`: Template or Item not found, or Item does not belong to Template.
- **Audit Log:** Dispatch audit entry `ChecklistItemReviewerAssigned` recording item name, old reviewer ID, and new reviewer ID.

### Frontend Scope

- In `frontend/app/admin/checklist-templates/[id]/page.tsx`:
  - Enhance the reviewer selection dropdown on each item row.
  - Options include `None / Unassigned` plus active reviewers fetched from `GET /api/checklist-templates/reviewers`.
  - Immediate auto-save or bulk save integration with success toast notification.
  - Visible and editable for `Administrator` and `OnboardingManager`; read-only for other roles.

### Definition of Done

- [ ] Both Administrator and OnboardingManager can assign and unassign reviewers.
- [ ] Attempting to assign a user who does not have the `Reviewer` role is rejected with `400 Bad Request`.
- [ ] Item must belong to the specified template.
- [ ] Audit log entry `ChecklistItemReviewerAssigned` written to `AuditLogs`.
- [ ] Automated unit and controller tests in `OnboardFlow.Api.Tests` pass.
- [ ] Pull request opened targeting `dev`.

---

## 7. Branch 3 — `feature/CODWP-16-us19-set-due-date`

Starts after US-13 is merged into `dev` (can proceed independently of US-18).

**User Story:** As an administrator, I want to set a due date for a checklist item so that document submission and review deadlines can be tracked.

### Database Migration

- Create migration `AddDueDaysToChecklistItem` adding `DueDays` (`int?`) to `ChecklistItems` table:
  ```bash
  dotnet ef migrations add AddDueDaysToChecklistItem --project backend/src/OnboardFlow.Api
  ```

### Backend Scope

- **Endpoint:** `PUT /api/checklist-templates/{templateId}/items/{itemId}/due`
  - **Authorization:** `[Authorize(Roles = AppRoles.Administrator)]`
  - **Request Body (`SetItemDueLimitDto`):**
    ```json
    {
      "dueDays": 7,
      "dueDate": "2026-12-31"
    }
    ```
    *(Both fields optional; send `null` to clear).*
  - **Validation Rules:**
    - `dueDays`: Integer between `1` and `365` (inclusive), or `null`.
    - `dueDate`: Must not be in the past relative to UTC today, or `null`.
  - **Responses:**
    - `200 OK`: Returns updated `ChecklistItemDto` including computed `isOverdue: boolean`.
    - `400 Bad Request`: `dueDays` out of range (< 1 or > 365), or `dueDate` in the past.
    - `401 / 403`: Unauthorized or caller lacks Administrator role.
    - `404 Not Found`: Template or Item not found.
- **Overdue Computation Logic:**
  - `isOverdue` is `true` if `dueDate != null` and `dueDate.Value.Date < TimeProvider.GetUtcNow().Date`.
  - (For template items with only `dueDays`, `isOverdue` remains `false` until an active onboarding case instance instantiates a deadline).
- **Audit Log:** Dispatch audit entry `ChecklistItemDueUpdated`.

### Frontend Scope

- In `frontend/app/admin/checklist-templates/[id]/page.tsx`:
  - Provide input for `dueDays` ("Due in N days after case start") alongside the `dueDate` date-picker.
  - Display an **Overdue** warning badge if `isOverdue` is true or if target date has passed.
  - Clear validation feedback when entering invalid days or past dates.

### Definition of Done

- [ ] EF migration `AddDueDaysToChecklistItem` generated and applies cleanly.
- [ ] Admin can update due days, due date, or both.
- [ ] Validation rejects 0 days, > 365 days, and past calendar dates with descriptive 400 errors.
- [ ] `isOverdue` evaluated using testable `TimeProvider`.
- [ ] Audit log entry `ChecklistItemDueUpdated` recorded.
- [ ] Unit and controller tests pass in `OnboardFlow.Api.Tests`.
- [ ] Pull request opened targeting `dev`.

---

## 8. Shared Conventions & Testing Strategy

- **C# / .NET Conventions:** `PascalCase` properties, dependency injection via ASP.NET Core service container, lightweight repository pattern or direct `AppDbContext` usage consistent with existing modules.
- **API Boundary:** Strict DTO usage; EF Core entities are never returned directly from controllers.
- **Roles:** Defined in `OnboardFlow.Api.Common.Authorization.AppRoles`:
  - `AppRoles.Administrator` (`"Administrator"`)
  - `AppRoles.OnboardingManager` (`"OnboardingManager"`)
  - `AppRoles.Reviewer` (`"Reviewer"`)
  - `AppRoles.Client` (`"Client"`)
- **Backend Tests:** Located in `backend/tests/OnboardFlow.Api.Tests`:
  - Uses `Microsoft.EntityFrameworkCore.InMemory` for isolated context testing.
  - Test role authorization attributes and status codes (`200`, `201`, `400`, `403`, `404`, `409`).
- **Frontend:** Next.js 16 App Router, TypeScript, Tailwind CSS 4, React 19.

---

## 9. Next Steps for Coding Agent

Per instruction **0.1**:
1. Do not start coding or editing solution files immediately.
2. Present this specification alignment and wait for the user to confirm and name the user story to start (e.g. `US-13`).
3. Once approved, create the designated feature branch off `dev` and proceed step by step.
