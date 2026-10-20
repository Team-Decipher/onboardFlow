# onboardFlow REST API Specification

All API endpoints are prefixed with `/api`. Protected routes require a Bearer token:
`Authorization: Bearer <JWT>`

## 1. Authentication (`/api/auth`)
- `POST /api/auth/login`: Authenticate user and issue JWT token.
- `POST /api/auth/register`: Create user account (Admin only).
- `GET /api/auth/me`: Fetch current authenticated identity and permissions.

## 2. Cases (`/api/cases`)
- `GET /api/cases`: List onboarding cases (filterable by status, client type, and owner).
- `POST /api/cases`: Create a new onboarding case with template checklists.
- `GET /api/cases/{id}`: Retrieve detailed case status, completion percentage, and checklist items.
- `PUT /api/cases/{id}/status`: Update case state (e.g., InProgress, UnderReview, Completed).

## 3. Checklists (`/api/checklists`)
- `GET /api/checklists/templates`: Retrieve default checklist templates per client tier (Enterprise, SME, Partner).
- `POST /api/cases/{caseId}/items`: Append a custom checklist requirement to an active case.
- `PUT /api/checklists/{itemId}`: Update checklist item parameters, reviewer assignment, or status.

## 4. Documents (`/api/documents`)
- `POST /api/documents/upload`: Upload document file (multipart/form-data) linked to a checklist item.
- `GET /api/documents/{id}/download`: Generate SAS link or stream file from Azure Blob Storage.
- `POST /api/documents/{id}/review`: Submit approval or rejection with mandatory reviewer feedback.
- `GET /api/documents/{id}/versions`: Retrieve historic document versions.

## 5. Audit & Compliance (`/api/audit`)
- `GET /api/audit`: Query tamper-evident event log with timestamp, user, and action filters.
