# Customer Module

## Overview

The Customer module manages the full lifecycle of property buyers: creation, editing, property allotment, blocking/unblocking, attachments, and deletion. All actions are permission-gated per user via the `UserModulePermission` system.

**Controller:** `CustomerController.cs`  
**Model:** `Customer.cs`  
**Views:** `Views/Customer/`

---

## Registration Step (First Step in Onboarding)

The **Registration** module is the first step in the customer onboarding process. It captures initial interest before creating a full customer profile.

### Flow

1. **Create Registration** (`/Registration/Create`) — Lightweight form: Full Name, CNIC, Phone, Email; optionally Project and Size.
2. **Review** — Status: `Pending` → `Approved` or `Rejected`.
3. **Create Customer** — When creating a customer, optionally select an approved Registration to auto-fill name, CNIC, phone, email and link `RegID`.

### Integration with Customer Create

- On the Customer Create form, users can search or select a Registration ID.
- If a Registration is selected, `FullName`, `CNIC`, `Phone`, and `Email` are auto-filled.
- The `RegID` foreign key links the Customer to the Registration.
- **Customers can be created with or without Registration** — `RegID` is nullable.
- One Registration can link to multiple Customers (e.g., same person buying multiple plots).
- A Registration cannot be deleted if it has linked Customers.

---

## Database Table: `Customers`

| Column | Type | Notes |
|--------|------|-------|
| CustomerID | CHAR(10) PK | Auto-generated from project prefix + sequence |
| RegID | CHAR(10) FK | Links to Registration (optional) |
| PlanID | CHAR(10) FK | Links to PaymentPlan (required) |
| ProjectID | CHAR(10) FK | Links to Project (required) |
| FullName, FatherName, CNIC, PassportNo, DOB, Gender, Nationality | Personal info | CNIC format: `XXXXX-XXXXXXX-X` |
| Phone, Email, MailingAddress, PermanentAddress, City, Country | Contact info | |
| SubProject, RegisteredSize | Project-specific info | Values come from Configurations table |
| NomineeName, NomineeID, NomineeRelation | Nominee/kin info | |
| NomineeNICDocumentPath, NomineePicturePath | File paths | Stored under `wwwroot/uploads/customers/{id}/kin/` |
| DealerID | INT FK (nullable) | Optional link to Dealer |
| Status | NVARCHAR(50) | `Active`, `Blocked`, etc. |
| CreatedAt | DATETIME | Auto-set on creation |

**Related Collections:** CustomerLogs, Allotments, Possessions, Penalties, Waivers, Refunds, Transfers, NDCs.

---

## CustomerID Generation

- Uses the **project prefix** (e.g. `APT`, `1C`) from the `Projects.Prefix` column.
- Finds the highest existing sequence number for that prefix and increments by 1.
- Format: `{Prefix}{5-digit padded number}` e.g. `APT00103`.
- Falls back to a random 10-char ID if no project/prefix found.

---

## Key Business Rules

1. **Identity Validation:** Either a valid CNIC (`XXXXX-XXXXXXX-X`) or a Passport Number (min 5 chars) is required. Both are optional individually but at least one must be provided.
2. **Age Check:** DOB must be at least 16 years ago.
3. **Status:** Defaults to `Active` on creation. Can be changed to `Blocked` via the Customer Blocking feature.
4. **After Creation:** User is redirected to the Edit page so they can immediately upload attachments.

---

## Pages & Actions

| Action | Permission | Description |
|--------|-----------|-------------|
| `Index` | Read | List all customers with project/status/search filters |
| `ByProject` | Read | Group customers by project |
| `Reports` | Read | Customer reports page |
| `Details(id)` | Read | Full customer detail view |
| `AccountStatement(id)` | Read | Financial statement for a customer |
| `AllotmentLetter(id)` | Read | Generate allotment letter |
| `Create` | Edit | New customer form; auto-generates ID, optionally assigns property |
| `Edit(id)` | Edit | Update customer info and nominee documents |
| `Delete(id)` | Admin | Soft-deletes customer and cleans up allotments |
| `CustomerBlocking` | Edit | Block/unblock a customer with reason and attachment; dual-logged to BlockingLogs + ActivityLog |
| `UploadAttachment` | Edit | Upload documents (AJAX); stored under `wwwroot/uploads/customers/{id}/` |
| `DeleteAttachment` | Edit | Remove an uploaded attachment |

---

## Property Assignment

When creating or editing a customer, a property can be assigned via `selectedPropertyID`.  
- `AssignCustomerPropertyAsync` creates an `Allotment` record and marks the `Property.Status` as `Sold`.
- Available properties are fetched via AJAX endpoints: `GetAvailablePropertiesForCustomer` and `GetAvailablePropertiesByProject`, filtered by project, size, and dealer.

---

## File Uploads

- **Nominee documents** (NIC scan, picture): saved during Create/Edit under `wwwroot/uploads/customers/{CustomerID}/kin/`.
- **Attachments** (general): uploaded via AJAX to `wwwroot/uploads/customers/{CustomerID}/`.
- Allowed extensions: `.jpg`, `.jpeg`, `.png`, `.gif`, `.bmp`, `.pdf`. Max size: 8 MB.

---

## Dropdown / Config Values

City, Country, Size, SubProject, and Nationality dropdown options are pulled from the `Configurations` table (comma-separated values stored under keys like `cities`, `countries`, `sizes`, `subprojects`, `nationalities`).

---

## Activity Logging

All create, edit, delete, and blocking actions are logged to the `ActivityLog` table with `RefType = "Customer"` and the CustomerID as `RefID`.
