# Properties Module

## Overview

Manages the inventory of physical properties (plots, apartments, shops, etc.) within projects. Properties can be created individually or bulk-imported from Excel/CSV. They move through statuses as they are allotted, rented, or sold. The module also handles dealer assignment, allotment, and bulk dealer transfers.

**Controller:** `PropertyController.cs`
**Model:** `Property.cs`
**Views:** `Views/Property/`

---

## Database Table: `Property`

| Column | Type | Notes |
|--------|------|-------|
| PropertyID | CHAR(10) PK | Random GUID-based 10-char ID |
| ProjectID | CHAR(10) FK | Which project this property belongs to |
| PlotNo | NVARCHAR(50) | Plot/unit number |
| Street | NVARCHAR(50) | Street name |
| PlotType | NVARCHAR(50) | Residential, Commercial, Industrial, Agricultural (config-driven) |
| Block | NVARCHAR(50) | Block identifier |
| Floor | NVARCHAR(50) | Floor (for apartments) |
| PropertyType | NVARCHAR(50) | e.g. Plot, Apartment, Shop (project-specific) |
| Size | NVARCHAR(50) | e.g. 5 Marla, 10 Marla (project-specific) |
| Status | NVARCHAR(50) | `Available`, `Allotted`, `Sold`, `Rented` |
| DealerID | INT FK (nullable) | Dealer who owns/manages the inventory |
| AdditionalInfo | NVARCHAR(MAX) | Free-text notes |
| CreatedAt | DATETIME | Auto-set |

**Related Collections:** Allotments, Possessions, PropertyLogs, Rentals.

---

## Property Statuses

| Status | Set By |
|--------|--------|
| Available | Default on creation / when rental is closed |
| Allotted | When allotted to a customer via `Allot` action |
| Sold | When assigned to a customer during Customer Create/Edit |
| Rented | When a rental agreement is created |

---

## Allotment (from Property side)

The `Allot` action creates an `Allotment` record linking a property to a customer:
- Sets `WorkFlowStatus = "Pending"` and `AllottmentType` (e.g. Full, Partial).
- Updates property status to `Allotted`.
- Logged in ActivityLog.

---

## Bulk Import (Excel / CSV)

1. **Import page:** User uploads an `.xlsx`, `.xls`, or `.csv` file and selects a Dealer.
2. **Preview:** File is parsed; each row is validated (ProjectID must exist, PlotNo required). Invalid rows are flagged.
3. **Confirm:** Valid rows are inserted as new properties with `Status = "Available"`. Duplicates (same ProjectID + PlotNo + Block) are skipped.
4. **Excel columns:** ProjectID, PlotNo, Street, PlotType, Block, PropertyType, Size, Floor, AdditionalInfo.

---

## Reserve / Bulk Dealer Change

The **Reserve** page lists all `Available` properties (optionally filtered by project). From here, staff can select multiple properties and change their dealer assignment in bulk. Each change creates a `PropertyLog` entry recording old and new dealer.

---

## Pages & Actions

| Action | Permission | Description |
|--------|-----------|-------------|
| `Index` | Read | List all properties with project, allotment, and customer info |
| `Details(id)` | - | Full property details including allotments and possessions |
| `Create` | Edit | New property form with config-driven dropdowns |
| `Edit(id)` | Edit | Update property details |
| `Delete(id)` | Admin | Remove a property |
| `Allot(id)` | - | Allot property to a customer |
| `Import` | - | Upload Excel/CSV for bulk property creation |
| `ImportPreview` | - | Preview and validate import data |
| `ImportConfirm` | - | Confirm and save valid import rows |
| `Reserve` | - | List available properties for dealer management |
| `BulkChangeDealer` | - | Reassign dealer for selected properties |
| `GetProjectDetails` (AJAX) | - | Returns sizes and property types for a project |

---

## Config-Driven Dropdowns

- **PlotType:** From `Configurations` table key `PlotTypes`. Fallback: Residential, Commercial, Industrial, Agricultural.
- **PropertyType:** From `Configurations` key `propertytypes`, or from `Project.PropertyTypes` (project-specific).
- **Size:** From `Project.Sizes` (project-specific, loaded via AJAX).
