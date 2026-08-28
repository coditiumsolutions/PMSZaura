# Deploy PMS to IIS – Step-by-Step Guide

## Prerequisites

1. **.NET SDK** (on your dev machine) – for building and publishing  
   - [Download](https://dotnet.microsoft.com/download) if needed.

2. **On the server (where IIS runs):**
   - **.NET Runtime** (or ASP.NET Core Hosting Bundle) for the same version your app targets  
   - **IIS** with **ASP.NET Core Module v2**  
   - Install Hosting Bundle from: https://dotnet.microsoft.com/download/dotnet (choose your .NET version → Hosting Bundle).

---

## 1. Run the deployment script

From the project folder (`d:\Dotnet\CoreCursor\PMSCoditium\PMS`):

**Option A – Batch file**
```cmd
.\DeployToIIS.bat
```

**Option B – PowerShell**
```powershell
powershell -ExecutionPolicy Bypass -File .\DeployToIIS.ps1
```

For **GCP Ubuntu production**, use [DEPLOY_GCP.md](./DEPLOY_GCP.md) / `deploy-gcp.ps1` instead.

This will:
- Clean, restore, and build in **Release**
- Publish the app to **D:\PMSDeploy**
- Fix `web.config` (DLL name and Production environment)

---

## 2. Configure IIS (first time only)

### 2.1 Open IIS Manager

- Press **Win + R**, type **inetmgr**, press Enter  
  or  
- Server Manager → Tools → **Internet Information Services (IIS) Manager**

### 2.2 Create Application Pool (if not exists)

1. In the left tree, click **Application Pools**.
2. **Add Application Pool**:
   - Name: **PMS**
   - .NET CLR: **No Managed Code**
   - Managed pipeline: **Integrated**
   - Start application pool immediately: **checked**
3. Click **OK**.
4. Right‑click **PMS** → **Advanced Settings**:
   - Set **Identity** to an account that can read `D:\PMSDeploy` (e.g. **ApplicationPoolIdentity** or a dedicated domain account).

### 2.3 Create or update the website

1. In the left tree, click **Sites**.
2. **Add Website** (or edit existing **PMS** site):
   - **Site name:** PMS  
   - **Application pool:** PMS  
   - **Physical path:** `D:\PMSDeploy`  
   - **Binding:**  
     - Type: **http**  
     - IP: **172.20.228.2** (or **All Unassigned**)  
     - Port: **84**  
     - Host name: leave blank unless you use a host header  
3. Click **OK**.

### 2.4 Permissions

- Ensure the app pool identity has **Read & Execute** on `D:\PMSDeploy` and subfolders.  
- If you use **ApplicationPoolIdentity**, grant rights to **IIS AppPool\PMS** on `D:\PMSDeploy`.

---

## 3. After each deployment

1. **Restart the site** (optional but recommended):
   ```cmd
   C:\Windows\System32\inetsrv\appcmd.exe stop site "PMS"
   C:\Windows\System32\inetsrv\appcmd.exe start site "PMS"
   ```
   Or in IIS Manager: **Sites** → right‑click **PMS** → **Manage Website** → **Restart**.

2. **Or recycle the app pool:**
   - **Application Pools** → right‑click **PMS** → **Recycle**.

3. **Test:**  
   Open: **http://172.20.228.2:84/**  
   (Or **http://localhost:84** if binding is on All Unassigned.)

---

## Summary

| Item        | Value              |
|------------|--------------------|
| Deploy path| `D:\PMSDeploy`      |
| IIS site   | PMS                |
| URL        | http://172.20.228.2:84/ |
| Environment| Production (set in `web.config`) |

---

## Troubleshooting

- **500.19 / web.config error:** Install ASP.NET Core Hosting Bundle on the server.
- **500.30 / In-Process start failure:** Check Event Viewer (Windows Logs → Application) and that the app pool has access to `D:\PMSDeploy`.
- **Blank or generic error page:** Ensure `web.config` has `<environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />` (already set in this project).
- **Connection refused:** Confirm the site is started and the firewall allows port 84.
