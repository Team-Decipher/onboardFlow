# 🧪 Testing and Verification Guide: Client Organisations

This guide provides end-to-end testing procedures across all three tiers (**Database**, **Backend API**, and **Frontend UI**) for the **Client Organisations** feature.

---

## 1. Quick Verification Checklist

- [x] **Database Tier**: Tables `Organisations` and `LinkedCases` created, with unique constraint on `ClientCode` and cascade delete.
- [x] **Backend API**: All 7 endpoints responding with correct status codes (`200 OK`, `201 Created`, `204 No Content`, `400 Bad Request`, `404 Not Found`).
- [x] **Data Seeding**: Baseline organizations (`CLI-0018`, `CLI-0017`, `CLI-0016`, `CLI-0015`) auto-seeded on first launch.
- [x] **Frontend Screen 10**: Table displays 4 organizations with correct metadata, search filtering, client type filter, and "+ Add organisation" modal.
- [x] **Frontend Screen 11**: Displays Organization Information form with prefilled values, tab switcher (`Organisation details`, `Linked cases`, `Client access`), and functioning "Save changes" button with toast notification.
- [x] **Inter-page Navigation**: Seamless navigation between Client Organisations (`/organisations`) and User Accounts (`/users`).

---

## 2. Running the Services

### Option A: Local Development (Recommended for Testing)

#### 1. Start the Backend API
In PowerShell:
```powershell
cd "e:\4th sem\Software project\Implimentation\onboardFlow\backend\src\OnboardFlow.Api"
dotnet run --urls "http://localhost:5000"
```
*Expected log output:*
```
[Database] Seeded initial client organisations successfully.
Now listening on: http://localhost:5000
```

#### 2. Start the Frontend
In another PowerShell terminal:
```powershell
cd "e:\4th sem\Software project\Implimentation\onboardFlow\frontend"
npm run dev
```
*Expected log output:*
```
- Local: http://localhost:3000
```

---

### Option B: Docker Compose
```powershell
cd "e:\4th sem\Software project\Implimentation\onboardFlow"
docker compose up -d
```

---

## 3. Backend API Test Suite (curl / PowerShell)

### Test 1: Fetch all Client Organisations
```bash
curl -X GET "http://localhost:5000/api/organisations" -H "Accept: application/json"
```
*Verification Check:*
- HTTP Status `200 OK`.
- Array contains 4 items (`CLI-0018`, `CLI-0017`, `CLI-0016`, `CLI-0015`).

### Test 2: Search by Substring ("Ceylon")
```bash
curl -X GET "http://localhost:5000/api/organisations?search=Ceylon" -H "Accept: application/json"
```
*Verification Check:*
- HTTP Status `200 OK`.
- Returns only *Ceylon Exports (Pvt) Ltd*.

### Test 3: Filter by Client Tier ("SME")
```bash
curl -X GET "http://localhost:5000/api/organisations?clientType=SME" -H "Accept: application/json"
```
*Verification Check:*
- HTTP Status `200 OK`.
- Returns 2 records: *Lanka Foods* and *Lotus Digital*.

### Test 4: Fetch Single Organisation by Code (`CLI-0018`)
```bash
curl -X GET "http://localhost:5000/api/organisations/CLI-0018" -H "Accept: application/json"
```
*Verification Check:*
- HTTP Status `200 OK`.
- `clientCode` equals `"CLI-0018"`, `primaryContact` equals `"Nimali Perera"`.
- `linkedCases` contains at least 1 case (`OBF-2026-018`).

### Test 5: Create a New Organisation
```bash
curl -X POST "http://localhost:5000/api/organisations" \
  -H "Content-Type: application/json" \
  -d '{
    "clientCode": "CLI-0019",
    "name": "Kandy Tea Exports",
    "city": "Kandy",
    "clientType": "Enterprise",
    "primaryContact": "Sunil Gamage",
    "contactEmail": "sunil@kandytea.lk",
    "phoneNumber": "+94 81 555 1234",
    "businessRegistrationNumber": "PV 654987",
    "businessAddress": "24 Lake View, Kandy",
    "casesSummary": "1 active",
    "status": "Active"
  }'
```
*Verification Check:*
- HTTP Status `201 Created`.
- Response contains generated `id` and linked case.

### Test 6: Update Organisation Details (Screen 11 Action)
```bash
curl -X PUT "http://localhost:5000/api/organisations/CLI-0018" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Ceylon Exports (Pvt) Ltd",
    "clientCode": "CLI-0018",
    "city": "Colombo",
    "clientType": "Enterprise",
    "primaryContact": "Nimali Perera",
    "contactEmail": "nimali.updated@example.com",
    "phoneNumber": "+94 11 234 5678",
    "businessRegistrationNumber": "PV 123456",
    "businessAddress": "42 Marine Drive, Colombo 03",
    "casesSummary": "1 active",
    "status": "Active"
  }'
```
*Verification Check:*
- HTTP Status `200 OK`.
- `contactEmail` is updated to `"nimali.updated@example.com"`.
- `updatedAtUtc` is populated.

---

## 4. Frontend UI Verification Scenarios

| Scenario | Steps to Reproduce | Expected Result |
| :--- | :--- | :--- |
| **Verify Screen 10 View** | Navigate to `http://localhost:3000/organisations`. | Sidebar highlights "Client organisations". Table renders 4 rows matching the wireframe (`Ceylon Exports`, `Lanka Foods`, `Oceanic Partners`, `Lotus Digital`). |
| **Search Filter** | Type "Kandy" in "Search organisations" input. | Table instantly filters down to *Lanka Foods* (`CLI-0017 · Kandy`). Clear button resets list. |
| **Dropdown Filter** | Select "Partner" from "Client type: All ▾". | Table shows only *Oceanic Partners*. |
| **Add Organisation Modal** | Click "+ Add organisation", enter company details, click "Create organisation". | Modal closes; new organisation appears at the top of the table; toast notification appears. |
| **Navigate to Screen 11** | Click `Edit →` next to *Ceylon Exports (Pvt) Ltd*. | Browser navigates to `/organisations/CLI-0018` or `/organisations/{id}`. Subtitle reads `Ceylon Exports (Pvt) Ltd / CLI-0018`. |
| **Edit Organisation Details** | Modify phone number or address, click `Save changes`. | "Saving..." state toggles; green toast notification "Organisation details updated successfully" confirms save. |
| **Verify Linked Cases Tab** | Click the "Linked cases" tab. | Displays active case `OBF-2026-018` with 67% progress bar and reviewer *Kasun Jayawardena*. |
| **Verify Client Access Tab** | Click the "Client access" tab. | Displays portal access for *Nimali Perera* (`Primary Client Admin`) and reset password action. |
