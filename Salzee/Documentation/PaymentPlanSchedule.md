# Payment Plan & Payment Schedule Module

## Overview

A **Payment Plan** is a billing template tied to a project. It defines the total price and duration. Each plan contains one or more **Payment Schedules** (installments) that customers pay against. Plans are shared across all customers assigned to the same plan.

**Controller:** `PaymentController.cs`  
**Models:** `PaymentPlan.cs`, `PaymentSchedule.cs`, `PaymentPlanCreateViewModel.cs`  
**Views:** `Views/Payment/` (PaymentPlans, PaymentSchedule, CreatePaymentPlan, CreatePaymentSchedule)

---

## Parent / child relationships

| Role | Entity | Key |
|------|--------|-----|
| Parent | `PaymentPlan` | `PlanID` |
| Children | `PaymentSchedule` rows | `PlanID` → `PaymentPlan` |
| Assignees | `Customer` | `PlanID` → `PaymentPlan` (many customers can share one plan) |

EF Core: deleting a **plan** cascades to its **schedules**. Deleting a **schedule** that still has **Payment** rows will fail (payments FK uses `Restrict`). Remove or reassign payments first.

**Plan detail / installments page:** `GET /Payment/PaymentSchedule?planId={PlanID}` — loads the plan with `PaymentSchedules` and `Customers` (read permission). Example: `.../Payment/PaymentSchedule?planId=6917D14391`.

---

## Database Tables

### PaymentPlan

| Column | Type | Notes |
|--------|------|-------|
| PlanID | CHAR(10) PK | Random 10-char ID |
| ProjectID | CHAR(10) FK | Which project this plan belongs to |
| PlanName | NVARCHAR(150) | Display name |
| TotalAmount | DECIMAL(18,2) | Total price in local currency (PKR) |
| TotalAmountUSD | DECIMAL(18,2) | Equivalent in USD |
| ExchangeRate | DECIMAL(18,4) | USD-to-PKR rate at plan creation |
| Currency | NVARCHAR(10) | e.g. PKR, SSP |
| DurationMonths | INT | Plan duration |
| Frequency | NVARCHAR(50) | Monthly, Quarterly, Half Yearly, Yearly |
| Description | NVARCHAR(255) | Optional notes |
| CreatedAt | DATETIME | Auto-set |

**Relationships:** One plan has many Customers and many PaymentSchedules.

### PaymentSchedule

| Column | Type | Notes |
|--------|------|-------|
| ScheduleID | CHAR(10) PK | Random 10-char ID |
| PlanID | CHAR(10) FK | Parent plan |
| PaymentDescription | NVARCHAR(250) | e.g. "Installment", "Token" |
| InstallmentNo | INT | 0 = Token, 1+ = regular installments |
| DueDate | DATETIME | When this installment is due |
| Amount | DECIMAL(18,2) | Installment amount (PKR) |
| AmountUSD | DECIMAL(18,2) | Installment amount (USD) |
| SurchargeApplied | BIT | Whether late surcharge applies |
| SurchargeRate | DECIMAL(18,2) | Stored as decimal (e.g. `0.05` = 5%). **Add/Edit schedule forms** send a percentage; the controller divides by 100. **Create plan (JSON)** sends the decimal already (see `CreatePaymentPlan.cshtml`). |
| Description | NVARCHAR(255) | Optional notes |

**Relationships:** One schedule has many Payments.

---

## Creating a Payment Plan (with auto-generated schedules)

`POST CreatePaymentPlan` accepts **JSON** (`[FromBody] PaymentPlanCreateViewModel`), not a classic form post.

1. Validates project, total amount (PKR or USD), and exchange rate.
2. Auto-calculates if only one currency is provided (converts using exchange rate).
3. Uses the user-entered **Installment Amount (PKR)** for each generated installment row (USD derived from exchange rate when missing).
4. Validates: `TotalInstallments > 0`, token ≤ plan total, and **token + all installment amounts ≤ plan total** (strict exceed is rejected; sum may be below total).
5. Creates the `PaymentPlan`, then `PaymentSchedule` rows (optional token as installment #0 without surcharge and with its own token description; regular rows use the regular payment description; optional possession is added as the **last** payment with manual amount + account head and is skipped when amount is `0`). **Token** and **possession** each have their own due date on the create form; only **regular** installments use the first-installment date plus frequency stepping below.

### Due Date Calculation

**Token (installment 0)** and **possession (last payment)** use the due dates entered on Create Payment Plan. **Regular installments** use the first installment due date plus frequency:

| Frequency | Interval |
|-----------|----------|
| Monthly | +1 month per installment index |
| Quarterly | +3 months per index |
| Half Yearly | +6 months per index |
| Yearly | +1 year per index |

---

## Adding / Editing / Deleting Individual Schedules

- **Create (`CreatePaymentSchedule`):** Sum of all schedule amounts after insert must not exceed `PaymentPlan.TotalAmount`. Surcharge rate from form: percentage → stored decimal. PKR/USD filled from the other using plan `ExchangeRate` when needed.
- **Edit (`EditPaymentSchedule`):** By default, sum of all schedules (including the edited row) must not exceed `PaymentPlan.TotalAmount`. If it would exceed, the UI offers **Increase payment plan total** with a **required reason**; the parent plan’s `TotalAmount` (and USD from the plan’s exchange rate) is raised in the same transaction. The plan page shows how many **customers** share the plan. A row is written to `ActivityLog` with `Details` JSON (after migration `AddActivityLogDetails`).
- **Delete (`DeletePaymentSchedule`):** Requires module delete (Admin) permission. Fails if payments still reference the schedule.

---

## Pages & Actions

| Action | Permission | Description |
|--------|-----------|-------------|
| `PaymentPlans` | Read | List all plans with project, customer count, schedule count |
| `CreatePaymentPlan` | Edit | JSON plan + auto schedules |
| `PaymentSchedule(planId)` | Read | Plan details + all installments for that `planId` |
| `CreatePaymentSchedule(planId)` | Edit | Add one installment |
| `EditPaymentSchedule` | Edit | Update an installment |
| `DeletePaymentSchedule` | Admin (delete) | Remove an installment |
| `Schedules` | Read | Global list of all schedules across plans |

---

## Exchange Rate

Pulled from `Configurations` key `Currency:USDToPKR`. If missing or invalid, defaults to `1`. Used for PKR/USD conversion on plan creation and schedule add/edit when one side is missing.
