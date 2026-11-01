# PMS Offline Migration Instructions (No Link Between Servers)

This runbook assumes the **new server has zero network link** to the current server.  
Migration is done by preparing an offline package, copying it (USB/external disk), and restoring locally.
This guide uses **`.sql` dump files only** (no `.bak` required).

Target machine:
- Windows Server 2022
- Visual Studio 2022
- SQL Server 2022
- Cursor AI

---

## 1) Offline migration package (build this on old server)

Create one folder, e.g. `D:\PMS_MigrationPackage_YYYYMMDD`, with:

1. `Project\` -> full project folder copy (entire repository)
2. `Backups\PMS_FULL_SCHEMA_DATA_YYYYMMDD_HHMM.sql` -> fresh full SQL schema+data dump (**required**)
4. `Uploads\` -> copy of old app uploads folder (if you need historical attachments/files)
5. `Checksums\SHA256.txt` -> file hash list for integrity verification

> Because servers are isolated, include everything needed in this package before move.

---

## 2) Create fresh SQL dump backup (old server)

### 2.1 Full schema+data SQL dump (`.sql`) - required

From project root:

```powershell
powershell -ExecutionPolicy Bypass -File ".\Scripts\Export-FullSqlDump.ps1" `
  -ServerInstance "YOUR_SQL_SERVER" `
  -DatabaseName "PMS" `
  -OutputFile "D:\PMS_MigrationPackage_YYYYMMDD\Backups\PMS_FULL_SCHEMA_DATA_YYYYMMDD_HHMM.sql" `
  -TrustServerCertificate
```

This dump contains table/view/procedure/function schema and data inserts.

### 2.2 Generate checksums for all package files

```powershell
Get-ChildItem "D:\PMS_MigrationPackage_YYYYMMDD" -Recurse -File |
Get-FileHash -Algorithm SHA256 |
ForEach-Object { "$($_.Hash) *$($_.Path)" } |
Set-Content "D:\PMS_MigrationPackage_YYYYMMDD\Checksums\SHA256.txt"
```

---

## 3) Move package to new server (offline media)

1. Copy `D:\PMS_MigrationPackage_YYYYMMDD` to external media.
2. Move media to new server.
3. Copy package to new server, e.g. `D:\PMS_MigrationPackage_YYYYMMDD`.
4. Verify hashes:

```powershell
Get-FileHash -Algorithm SHA256 "D:\PMS_MigrationPackage_YYYYMMDD\Backups\*.sql"
```

Compare with `Checksums\SHA256.txt`.

---

## 4) Prepare new server prerequisites (all local)

Install/verify on the new server:

1. .NET 8 SDK (`dotnet --info`)
2. ASP.NET Core Hosting Bundle (matching runtime major version)
3. SQL Server 2022 + SSMS
4. IIS:
   - Web Server
   - Management Tools
5. Visual Studio 2022

No step in this guide depends on old server connectivity.

---

## 5) Place project and uploads on new server

1. Copy project to target path, e.g.:
   - `D:\PMS\PMS\PMS`
2. If you exported uploads:
   - copy package `Uploads\` into published app uploads path later (`D:\PMSDeploy\wwwroot\uploads`)

Open `D:\PMS\PMS\PMS` in Cursor AI.

---

## 6) Restore database locally on SQL Server 2022 (SQL dump only)

Use the provided import utility:

```powershell
powershell -ExecutionPolicy Bypass -File ".\Scripts\Import-FullSqlDump.ps1" `
  -ServerInstance ".\SQL2022" `
  -DatabaseName "PMS" `
  -DumpFile "D:\PMS_MigrationPackage_YYYYMMDD\Backups\PMS_FULL_SCHEMA_DATA_YYYYMMDD_HHMM.sql"
```

`Import-FullSqlDump.ps1` will:
1. Create database `PMS` if it does not exist
2. Execute the full schema+data dump into that database
3. Recreate tables, views, procedures, functions, and data from the dump

---

## 7) Configure application settings on new server

Edit:
- `appsettings.json`
- `appsettings.Development.json` (if used)

Set:
1. Local SQL Server 2022 connection string
2. Local URLs/secrets/paths


Because this is isolated, remove/avoid any references to old server names/addresses.

---

## 8) Build and publish locally

From `D:\PMS\PMS\PMS`:

```powershell
dotnet restore
dotnet build
.\DeployToIIS.bat
```

Expected publish folder from current script:
- `D:\PMSDeploy`

---

## 9) Configure IIS locally

1. Create site `PMS` (or your name)
2. Physical path: `D:\PMSDeploy`
3. App pool:
   - No Managed Code
   - Integrated
4. Bindings: local port/host as needed
5. File permissions:
   - Read/execute on `D:\PMSDeploy`
   - Write on `D:\PMSDeploy\wwwroot\uploads` (and other runtime write folders)

Restart IIS:

```powershell
iisreset
```

---

## 10) Post-migration validation (new server only)

Validate all inside new environment:

1. Login
2. Dashboard
3. Customer create/edit
4. Property allotment
5. Transfer create/edit
6. NDC create/edit
7. Payment flows
8. Attachments upload/download
9. Reports

Also collect:

```powershell
dotnet --info
```

---

## 11) New-server safety export (mandatory)

After successful validation on new server, take a fresh SQL dump there:

```powershell
powershell -ExecutionPolicy Bypass -File ".\Scripts\Export-FullSqlDump.ps1" `
  -ServerInstance ".\SQL2022" `
  -DatabaseName "PMS" `
  -OutputFile "D:\Backups\PMS_POST_MIGRATION_YYYYMMDD_HHMM.sql" `
  -TrustServerCertificate
```

Keep:
- Source pre-migration SQL dump
- New server post-migration SQL dump

---

## 12) Cursor AI handoff prompt for new server

Use this in Cursor on the new server:

> Read `Migrations/MigrationInstructions.md` and execute this as a fully offline migration (no connectivity to old server). Use only local files from the migration package, configure local SQL/IIS, deploy, and run full smoke tests. Ask before any destructive DB action.

---

## 13) Common offline migration failures

- **Missing package artifact**  
  Rebuild migration package on old server and recopy via external media.

- **DB restore path mismatch**  
  Use `RESTORE FILELISTONLY` + `MOVE`.

- **500.30 startup failure**  
  Install/repair hosting bundle, verify app pool settings.

- **Attachment files missing**  
  Ensure offline `Uploads\` copy is placed in new publish path.


