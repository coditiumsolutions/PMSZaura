# Step 12 — Audit, permissions, scheduled jobs, go-live

**Status:** In progress (12.1–12.3 partial in app; 12.4 UAT / parallel run is process)  
**Prerequisites:** Core AMS usable; integration if live posting required.

## Objectives

- AccountingAuditLog
- `UserModulePermission` extensions plan §10
- Jobs plan §11
- UAT & parallel run plan §13 Phase 7

## Checklist

- [ ] Role matrix tested
- [ ] Go-live checklist completed

## Notes

### 12.1 App-level audit

- Table `acc.AccountingAuditLog` (see `Scripts/AMS_Create_acc_schema.sql`).
- EF entity `AccAccountingAuditLog`, `DbSet` on `PMSDbContext`.
- `AccAccountingAuditSaveChangesInterceptor` (singleton) registered on `AddDbContext`; `AddHttpContextAccessor()` required. Writes one row per changed `acc` entity per save (INSERT/UPDATE/DELETE), skips `AccountingAuditLog` itself.
- UI: `AmsAuditController` / `Views/AmsAudit` — read-only list + JSON detail (AMS module read permission).

### 12.2 Permissions

- `AMS` is already a `UserModulePermission` module key (seed + `AccountController` profile). No extra schema; assign read/edit per user as needed.

### 12.3 Scheduled jobs

- `AmsAccountingJobsHostedService` + options `AmsBackgroundJobs` in `appsettings.json` (`Enabled`, `StartupDelaySeconds`, `IntervalMinutes`). Default `Enabled: false`; when true, logs a daily-style count of post-dated cheques due (Pending, `ChequeDate` ≤ UTC today). Invoice generator / reminders remain placeholders.

### 12.4 UAT & parallel run

- Follow plan §13 Phase 7 and §14 scenarios; not automated in code.

## Sign-off
