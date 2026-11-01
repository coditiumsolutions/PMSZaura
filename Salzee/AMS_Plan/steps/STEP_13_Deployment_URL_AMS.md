# Step 13 (parallel) — Deploying AMS at `zkbeclipse.pk/AMS`

This step can be **planned early** alongside Step 1–2; it does not replace database steps.

## Can AMS live at `https://zkbeclipse.pk/AMS`?

**Yes**, for a single ASP.NET Core MVC app hosting both PMS and AMS, using a **path prefix** is standard and **does not break the project by itself**.

### Typical approach

1. **ASP.NET Core Area** named `AMS` with route prefix `AMS` → URLs like `/AMS/SomeController/Index`.
2. Or a **conventional route** added before the default route: `pattern: "AMS/{controller=Home}/{action=Index}/{id?}"`.
3. **Navigation:** AMS section of `_Layout` or a dedicated AMS layout with links rooted at `/AMS/...`.

### Things to verify (avoid surprises)

| Topic | Risk | Mitigation |
|--------|------|------------|
| **Reverse proxy / IIS** | If the public URL is `/AMS` but the app receives requests **without** the path segment, links and redirects break. | Set **`PathBase`** (`app.UsePathBase("/AMS")`) when the proxy forwards stripped paths; match your real IIS ARR setup. |
| **Static files** | Default `wwwroot` URLs are often root-relative. | Ensure AMS views reference `~/...` or include path base in tag helpers / `asp-append-version`. |
| **Authentication** | Cookie path usually `/` — OK. | If you ever set `Path` on cookies, include `/AMS` consistently. |
| **Same site, same pool** | Heavy AMS queries share CPU with PMS. | Acceptable for SME scale; monitor later. |
| **IIS “Application” under site** | Sometimes `/AMS` is a **child application** with its own `web.config`. | Your current deploy is **one** app to `C:\PMSDeploy`; keep **one** `web.config` unless you intentionally split. |

### Alternative

- **`ams.zkbeclipse.pk`** — clearer isolation, separate binding/cert/DNS effort.

## Checklist for go-live URL

- [ ] Route prefix works for controllers + API (if any)
- [ ] Login redirect returns to correct path after auth
- [ ] Production `web.config` / Kestrel behind IIS tested once with `/AMS`

## Notes

*(Record actual IIS site binding: root vs `/AMS` virtual app.)*

## Sign-off

- [ ] Who deployed / verified URL
