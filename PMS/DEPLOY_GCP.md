# GCP deployment (PMS)

## Production target

| Item | Value |
|------|--------|
| URL | https://zaura.coditium.com |
| VM IP | `34.93.239.49` |
| SSH user | `zaura_coditium` |
| SSH key (local) | `D:\.ssh\key_gcp_zaura` |
| App directory | `/var/www/pms` |
| systemd unit | `pms.service` → `dotnet /var/www/pms/PMS.dll` |
| Kestrel | `http://127.0.0.1:8080` (nginx reverse proxy) |
| DataProtection keys | `/var/www/pms/data-protection-keys` |

SQL Server runs on the VM. The app must use `127.0.0.1` (not the public IP) for `ConnectionStrings:DefaultConnection`.

## GitHub Actions (preferred)

Workflow: [`.github/workflows/deploy-gcp.yml`](../.github/workflows/deploy-gcp.yml)

It publishes `linux-x64` on every push to `main` that touches `PMS/**`, or when you run **Actions → Deploy PMS to GCP → Run workflow**.

Required repository secret:

| Secret | Source |
|--------|--------|
| `SSH_PRIVATE_KEY` | Private key from `D:\.ssh\key_gcp_zaura` |

Optional secrets (defaults match this VM):

| Secret | Default |
|--------|---------|
| `SSH_HOST` | `34.93.239.49` |
| `SSH_USER` | `zaura_coditium` |
| `SSH_PORT` | `22` |
| `SSH_REMOTE_PATH` | `/var/www/pms` |
| `SSH_SERVICE_NAME` | `pms` |
| `SSH_DOMAIN_NAME` | `zaura.coditium.com` |
| `GCP_DB_CONNECTION` | `Server=127.0.0.1;Database=DBZaura;...` (local SQL on the VM) |

The private key is never stored in the repo.

## Deploy from Windows

```powershell
cd D:\Dotnet\CoreCursor\Zaura-PMSCoditium\PMS
powershell -ExecutionPolicy Bypass -File .\deploy-gcp.ps1
```

What it does:

1. `dotnet publish` → `linux-x64` (framework-dependent) under `publish-out/linux-x64`
2. Packs `pms-gcp-deploy.tar.gz` and uploads to `/tmp` on the VM
3. Runs `deploy-gcp-remote.sh` over SSH as `zaura_coditium` (uses `sudo` on the VM)

Uploads and data-protection keys under `/var/www/pms` are preserved (rsync excludes).

## Manual SSH check

```powershell
ssh -i "D:\.ssh\key_gcp_zaura" zaura_coditium@34.93.239.49
sudo systemctl status pms --no-pager
```

## Related docs

- [DEPLOYMENT_COMMANDS.txt](./DEPLOYMENT_COMMANDS.txt) — short cheat sheet
- [Documentation/AccountStatementPdf.md](./Documentation/AccountStatementPdf.md) — Linux PDF / disk notes
