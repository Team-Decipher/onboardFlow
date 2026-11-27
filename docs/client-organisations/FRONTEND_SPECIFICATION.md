# 🎨 Frontend Specification: Client Organisations

This document specifies the Presentation Tier design and user interface implementation for the **Client Organisations** module, covering **Screen 10 (Client organisations)** and **Screen 11 (Organisation details)**.

---

## 1. UI Wireframe & Design System

The frontend interface strictly mirrors the official project mockups:

### Color Palette
| Token | Hex / Value | Usage |
| :--- | :--- | :--- |
| `Sidebar Background` | `#0d282f` / `#0c2328` | Left vertical navigation bar |
| `Primary Brand Teal` | `#007a78` | Active navigation pill, primary action buttons, focus rings |
| `Primary Hover Teal` | `#006866` / `#005f5d` | Hover states for primary buttons |
| `Page Background` | `#f8fafc` | Main screen container background |
| `Card & Table Background` | `#ffffff` | Elevated surface containers with `border-slate-200/90` |
| `Text Primary` | `#0f172a` / `slate-900` | Section headings, table rows, prominent labels |
| `Text Secondary` | `#64748b` / `slate-500` | Subtitles, helper text, breadcrumbs |
| `Avatar Badge` | `#e8f5f4` (bg), `#007a78` (text) | Circular user avatar (`KJ`) |

---

## 2. Screen 10: Client Organisations (`/organisations`)

### 2.1 Layout Hierarchy
```
+-----------------------------------------------------------------------------------------+
| Sidebar (OnboardFlow WORKSPACE) | Top Navbar (Workspace / Client organisations | KJ)   |
|                                 |-------------------------------------------------------|
| - [Active] Client organisations | Client organisations                                  |
| - Client types                  | Maintain client records and their onboarding contacts.|
| - Checklist templates           |                                     [+ Add organisation]|
| - Approval workflows            |-------------------------------------------------------|
| - User accounts                 | [Search organisations] [Client type: All ▾] [Status:▾]|
| - Reviewer workload             |-------------------------------------------------------|
| - Audit log                     | Table: ORGANISATION | CLIENT TYPE | CONTACT | CASES   |
| - System settings               | Ceylon Exports      | Enterprise  | Nimali  | 1 active|
|                                 | Lanka Foods         | SME         | Dilan   | 2 active|
| Need a hand?                    | Oceanic Partners    | Partner     | Shanika | 1 active|
| System administrator (Exit)     | Lotus Digital       | SME         | Ruwan   | Complete|
+-----------------------------------------------------------------------------------------+
```

### 2.2 Functional Elements
- **Page Title**: `Client organisations` (font size `text-2xl font-bold tracking-tight text-slate-900`).
- **Subtitle**: `Maintain client records and their onboarding contacts.` (`text-sm text-slate-500 mt-1`).
- **Action Button**: `+ Add organisation` (`bg-[#007a78] hover:bg-[#006866] text-white px-4 py-2 rounded-lg text-sm font-medium`).
- **Filter Toolbar**:
  - `Search organisations` input with live substring matching across name, code, city, contact, and BRN.
  - `Client type` dropdown selector: `All`, `Enterprise`, `SME`, `Partner`.
  - `Status` dropdown selector: `Active`, `All`, `Inactive`.
- **Organisations Table**:
  - Header row: Small uppercase labels `ORGANISATION`, `CLIENT TYPE`, `PRIMARY CONTACT`, `CASES`.
  - Primary Column: Displays Company Name in `text-slate-900 font-medium` and subtext `{clientCode} · {city}` in `text-xs text-slate-400`.
  - Action Column: `Edit →` link navigating directly to `/organisations/{id}` or `/organisations/{clientCode}`.
- **Add Organisation Modal**:
  - Controlled popup dialog for adding a new client record.
  - Automatically calculates the next client code (e.g., `CLI-0019`).
  - Sends `POST /api/organisations` and immediately injects the result into the table.

---

## 3. Screen 11: Organisation Details (`/organisations/[id]`)

### 3.1 Layout Hierarchy
```
+-----------------------------------------------------------------------------------------+
| Sidebar                         | Top Navbar (Workspace / Organisation details | KJ)    |
|                                 |-------------------------------------------------------|
|                                 | Organisation details                                  |
|                                 | Ceylon Exports (Pvt) Ltd / CLI-0018                   |
|                                 |-------------------------------------------------------|
|                                 | [Organisation details]  [Linked cases]  [Client access]|
|                                 |=======================================================|
|                                 | Card: Organisation information                        |
|                                 | Organisation name *       Client type *               |
|                                 | [Ceylon Exports (Pvt) Ltd] [Enterprise ▾]             |
|                                 | Contact name *            Contact email *             |
|                                 | [Nimali Perera]           [nimali@example.com]        |
|                                 | Phone number              Business reg number         |
|                                 | [+94 11 234 5678]         [PV 123456]                 |
|                                 | Business address                                      |
|                                 | [42 Marine Drive, Colombo 03]                         |
|                                 |                                                       |
|                                 | [ Cancel ]   [ Save changes ]                         |
+-----------------------------------------------------------------------------------------+
```

### 3.2 Tab System
1. **Tab 1: `Organisation details`**:
   - Displays core organizational information form.
   - Form fields:
     - `Organisation name *` (Required)
     - `Client type *` (Dropdown: `Enterprise`, `SME`, `Partner`)
     - `Contact name *` (Required)
     - `Contact email *` (Required, email validated)
     - `Phone number`
     - `Business registration number`
     - `Business address`
   - Actions:
     - `Cancel`: Navigates back to `/organisations`.
     - `Save changes`: Submits `PUT /api/organisations/{id}`, updates backend state, and displays a toast notification.

2. **Tab 2: `Linked cases`**:
   - Lists onboarding workflow cases linked to this organisation.
   - Displays case identifier (e.g. `OBF-2026-018`), case title, assigned reviewer, target completion SLA date, and a visual checklist completion progress bar.
   - Direct link to open the case in the workflow portal.

3. **Tab 3: `Client access`**:
   - Manages client portal credentials for self-service document upload.
   - Displays authorized contacts, roles (`Primary Client Admin`, `Document Submitter`), and last login timestamps.
   - Provides quick actions: `Reset login` and `+ Invite contact`.

---

## 4. Toast Notification & Feedback System

- Fixed position toast at `top-5 right-5` with `#007a78` background and slide-in animation.
- Displays feedback on operations:
  - *"Organisation details updated successfully."*
  - *"Organisation added successfully."*
  - *"Changes saved (local offline mode)."*
  - *"Portal invitation sent."*
