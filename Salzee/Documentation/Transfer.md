# Transfer Module

## Overview

The Transfer module handles ownership transfers of properties from one person (seller) to another (buyer). It follows a multi-step workflow with approval gates and requires an active NDC before a transfer can be initiated.

**Controller:** `TransferController.cs`
**Model:** `Transfer.cs`
**Views:** `Views/Transfer/`

---

## Database Table: `Transfer`

| Column | Type | Notes |
|--------|------|-------|
| TransferID | CHAR(50) PK | Auto-generated: `TRF-YYYYMMDD-NNNN` |
| CustomerID | CHAR(10) FK | The existing customer (seller side) |
| WorkFlowStatus | NVARCHAR(100) | See Workflow section below |
| SellerName, SellerFatherName, SellerCNIC, SellerContact, SellerAddress | Seller info | Pre-filled from Customer record |
| BuyerName, BuyerFatherName, BuyerCNIC, BuyerPassportNo, BuyerContact, BuyerAddress, BuyerCity, BuyerCountry | Buyer info | Entered manually |
| SellerAttachments, BuyerAttachments | NVARCHAR(MAX) | JSON array of attachment refs |
| SellerBiometric, BuyerBiometric | NVARCHAR(MAX) | Fingerprint/biometric data (base64) |
| TransferFeeDue, TransferFeePaid | FLOAT | Fee must match when approving |
| PaymentDate, PaymentMode, PaymentChallanNo | Payment info | Required on creation |
| Details, CROComments, AccountsComments, TransferComments | NVARCHAR(MAX) | Free-text notes by different departments |
| CreatedAt | DATETIME | Auto-set |

---

## TransferID Generation

Format: `TRF-YYYYMMDD-NNNN`
- Prefix `TRF-` + today's date + `-` + 4-digit daily sequence.
- Example: `TRF-20260220-0001`, `TRF-20260220-0002`.

---

## Workflow

Transfers move through four statuses in order:

```
Created  -->  At the Desk of Accounts  -->  At the Desk of Transfer Approval  -->  Approved
```

| Status | Meaning |
|--------|---------|
| Created | Initial state; CRO fills in seller/buyer details and payment info |
| At the Desk of Accounts | Accounts team reviews payment and adds AccountsComments |
| At the Desk of Transfer Approval | Transfer authority reviews and adds TransferComments |
| Approved | Final; locks the transfer from further edits |

---

## Key Business Rules

1. **NDC Required:** A transfer can only be created if the customer has an active NDC (current date falls between `IssuedDate` and `NDCExpiryDate`).
2. **Fee Match on Approval:** When workflow moves to `Approved`, `TransferFeeDue` must equal `TransferFeePaid`. Otherwise the save is rejected.
3. **Approved = Locked:** Once approved, the transfer cannot be edited.
4. **Customer Record Updated on Approval:** When approved, the Customer record is overwritten with the buyer's details (name, father name, CNIC, phone, address, city, country) so the buyer becomes the new owner of record.
5. **Buyer CNIC Validation:** Buyer CNIC must match format `XXXXX-XXXXXXX-X`.

---

## Pages & Actions

| Action | Permission | Description |
|--------|-----------|-------------|
| `Index` | Read | List transfers with customer ID and workflow status filters |
| `Create` | Edit | New transfer form; seller info auto-populated from Customer |
| `Edit(id)` | Edit | Update transfer details; blocked if already Approved |
| `Details(id)` | Read | Full transfer detail view with workflow status |
| `TransferLetter(id)` | Read | Printable letter for approved transfers only |
| `TransferReceipt(id)` | Read | Printable receipt for non-approved (in-progress) transfers |
| `GetCustomerForTransfer` (AJAX) | Read | Fetches seller info + NDC status for a given customer ID |

---

## Attachments

- Uploaded via AJAX (`UploadAttachment`), stored under `wwwroot/uploads/transfers/{TransferID}/`.
- Each attachment is typed as either `Seller` or `Buyer`.
- Stored in the shared `Attachments` table with `RefType = "Transfer"`.
- Allowed: `.jpg`, `.jpeg`, `.png`, `.gif`, `.bmp`, `.pdf`. Max 8 MB.
- Deletion requires Admin permission.

---

## Config-Driven Dropdowns

- **Cities / Countries:** From `Configurations` table (`cities`, `countries` keys).
- **Payment Methods:** From `Configurations` table (`paymentmethods` key), fallback: DD/DS, Cash, Bank Transfer, Cheque, Online, Mobile Money.
