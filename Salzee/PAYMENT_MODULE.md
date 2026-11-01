# Payment Module — Business Logic Documentation

> **Audience:** Developers maintaining or extending the PMS Payment module.  
> **Last reviewed:** Feb 2026  
> **Controller:** `PaymentController.cs`  
> **Key models:** `PaymentPlan`, `PaymentSchedule`, `Payment`, `Penalty`, `Waiver`

---

## 1. Core Concepts

The payment system is built around three layered entities:

```
Project
  └─ PaymentPlan          (one plan per property offering / price tier)
       └─ PaymentSchedule  (one row per installment / due date)
            └─ Payment     (one or more actual receipts against a schedule row)
```

A `Customer` is linked to exactly **one** `PaymentPlan` via `Customer.PlanID`. All installments and payment history for a customer flow through that plan.

---

## 2. Data Models

### PaymentPlan (`PaymentPlan` table)

| Column | Type | Notes |
|---|---|---|
| `PlanID` | CHAR(10) PK | Auto-generated GUID prefix |
| `ProjectID` | NVARCHAR(10) FK | Linked project |
| `PlanName` | NVARCHAR(150) | Display name, e.g. "3-Year Installment" |
| `TotalAmount` | decimal(18,2) | Total plan value in PKR |
| `TotalAmountUSD` | decimal(18,2) | Parallel USD value |
| `ExchangeRate` | decimal(18,4) | PKR per USD at plan creation time |
| `Currency` | NVARCHAR(10) | Default `PKR` |
| `DurationMonths` | int | Informational only, not enforced |
| `Frequency` | NVARCHAR(50) | Monthly / Quarterly / Half Yearly / Yearly |

### PaymentSchedule (`PaymentSchedule` table)

One row = one installment slot. Together all schedules for a plan must not exceed `PaymentPlan.TotalAmount`.

| Column | Type | Notes |
|---|---|---|
| `ScheduleID` | CHAR(10) PK | Auto-generated |
| `PlanID` | CHAR(10) FK | Parent plan |
| `InstallmentNo` | int | `0` = token/down-payment; `1..N` = regular installments |
| `DueDate` | datetime | When this installment is due |
| `Amount` | decimal(18,2) | PKR amount for this installment |
| `AmountUSD` | decimal(18,2) | USD equivalent |
| `SurchargeApplied` | bit | Whether late surcharge is configured |
| `SurchargeRate` | decimal(18,2) | Stored as a **fraction** (e.g. `0.05` = 5%) |
| `PaymentDescription` | NVARCHAR(250) | Label, e.g. "Quarterly Installment" |

> **Important:** `SurchargeRate` is stored as a fraction (`0.05`) but the create/edit forms submit it as a **percentage** (`5`). The controller always divides by 100 before saving:
> ```csharp
> schedule.SurchargeRate = schedule.SurchargeRate / 100m;
> ```

### Payment (`Payments` table)

One row = one actual cash receipt. Multiple payments can exist per `PaymentSchedule` (partial payments allowed).

| Column | Type | Notes |
|---|---|---|
| `PaymentID` | CHAR(10) PK | Auto-generated |
| `ScheduleID` | CHAR(10) FK | Which installment this pays |
| `CustomerID` | CHAR(10) FK | Who paid (scoped per customer even on shared plans) |
| `PaymentDate` | datetime | Timestamp of recording |
| `Amount` | decimal(18,2) | Amount received in PKR |
| `Method` | NVARCHAR(50) | Cash / DD/DS / Bank Transfer / Cheque / Online / Mobile Money |
| `ReferenceNo` | NVARCHAR(100) | Cheque/transaction number |
| `Status` | NVARCHAR(250) | `Paid` / `Partially Paid` / `Pending` |
| `Remarks` | NVARCHAR(255) | Free text notes |

---

## 3. Payment Plan Creation (`CreatePaymentPlan`)

**Route:** `POST /Payment/CreatePaymentPlan` (JSON body via AJAX)  
**Permission required:** Edit

### Step-by-step logic

1. **Validate inputs** — `ProjectID` required; `TotalAmount` or `TotalAmountUSD` must be > 0.

2. **Currency conversion** — If only one currency is provided, the other is calculated using `ExchangeRate` from `Configuration` table key `Currency:USDToPKR`. If no rate is configured, defaults to `1`.

3. **Installment amount distribution:**
   ```
   distributableAmount = TotalAmount − tokenAmount
   baseInstallment     = ROUND(distributableAmount / N, 2)
   lastInstallment     = distributableAmount − (baseInstallment × (N−1))
   ```
   The last installment absorbs rounding remainders to guarantee the sum is exact.

4. **Grand total guard:**
   ```
   grandTotal = tokenAmount + sum(all installments)
   grandTotal must NOT exceed PaymentPlan.TotalAmount
   ```
   Returns an error if it does.

5. **Token (installment #0):** If `IncludeToken = true`, a schedule row with `InstallmentNo = 0` is created at `FirstInstallmentDueDate`. Token installments have `SurchargeApplied = false` and `SurchargeRate = 0`.

6. **Regular installments:** Due dates are computed by `CalculateDueDate()`:
   | Frequency | Offset per installment |
   |---|---|
   | Monthly | +1 month |
   | Quarterly | +3 months |
   | Half Yearly | +6 months |
   | Yearly | +1 year |

7. **Plan is saved first**, then all schedules in a batch (`AddRange`).

---

## 4. Adding Individual Schedules (`CreatePaymentSchedule` / `EditPaymentSchedule`)

These actions let admins manually add or adjust single installment rows after the plan is created.

**Business rule enforced on both create and edit:**
```
SUM(all existing schedules) + newAmount  ≤  PaymentPlan.TotalAmount
```
Returns an error if the cap would be exceeded.

**USD auto-fill:** If `AmountUSD` is not provided but `ExchangeRate` is known, `AmountUSD = ROUND(Amount / rate, 2)`. Vice versa if only USD is given.

---

## 5. Record Payment (`RecordPayment`)

This is the core daily-use workflow for receiving customer payments.

### GET — Form Setup

**Route:** `GET /Payment/RecordPayment?scheduleId=&customerId=`

Can be opened two ways:
- **Blank** — staff types a Customer ID and searches.
- **Pre-filled** — linked from the Payment Schedule page with `scheduleId` (and optionally `customerId`). The controller pre-loads the schedule info card and the customer ID is auto-resolved from the plan.

### AJAX Customer Search (`GetCustomerPaymentInfo`)

**Route:** `GET /Payment/GetCustomerPaymentInfo?customerId=`

Returns a JSON payload used to populate the installment dropdown:

```json
{
  "found": true,
  "customerId": "C1500108",
  "fullName": "John Doe",
  "schedules": [
    {
      "scheduleId": "ABCD123456",
      "planName": "3-Year Plan",
      "installmentNo": 3,
      "dueDate": "Mar 01, 2026",
      "isOverdue": true,
      "amount": 50000.00,
      "paid": 10000.00,
      "outstanding": 40000.00,
      "surchargeApplied": true,
      "surchargeRate": 0.05
    }
  ]
}
```

**Key filtering logic:**
- Only **active** customers (`Status = "Active"`) can be found.
- Only schedules with **outstanding > 0** are returned (already fully-paid schedules are hidden).
- Payments are scoped **per customer** — if a plan is shared across customers, each customer's payment history is isolated:
  ```csharp
  .Where(p => scheduleIds.Contains(p.ScheduleID) && p.CustomerID == customerIdTrimmed)
  ```

### POST — Save Payment

**Route:** `POST /Payment/RecordPayment`

#### Validation chain (in order):

| Check | Error message |
|---|---|
| `customerId` or `scheduleId` is empty | "Customer and installment are required." |
| `amount ≤ 0` | "Amount must be greater than zero." |
| Customer not found or inactive | "Customer not found or inactive." |
| Schedule not found | "Installment not found." |
| Schedule's `PlanID ≠ customer.PlanID` | "Selected installment does not belong to this customer's plan." |
| `amount > totalDue` | "Amount must not exceed Total Due for this installment (PKR X)." |
| Full payment but status = "Partially Paid" | Status conflict error |
| Partial payment but status = "Paid" | Status conflict error |

#### Status rules:

```
if amount == totalDue  →  status must be "Paid"
if amount <  totalDue  →  status must be "Partially Paid"
```

These rules are enforced **both on the client** (JS disables conflicting options) **and on the server** (controller validates before saving).

#### What gets saved:

```csharp
new Payment {
    PaymentID   = GenerateID(),   // 10-char GUID prefix
    CustomerID  = customerId,
    ScheduleID  = scheduleId,
    PaymentDate = DateTime.Now,
    Amount      = amount,
    Method      = method,
    ReferenceNo = referenceNo,
    Status      = status,
    Remarks     = remarks
}
```

After saving, the user is redirected **back to the same Record Payment page** (with the same `customerId` and `scheduleId` pre-filled) so multiple partial payments can be entered in one session.

---

## 6. Edit Payment (`EditPayment`)

**Permission required:** Edit

Only the following fields can be edited on an existing payment:
- `PaymentDate`, `Amount`, `Method`, `ReferenceNo`, `Status`, `Remarks`

**Guard:** `Amount` must not exceed `(Schedule.Amount − sum of all OTHER payments for this schedule)`.  
This prevents editing a payment to exceed the installment cap.

---

## 7. Delete Payment (`DeletePayment`)

**Permission required:** Admin

Hard-deletes the payment row. No soft-delete. An activity log entry is created before deletion.

---

## 8. Late Payment Surcharge (Display Only)

Surcharge is **not automatically added** to the payment amount. It is informational only:

- The `RecordPayment` view shows a yellow warning card when `SurchargeApplied = true` and `DueDate < today`.
- The warning displays: days overdue, surcharge amount (`Amount × SurchargeRate`), and recommended total.
- Staff must manually decide whether to collect the surcharge — it is not enforced by the system.

---

## 9. Penalties and Waivers

These are **standalone** records not tied to `PaymentSchedule`. They are tracked separately for reporting purposes.

### Penalty (`Penalties` table)
- Added manually by staff per customer.
- Fields: `CustomerID`, `Amount`, `Reason`, `AppliedOn`.
- No automatic application — purely a record.

### Waiver (`Waivers` table)
- Approved by the logged-in user (`ApprovedBy = current UserID`).
- Fields: `CustomerID`, `Amount`, `Reason`, `ApprovedBy`, `CreatedAt`.
- No automatic deduction from outstanding — purely a record.

---

## 10. Access Control (ACL)

| Action | Required Permission |
|---|---|
| View any payment page | Read |
| Record Payment, Edit Payment, Create/Edit Plan & Schedule | Edit |
| Delete Payment, Delete Schedule | Admin |

Permissions are checked via `IModulePermissionService` using `ModuleKey = "Payment"`.

---

## 11. Activity Logging

Every create, edit, and delete action writes to `ActivityLog`:

| Action text | RefType | RefID |
|---|---|---|
| `Record Payment - {UserName}` | `Payment` | `PaymentID` |
| `Edit Payment - {UserName}` | `Payment` | `PaymentID` |
| `Delete Payment. CustomerID: {X}` | `Payment` | `PaymentID` |
| `Create Payment Plan` | `PaymentPlan` | `PlanID` |
| `Create Payment Schedule` | `PaymentSchedule` | `ScheduleID` |
| `Update Payment Schedule` | `PaymentSchedule` | `ScheduleID` |
| `Delete Payment Schedule` | `PaymentSchedule` | `ScheduleID` |

---

## 12. Page Inventory

| URL | Purpose |
|---|---|
| `/Payment/Index` | All payments (raw list) |
| `/Payment/PaymentPlans` | All payment plans with customer and schedule counts |
| `/Payment/PaymentSchedule?planId=` | Installments for a specific plan |
| `/Payment/Schedules` | All installments across all plans |
| `/Payment/CustomerPayments` | All payment receipts; filterable by customer |
| `/Payment/RecordPayment` | Record a new payment receipt |
| `/Payment/EditPayment?paymentId=` | Edit an existing receipt |
| `/Payment/Receipt?id=` | Print-friendly payment receipt |
| `/Payment/CreatePaymentPlan` | Wizard to create a new plan + schedule in one step |
| `/Payment/CreatePaymentSchedule?planId=` | Add a single installment to an existing plan |
| `/Payment/Penalties` | View and add customer penalties |
| `/Payment/Waivers` | View and add customer waivers |

---

## 13. Common Developer Pitfalls

1. **Surcharge rate storage:** Always stored as a fraction (`0.05`), but forms submit as percentage (`5`). The controller divides by 100 on save. Don't divide again when displaying — multiply by 100 to show `%`.

2. **Payments are per-customer, not per-plan:** When summing paid amounts, always filter by both `ScheduleID` **and** `CustomerID`. Plans can theoretically be shared, so summing all payments on a schedule without the customer filter would show incorrect totals.

3. **Outstanding = `Amount − paidByThisCustomer`**, not total payments from all customers.

4. **Partial payments are allowed on the same schedule:** Multiple `Payment` rows can reference the same `ScheduleID` for the same customer. The sum of those rows must never exceed `PaymentSchedule.Amount`.

5. **Token is installment #0:** When displaying an installment list, `InstallmentNo = 0` is the down-payment/token, not a regular installment. Handle it specially in reports if needed.

6. **`ExchangeRate` is fixed at plan creation time** — it is not recalculated when the live rate changes. The rate from `Configuration['Currency:USDToPKR']` is only read at plan creation.
