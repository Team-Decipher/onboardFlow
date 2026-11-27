# 🔌 REST API Specification: Client Organisations

This document specifies the Application Logic Tier (REST Web API) for the **Client Organisations** module, implemented via `OrganisationsController` in **ASP.NET Core**.

---

## 1. Base URLs & Common Headers

- **Local Development**: `http://localhost:5000/api/organisations`
- **Internal Docker Network**: `http://backend:8080/api/organisations`
- **Production / Ingress**: `https://api.onboardflow.example.com/api/organisations`

### Common Request Headers
| Header | Value | Description |
| :--- | :--- | :--- |
| `Content-Type` | `application/json` | Required for `POST` and `PUT` |
| `Accept` | `application/json` | Required for all calls |
| `Authorization` | `Bearer <JWT_TOKEN>` | Optional for internal development; required in production RBAC |

---

## 2. API Endpoints Summary

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `GET` | `/api/organisations` | Retrieve list of client organisations with search and filtering |
| `GET` | `/api/organisations/{id}` | Retrieve a single organisation by GUID or ClientCode (`CLI-0018`) |
| `POST` | `/api/organisations` | Create a new client organisation |
| `PUT` | `/api/organisations/{id}` | Update existing organisation details |
| `DELETE` | `/api/organisations/{id}` | Remove or archive a client organisation |
| `GET` | `/api/organisations/{id}/cases` | Retrieve linked onboarding cases for this organisation |
| `GET` | `/api/organisations/{id}/access` | Retrieve client portal user access logins |

---

## 3. Endpoint Details

### 3.1 `GET /api/organisations`
Retrieves a list of client organisations matching query criteria.

#### Query Parameters
- `search` *(string, optional)*: Substring search matching `Name`, `ClientCode`, `City`, `PrimaryContact`, `ContactEmail`, or `BusinessRegistrationNumber`.
- `clientType` *(string, optional)*: Filter by client tier (`"Enterprise"`, `"SME"`, `"Partner"`, or `"All"`).
- `status` *(string, optional)*: Filter by status (`"Active"`, `"Inactive"`, or `"All"`).

#### Success Response (`200 OK`)
```json
[
  {
    "id": "f1a23456-1111-2222-3333-000000000001",
    "clientCode": "CLI-0018",
    "name": "Ceylon Exports (Pvt) Ltd",
    "city": "Colombo",
    "clientType": "Enterprise",
    "primaryContact": "Nimali Perera",
    "contactEmail": "nimali@example.com",
    "phoneNumber": "+94 11 234 5678",
    "businessRegistrationNumber": "PV 123456",
    "businessAddress": "42 Marine Drive, Colombo 03",
    "activeCasesCount": 1,
    "casesSummary": "1 active",
    "status": "Active",
    "createdAtUtc": "2026-08-19T10:00:00Z",
    "updatedAtUtc": null,
    "linkedCases": [
      {
        "id": "c1a23456-1111-2222-3333-000000000001",
        "organisationId": "f1a23456-1111-2222-3333-000000000001",
        "caseNumber": "OBF-2026-018",
        "title": "Enterprise Statutory KYC & Compliance Onboarding",
        "status": "Active",
        "progressPercentage": 67,
        "reviewerName": "Kasun Jayawardena",
        "targetCompletionDate": "2026-12-05T00:00:00Z",
        "createdAtUtc": "2026-10-30T10:00:00Z"
      }
    ]
  }
]
```

---

### 3.2 `GET /api/organisations/{id}`
Retrieves a specific organisation. Supports either a GUID `id` or human-readable `ClientCode` (e.g., `CLI-0018`).

#### Success Response (`200 OK`)
Returns the complete `ClientOrganisation` object including the eager-loaded `linkedCases` array.

#### Error Response (`404 Not Found`)
```json
{
  "message": "Organisation with ID or Code 'CLI-9999' was not found."
}
```

---

### 3.3 `POST /api/organisations`
Registers a new client organisation.

#### Request Body Schema (`CreateOrganisationRequest`)
```json
{
  "clientCode": "CLI-0019",
  "name": "Lanka Spice Traders",
  "city": "Matale",
  "clientType": "SME",
  "primaryContact": "Sunil Weerasinghe",
  "contactEmail": "sunil@lankaspice.lk",
  "phoneNumber": "+94 66 222 3456",
  "businessRegistrationNumber": "PV 789456",
  "businessAddress": "12 Temple Road, Matale",
  "casesSummary": "1 active",
  "status": "Active"
}
```

#### Validation Rules
- `Name`: Required (non-empty).
- `PrimaryContact`: Required (non-empty).
- `ClientCode`: If omitted, backend automatically calculates the next sequence (e.g. `CLI-0019`).
- `ClientCode` uniqueness: Returns `409 Conflict` if the code already exists.

#### Success Response (`201 Created`)
Header: `Location: /api/organisations/{newGuid}`
Body: Complete created `ClientOrganisation` object.

---

### 3.4 `PUT /api/organisations/{id}`
Updates details of an existing organisation (Screen 11 "Save changes" action).

#### Request Body Schema (`UpdateOrganisationRequest`)
```json
{
  "name": "Ceylon Exports (Pvt) Ltd",
  "clientCode": "CLI-0018",
  "city": "Colombo",
  "clientType": "Enterprise",
  "primaryContact": "Nimali Perera",
  "contactEmail": "nimali@example.com",
  "phoneNumber": "+94 11 234 5678",
  "businessRegistrationNumber": "PV 123456",
  "businessAddress": "42 Marine Drive, Colombo 03",
  "casesSummary": "1 active",
  "status": "Active"
}
```

#### Success Response (`200 OK`)
Returns updated `ClientOrganisation` object with `updatedAtUtc` timestamp set.

---

### 3.5 `DELETE /api/organisations/{id}`
Removes or archives an organisation.

#### Success Response (`204 No Content`)

---

### 3.6 `GET /api/organisations/{id}/cases`
Retrieves all onboarding cases linked to the organisation (Screen 11 "Linked cases" tab).

#### Success Response (`200 OK`)
```json
[
  {
    "id": "case-018-uuid",
    "organisationId": "org-018-uuid",
    "caseNumber": "OBF-2026-018",
    "title": "Enterprise Statutory KYC & Compliance Onboarding",
    "status": "Active",
    "progressPercentage": 67,
    "reviewerName": "Kasun Jayawardena",
    "targetCompletionDate": "2026-12-05T00:00:00Z"
  }
]
```

---

### 3.7 `GET /api/organisations/{id}/access`
Retrieves authorized portal user logins for the client (Screen 11 "Client access" tab).

#### Success Response (`200 OK`)
```json
[
  {
    "id": "access-001-uuid",
    "fullName": "Nimali Perera",
    "email": "nimali@example.com",
    "role": "Primary Client Admin",
    "status": "Active",
    "lastLogin": "Today at 09:14 AM"
  },
  {
    "id": "access-002-uuid",
    "fullName": "Finance Department (Ceylon Exports (Pvt) Ltd)",
    "email": "billing@example.com",
    "role": "Document Submitter",
    "status": "Active",
    "lastLogin": "Yesterday at 04:30 PM"
  }
]
```
