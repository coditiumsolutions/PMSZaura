# GCP deployment (PMS)

## Production target

| Item | Value |
|------|--------|
| URL | https://pms.coditium.com |
| VM IP | `34.131.132.158` |
| SSH user | `coditiums` |
| SSH key (local) | `D:\.ssh\Learning\gcp_coditium_vm` |
| App directory | `/var/www/pms` |
| systemd unit | `pms.service` → `dotnet /var/www/pms/PMS.dll` |
| Kestrel | `http://127.0.0.1:8080` (nginx reverse proxy) |
| DataProtection keys | `/var/www/pms/data-protection-keys` |

**Incorrect / obsolete values (do not use):**

- SSH user `coditiumsolutions`
- Key `D:\.ssh\GCP ssh\github_deploy_key` as the default local deploy key (may exist for CI; local deploy uses Learning key)
- App path `/var/www/pms/app` (service does not run from there)
- Old VM IP `34.93.208.98`

## Deploy from Windows

```powershell
cd d:\Dotnet\CoreCursor\PMSCoditium\PMS
powershell -ExecutionPolicy Bypass -File .\deploy-gcp.ps1
```

What it does:

1. `dotnet publish` → `linux-x64` (framework-dependent) under `publish-out/linux-x64`
2. Packs `pms-gcp-deploy.tar.gz` and uploads to `/tmp` on the VM
3. Runs `deploy-gcp-remote.sh` over SSH as `coditiums` (uses `sudo` on the VM)

Uploads and data-protection keys under `/var/www/pms` are preserved (rsync excludes).

## Manual SSH check

```powershell
ssh -i "D:\.ssh\Learning\gcp_coditium_vm" coditiums@34.131.132.158
sudo systemctl status pms --no-pager
```

## Related docs

- [DEPLOYMENT_COMMANDS.txt](./DEPLOYMENT_COMMANDS.txt) — short cheat sheet
- [Documentation/AccountStatementPdf.md](./Documentation/AccountStatementPdf.md) — Linux PDF / disk notes
