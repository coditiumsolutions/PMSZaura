using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PMS.Data;
using PMS.Models;
using PMS.Services;
using System.Security.Claims;
using BCrypt.Net;

namespace PMS.Controllers
{
    public class AccountController : Controller
    {
        private const string TwoFactorPendingCookie = "PMS.2FA.Pending";
        private const string TwoFactorSetupCookie = "PMS.2FA.Setup";
        private const int TwoFactorPendingMinutes = 10;
        private const int TwoFactorMaxAttempts = 5;
        private const int TwoFactorLockoutMinutes = 15;

        private readonly PMSDbContext _context;
        private readonly IModulePermissionService _modulePermission;
        private readonly ISiteConfigService _siteConfigService;
        private readonly ITwoFactorConfigService _twoFactorConfig;
        private readonly ITotpAuthenticatorService _totp;
        private readonly TotpSecretProtector _totpProtector;
        private readonly IMemoryCache _memoryCache;

        private static readonly string[] ModuleKeys = new[]
        {
            "Home", "Registration", "Customer", "Transfer", "TransferFee", "NDC", "Project", "Dealer", "Property", "Payment",
            "Allotment", "Rental", "SalesInquiry", "Reports", "Account", "Settings", "ActivityLog",
            "AccountsManagement", "Ticket", "TesSQL", "InquiryApi", "Refund", "DuplicateFileTransfer", "Waiver", "PaymentAudit", "Possession"
        };

        private static readonly string[] PermissionOptions = new[] { "NoAccess", "Read", "Author", "Edit", "Admin" };

        public AccountController(
            PMSDbContext context,
            IModulePermissionService modulePermission,
            ISiteConfigService siteConfigService,
            ITwoFactorConfigService twoFactorConfig,
            ITotpAuthenticatorService totp,
            TotpSecretProtector totpProtector,
            IMemoryCache memoryCache)
        {
            _context = context;
            _modulePermission = modulePermission;
            _siteConfigService = siteConfigService;
            _twoFactorConfig = twoFactorConfig;
            _totp = totp;
            _totpProtector = totpProtector;
            _memoryCache = memoryCache;
        }

        [HttpGet]
        public async Task<IActionResult> Login()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var homePerm = await _modulePermission.GetPermissionAsync(userId, "Home");
                if (_modulePermission.CanRead(homePerm))
                    return RedirectToAction("Coditium", "Home");
                else
                    return RedirectToAction("Workspace", "Home");
            }
            var siteConfig = await _siteConfigService.GetAsync();
            return View(siteConfig);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Login(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Please enter both email and password.";
                var cfg = await _siteConfigService.GetAsync();
                return View(cfg);
            }

            try
            {
                // Case-insensitive email comparison
                var user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Email != null && u.Email.ToLower() == email.ToLower() && u.IsActive);

                if (user != null && !string.IsNullOrEmpty(user.PasswordHash) && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                {
                    var enforce2Fa = await _twoFactorConfig.IsEnforce2FAAsync();
                    var require2Fa = enforce2Fa || user.TwoFactorEnabled;

                    if (!require2Fa)
                        return await CompleteSignInAsync(user);

                    var rateKey = TwoFactorRateKey(user.UserID);
                    if (IsTwoFactorLockedOut(rateKey))
                    {
                        ViewBag.Error = "Too many failed attempts. Try again later or contact an administrator.";
                        var cfgLocked = await _siteConfigService.GetAsync();
                        return View(cfgLocked);
                    }

                    AppendTwoFactorPendingCookie(user.UserID);

                    if (!user.TwoFactorEnabled)
                    {
                        if (!enforce2Fa)
                        {
                            ClearTwoFactorCookies();
                            return await CompleteSignInAsync(user);
                        }
                        return RedirectToAction(nameof(TwoFactorSetup));
                    }

                    return RedirectToAction(nameof(TwoFactorVerify));
                }

                ViewBag.Error = "Invalid email or password.";
                var siteConfig = await _siteConfigService.GetAsync();
                return View(siteConfig);
            }
            catch (Exception ex) when (IsDatabaseConnectivityFailure(ex))
            {
                ViewBag.Error = "Cannot reach the database server. Connect OpenVPN (or check network/firewall), then try again.";
                var cfgDb = await _siteConfigService.GetAsync();
                return View(cfgDb);
            }
        }

        private static bool IsDatabaseConnectivityFailure(Exception ex)
        {
            for (var e = ex; e != null; e = e.InnerException)
            {
                if (e is System.ComponentModel.Win32Exception)
                    return true;
                if (e is System.Net.Sockets.SocketException)
                    return true;
                if (e is SqlException sqlEx &&
                    (sqlEx.Number == -2 || sqlEx.Number == 53 || sqlEx.Number == 40 ||
                     sqlEx.Message.Contains("network-related", StringComparison.OrdinalIgnoreCase) ||
                     sqlEx.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
                     sqlEx.Message.Contains("could not open", StringComparison.OrdinalIgnoreCase)))
                    return true;
            }
            return false;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> TwoFactorVerify()
        {
            var pendingUserId = ReadPendingUserId();
            if (string.IsNullOrEmpty(pendingUserId))
                return RedirectToAction(nameof(Login));

            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserID == pendingUserId && u.IsActive);
            if (user == null)
            {
                ClearTwoFactorCookies();
                return RedirectToAction(nameof(Login));
            }

            if (!user.TwoFactorEnabled)
            {
                ClearTwoFactorCookies();
                return RedirectToAction(nameof(Login));
            }

            ViewData["Title"] = "Authenticator verification";
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TwoFactorVerify(string code)
        {
            var pendingUserId = ReadPendingUserId();
            if (string.IsNullOrEmpty(pendingUserId))
                return RedirectToAction(nameof(Login));

            var rateKey = TwoFactorRateKey(pendingUserId);
            if (IsTwoFactorLockedOut(rateKey))
            {
                ViewBag.Error = "Too many failed attempts. Try again later.";
                return View();
            }

            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserID == pendingUserId && u.IsActive);
            if (user == null || !user.TwoFactorEnabled)
            {
                ClearTwoFactorCookies();
                return RedirectToAction(nameof(Login));
            }

            var secretPlain = _totpProtector.UnprotectFromStorage(user.TwoFactorSecret);
            // #region agent log
            AgentDebugLog.Write("H4", "AccountController.TwoFactorVerify:post", "before_verify", new
            {
                hasSecretPlain = !string.IsNullOrEmpty(secretPlain),
                storedSecretFieldLen = user.TwoFactorSecret?.Length ?? 0
            });
            // #endregion
            if (string.IsNullOrEmpty(secretPlain) || !_totp.VerifyTotp(secretPlain, code ?? ""))
            {
                RecordTwoFactorFailure(rateKey);
                ViewBag.Error = "Invalid code. Try again.";
                return View();
            }

            ClearTwoFactorFailures(rateKey);
            ClearTwoFactorCookies();
            return await CompleteSignInAsync(user);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> TwoFactorSetup()
        {
            var pendingUserId = ReadPendingUserId();
            if (string.IsNullOrEmpty(pendingUserId))
                return RedirectToAction(nameof(Login));

            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserID == pendingUserId && u.IsActive);
            if (user == null)
            {
                ClearTwoFactorCookies();
                return RedirectToAction(nameof(Login));
            }

            if (user.TwoFactorEnabled)
                return RedirectToAction(nameof(TwoFactorVerify));

            var site = await _siteConfigService.GetAsync();
            var issuer = string.IsNullOrWhiteSpace(site.ProjectName) ? "PMS" : site.ProjectName!;
            var secret = _totp.GenerateBase32Secret();
            var account = user.Email ?? user.UserID;
            var uri = _totp.BuildOtpAuthUri(issuer, account, secret);
            var qrPng = _totp.CreateQrCodePng(uri);
            var setupExp = DateTimeOffset.UtcNow.AddMinutes(TwoFactorPendingMinutes);
            var setupPayload = TwoFactorPendingSerializer.SerializeSetup(user.UserID, secret, setupExp);
            Response.Cookies.Append(TwoFactorSetupCookie, _totpProtector.ProtectSetupPayload(setupPayload), BuildTwoFactorCookieOptions(setupExp));

            ViewBag.QrPngBase64 = Convert.ToBase64String(qrPng);
            ViewBag.ManualSecret = secret;
            ViewBag.Issuer = issuer;
            ViewBag.Account = account;
            ViewData["Title"] = "Set up Google Authenticator";
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TwoFactorSetup(string code)
        {
            var pendingUserId = ReadPendingUserId();
            // #region agent log
            AgentDebugLog.Write("H4", "AccountController.TwoFactorSetup:post", "entry", new
            {
                hasPending = !string.IsNullOrEmpty(pendingUserId),
                hasSetupCookie = Request.Cookies.ContainsKey(TwoFactorSetupCookie)
            });
            // #endregion
            if (string.IsNullOrEmpty(pendingUserId))
                return RedirectToAction(nameof(Login));

            if (!Request.Cookies.TryGetValue(TwoFactorSetupCookie, out var setupRaw))
            {
                ViewBag.Error = "Setup session expired. Please start again from login.";
                return RedirectToAction(nameof(Login));
            }

            var setupBytes = _totpProtector.UnprotectSetupPayload(setupRaw);
            var setup = setupBytes == null ? null : TwoFactorPendingSerializer.DeserializeSetup(setupBytes);
            // #region agent log
            AgentDebugLog.Write("H4", "AccountController.TwoFactorSetup:post", "setup_cookie_state", new
            {
                setupNull = setup == null,
                userMatch = setup != null && setup.UserId == pendingUserId,
                notExpired = setup != null && setup.ExpiresUtc >= DateTimeOffset.UtcNow,
                setupSecretLen = setup?.SecretBase32?.Length ?? 0
            });
            // #endregion
            if (setup == null || setup.UserId != pendingUserId || setup.ExpiresUtc < DateTimeOffset.UtcNow)
            {
                Response.Cookies.Delete(TwoFactorSetupCookie, BuildTwoFactorCookieDeleteOptions());
                ViewBag.Error = "Setup session expired. Reload this page.";
                return RedirectToAction(nameof(TwoFactorSetup));
            }

            var rateKey = TwoFactorRateKey(pendingUserId);
            if (IsTwoFactorLockedOut(rateKey))
            {
                // #region agent log
                AgentDebugLog.Write("H5", "AccountController.TwoFactorSetup:post", "lockout_show_same_secret", new { });
                // #endregion
                return await TwoFactorSetupDisplayAsync(pendingUserId, setup.SecretBase32,
                    "Too many failed attempts. Try again later.");
            }

            if (!_totp.VerifyTotp(setup.SecretBase32, code ?? ""))
            {
                // #region agent log
                AgentDebugLog.Write("H5", "AccountController.TwoFactorSetup:post", "verify_fail_keep_same_secret", new { });
                // #endregion
                RecordTwoFactorFailure(rateKey);
                return await TwoFactorSetupDisplayAsync(pendingUserId, setup.SecretBase32,
                    "Invalid code. Enter the 6-digit code from your authenticator app.");
            }

            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserID == pendingUserId && u.IsActive);
            if (user == null)
            {
                ClearTwoFactorCookies();
                return RedirectToAction(nameof(Login));
            }

            user.TwoFactorSecret = _totpProtector.ProtectForStorage(setup.SecretBase32);
            user.TwoFactorEnabled = true;
            await _context.SaveChangesAsync();

            ClearTwoFactorFailures(rateKey);
            Response.Cookies.Delete(TwoFactorSetupCookie, BuildTwoFactorCookieDeleteOptions());
            ClearTwoFactorCookies();
            return await CompleteSignInAsync(user);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult TwoFactorCancel()
        {
            ClearTwoFactorCookies();
            Response.Cookies.Delete(TwoFactorSetupCookie, BuildTwoFactorCookieDeleteOptions());
            return RedirectToAction(nameof(Login));
        }

        /// <summary>Re-show setup UI for the same TOTP secret (cookie unchanged). Used after wrong code so the app and authenticator stay in sync.</summary>
        private async Task<IActionResult> TwoFactorSetupDisplayAsync(string pendingUserId, string secretBase32, string? errorMessage)
        {
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserID == pendingUserId && u.IsActive);
            if (user == null)
            {
                ClearTwoFactorCookies();
                return RedirectToAction(nameof(Login));
            }

            var site = await _siteConfigService.GetAsync();
            var issuer = string.IsNullOrWhiteSpace(site.ProjectName) ? "PMS" : site.ProjectName!;
            var account = user.Email ?? user.UserID;
            var uri = _totp.BuildOtpAuthUri(issuer, account, secretBase32);
            var qrPng = _totp.CreateQrCodePng(uri);
            if (!string.IsNullOrEmpty(errorMessage))
                ViewBag.Error = errorMessage;
            ViewBag.QrPngBase64 = Convert.ToBase64String(qrPng);
            ViewBag.ManualSecret = secretBase32;
            ViewBag.Issuer = issuer;
            ViewBag.Account = account;
            ViewData["Title"] = "Set up Google Authenticator";
            return View("TwoFactorSetup");
        }

        private async Task<IActionResult> CompleteSignInAsync(User user)
        {
            var session = new UserSession
            {
                SessionID = GenerateID(),
                UserID = user.UserID,
                IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                DeviceInfo = Request.Headers["User-Agent"].ToString()
            };

            _context.UserSessions.Add(session);
            await _context.SaveChangesAsync();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserID),
                new Claim(ClaimTypes.Name, user.FullName ?? ""),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim("SessionID", session.SessionID)
            };

            if (user.Role != null)
                claims.Add(new Claim(ClaimTypes.Role, user.Role.RoleName ?? ""));

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30),
                AllowRefresh = true,
                IssuedUtc = DateTimeOffset.UtcNow,
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            try
            {
                var loginLog = new ActivityLog
                {
                    UserID = user.UserID,
                    Action = "Login (MAC/Thumbprint Disabled)",
                    RefType = "User",
                    RefID = user.UserID,
                    CreatedAt = DateTime.Now
                };
                _context.ActivityLogs.Add(loginLog);
                await _context.SaveChangesAsync();
            }
            catch
            {
                // ActivityLog table may not exist
            }

            var homePerm = await _modulePermission.GetPermissionAsync(user.UserID, "Home");
            if (_modulePermission.CanRead(homePerm))
                return RedirectToAction("Coditium", "Home");
            return RedirectToAction("Workspace", "Home");
        }

        private void AppendTwoFactorPendingCookie(string userId)
        {
            var exp = DateTimeOffset.UtcNow.AddMinutes(TwoFactorPendingMinutes);
            var payload = TwoFactorPendingSerializer.SerializePending(userId, exp);
            var token = _totpProtector.ProtectPendingPayload(payload);
            Response.Cookies.Append(TwoFactorPendingCookie, token, BuildTwoFactorCookieOptions(exp));
        }

        private string? ReadPendingUserId()
        {
            if (!Request.Cookies.TryGetValue(TwoFactorPendingCookie, out var raw))
                return null;
            var bytes = _totpProtector.UnprotectPendingPayload(raw);
            if (bytes == null) return null;
            var dto = TwoFactorPendingSerializer.DeserializePending(bytes);
            if (dto == null || dto.ExpiresUtc < DateTimeOffset.UtcNow)
                return null;
            return dto.UserId;
        }

        private void ClearTwoFactorCookies()
        {
            Response.Cookies.Delete(TwoFactorPendingCookie, BuildTwoFactorCookieDeleteOptions());
        }

        private CookieOptions BuildTwoFactorCookieOptions(DateTimeOffset expires)
        {
            return new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                Expires = expires.UtcDateTime,
            };
        }

        private CookieOptions BuildTwoFactorCookieDeleteOptions()
        {
            return new CookieOptions { Path = "/", HttpOnly = true, Secure = Request.IsHttps, SameSite = SameSiteMode.Lax };
        }

        private static string TwoFactorRateKey(string userId) => $"2fa_rate_{userId}";

        private bool IsTwoFactorLockedOut(string rateKey) =>
            _memoryCache.TryGetValue(rateKey + "_lock", out _);

        private void RecordTwoFactorFailure(string rateKey)
        {
            _memoryCache.TryGetValue(rateKey, out int count);
            count++;
            _memoryCache.Set(rateKey, count, new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(10) });
            if (count >= TwoFactorMaxAttempts)
                _memoryCache.Set(rateKey + "_lock", true, TimeSpan.FromMinutes(TwoFactorLockoutMinutes));
        }

        private void ClearTwoFactorFailures(string rateKey)
        {
            _memoryCache.Remove(rateKey);
            _memoryCache.Remove(rateKey + "_lock");
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var sessionId = User.FindFirst("SessionID")?.Value;

                if (!string.IsNullOrEmpty(sessionId))
                {
                    var session = await _context.UserSessions.FindAsync(sessionId);
                    if (session != null)
                    {
                        session.LogoutTime = DateTime.Now;
                        await _context.SaveChangesAsync();
                    }
                }

                if (!string.IsNullOrEmpty(userId))
                {
                    try
                    {
                        await LogActivity(userId, "Logout", "User", userId);
                    }
                    catch
                    {
                        // ActivityLog table may not exist, continue anyway
                    }
                }
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            Response.Cookies.Delete(TwoFactorPendingCookie, BuildTwoFactorCookieDeleteOptions());
            Response.Cookies.Delete(TwoFactorSetupCookie, BuildTwoFactorCookieDeleteOptions());
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login");
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.UserSessions)
                .FirstOrDefaultAsync(u => u.UserID == userId);

            if (user == null)
            {
                return RedirectToAction("Login");
            }

            // Try to load ActivityLogs separately (table may not exist)
            try
            {
                user.ActivityLogs = await _context.ActivityLogs
                    .Where(a => a.UserID == userId)
                    .OrderByDescending(a => a.CreatedAt)
                    .Take(10)
                    .ToListAsync();
            }
            catch
            {
                // ActivityLog table doesn't exist, set to empty list
                user.ActivityLogs = new List<ActivityLog>();
            }

            return View(user);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Users()
        {
            var users = await _context.Users
                .Include(u => u.Role)
                .ToListAsync();

            var whitelistCounts = await _context.UserMacWhitelists
                .GroupBy(x => x.UserID)
                .Select(g => new { UserID = g.Key, Count = g.Count(x => x.IsActive) })
                .ToDictionaryAsync(x => x.UserID, x => x.Count);

            var blockedCounts = await _context.BlockedMacLoginAttempts
                .GroupBy(x => x.UserID)
                .Select(g => new { UserID = g.Key, Count = g.Count(x => !x.IsWhitelisted) })
                .ToDictionaryAsync(x => x.UserID, x => x.Count);

            ViewBag.WhitelistCounts = whitelistCounts;
            ViewBag.BlockedCounts = blockedCounts;
            return View(users);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult CreateUser()
        {
            ViewBag.Roles = _context.ACLs.ToList();
            SetUsersConfigViewBag();
            ViewBag.ModuleKeys = ModuleKeys;
            ViewBag.PermissionOptions = PermissionOptions;
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateUser(User user)
        {
            if (ModelState.IsValid)
            {
                user.RoleID = await ResolveRoleIdForUserTypeAsync(user.UserType, null);

                // Check if email already exists
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == user.Email);
                if (existingUser != null)
                {
                    ViewBag.Error = "Email already exists.";
                    ViewBag.Roles = _context.ACLs.ToList();
                    SetUsersConfigViewBag();
                    ViewBag.ModuleKeys = ModuleKeys;
                    ViewBag.PermissionOptions = PermissionOptions;
                    return View(user);
                }

                user.UserID = GenerateID();
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
                user.CreatedAt = DateTime.Now;
                user.IsActive = true;

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                await SaveModulePermissionsFromFormAsync(user.UserID);

                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(currentUserId))
                {
                    try
                    {
                        await LogActivity(currentUserId, "Create User", "User", user.UserID);
                    }
                    catch
                    {
                        // ActivityLog table may not exist, continue anyway
                    }
                }

                return RedirectToAction("Users");
            }

            ViewBag.Roles = _context.ACLs.ToList();
            SetUsersConfigViewBag();
            ViewBag.ModuleKeys = ModuleKeys;
            ViewBag.PermissionOptions = PermissionOptions;
            return View(user);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult UserDetails(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            return RedirectToAction(nameof(EditUser), new { id });
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditUser(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.UserSessions)
                .Include(u => u.ModulePermissions)
                .FirstOrDefaultAsync(u => u.UserID == id);

            if (user == null)
            {
                return NotFound();
            }

            // Try to load ActivityLogs separately (table may not exist)
            try
            {
                user.ActivityLogs = await _context.ActivityLogs
                    .Where(a => a.UserID == id)
                    .ToListAsync();
            }
            catch
            {
                // ActivityLog table doesn't exist, set to empty list
                user.ActivityLogs = new List<ActivityLog>();
            }

            ViewBag.Roles = _context.ACLs.ToList();
            SetUsersConfigViewBag();
            ViewBag.ModuleKeys = ModuleKeys;
            ViewBag.PermissionOptions = PermissionOptions;
            ViewBag.ModulePermissions = user.ModulePermissions.ToDictionary(p => p.ModuleKey, p => p.Permission);
            return View(user);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditUser(User user)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Check if email already exists for another user
                    var existingUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.Email == user.Email && u.UserID != user.UserID);
                    if (existingUser != null)
                    {
                        ViewBag.Error = "Email already exists for another user.";
                        ViewBag.Roles = _context.ACLs.ToList();
                        SetUsersConfigViewBag();
                        ViewBag.ModuleKeys = ModuleKeys;
                        ViewBag.PermissionOptions = PermissionOptions;
                        ViewBag.ModulePermissions = await _context.UserModulePermissions.Where(p => p.UserID == user.UserID).ToDictionaryAsync(p => p.ModuleKey, p => p.Permission);
                        return View(user);
                    }

                    var existing = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserID == user.UserID);
                    if (existing == null) return NotFound();
                    user.RoleID = await ResolveRoleIdForUserTypeAsync(user.UserType, existing.RoleID);
                    user.PasswordHash = existing.PasswordHash;
                    user.CreatedAt = existing.CreatedAt;
                    user.TwoFactorEnabled = existing.TwoFactorEnabled;
                    user.TwoFactorSecret = existing.TwoFactorSecret;
                    _context.Users.Update(user);
                    await _context.SaveChangesAsync();

                    await SaveModulePermissionsFromFormAsync(user.UserID);

                    var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (!string.IsNullOrEmpty(currentUserId))
                    {
                        try
                        {
                            await LogActivity(currentUserId, "Update User", "User", user.UserID);
                        }
                        catch
                        {
                            // ActivityLog table may not exist, continue anyway
                        }
                    }

                    TempData["Success"] = "User updated successfully.";
                    return RedirectToAction("Users");
                }
                catch (Exception)
                {
                    ViewBag.Error = "An error occurred while updating the user.";
                }
            }

            ViewBag.Roles = _context.ACLs.ToList();
            SetUsersConfigViewBag();
            ViewBag.ModuleKeys = ModuleKeys;
            ViewBag.PermissionOptions = PermissionOptions;
            ViewBag.ModulePermissions = await _context.UserModulePermissions.Where(p => p.UserID == user.UserID).ToDictionaryAsync(p => p.ModuleKey, p => p.Permission);
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ResetUserPassword(string userId, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrEmpty(userId))
                return NotFound();
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound();
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            {
                TempData["ResetPasswordError"] = "Password must be at least 6 characters.";
                return RedirectToAction(nameof(EditUser), new { id = userId });
            }
            if (newPassword != confirmPassword)
            {
                TempData["ResetPasswordError"] = "Passwords do not match.";
                return RedirectToAction(nameof(EditUser), new { id = userId });
            }
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Password has been reset successfully.";
            return RedirectToAction(nameof(EditUser), new { id = userId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ResetTwoFactor(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return NotFound();
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound();
            user.TwoFactorEnabled = false;
            user.TwoFactorSecret = null;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Authenticator (2FA) has been reset for this user. They must register again on next login if 2FA is required.";
            return RedirectToAction(nameof(EditUser), new { id = userId });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ToggleUserStatus(string userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found." });
                }

                user.IsActive = !user.IsActive;
                await _context.SaveChangesAsync();

                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(currentUserId))
                {
                    try
                    {
                        await LogActivity(currentUserId, 
                            user.IsActive ? "Activate User" : "Deactivate User", 
                            "User", user.UserID);
                    }
                    catch
                    {
                        // ActivityLog table may not exist, continue anyway
                    }
                }

                return Json(new { 
                    success = true, 
                    message = $"User {(user.IsActive ? "activated" : "deactivated")} successfully.",
                    isActive = user.IsActive
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }

        private async Task SaveModulePermissionsFromFormAsync(string userId)
        {
            var existing = await _context.UserModulePermissions.Where(p => p.UserID == userId).ToListAsync();
            _context.UserModulePermissions.RemoveRange(existing);
            foreach (var key in ModuleKeys)
            {
                var val = Request.Form["ModulePermissions_" + key].FirstOrDefault();
                if (string.IsNullOrEmpty(val)) val = "NoAccess";
                if (val != "NoAccess" && val != "Read" && val != "Author" && val != "Edit" && val != "Admin") val = "NoAccess";
                _context.UserModulePermissions.Add(new UserModulePermission
                {
                    UserID = userId,
                    ModuleKey = key,
                    Permission = val
                });
            }
            await _context.SaveChangesAsync();
        }

        private async Task<string?> ResolveRoleIdForUserTypeAsync(string? userType, string? fallbackRoleId)
        {
            if (string.IsNullOrWhiteSpace(userType))
            {
                return fallbackRoleId;
            }

            var normalized = userType.Trim();
            var mappedRoleId = await _context.ACLs
                .Where(r => r.RoleName != null && r.RoleName.ToLower() == normalized.ToLower())
                .Select(r => r.RoleID)
                .FirstOrDefaultAsync();

            return string.IsNullOrWhiteSpace(mappedRoleId) ? fallbackRoleId : mappedRoleId;
        }

        private void SetUsersConfigViewBag()
        {
            var deptConfig = _context.Configurations.FirstOrDefault(c => c.ConfigKey == "departments" && (c.Category == "Users" || c.Category == null));
            ViewBag.Departments = deptConfig?.ConfigValue != null
                ? deptConfig.ConfigValue.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList()
                : new List<string>();
            var desigConfig = _context.Configurations.FirstOrDefault(c => c.ConfigKey == "designations" && (c.Category == "Users" || c.Category == null));
            ViewBag.Designations = desigConfig?.ConfigValue != null
                ? desigConfig.ConfigValue.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList()
                : new List<string>();
        }

        private async Task LogActivity(string userId, string action, string refType, string refId)
        {
            try
            {
                var activityLog = new ActivityLog
                {
                    UserID = userId,
                    Action = action,
                    RefType = refType,
                    RefID = refId,
                    CreatedAt = DateTime.Now
                };

                _context.ActivityLogs.Add(activityLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                // ActivityLog table doesn't exist or other error occurred
                // Silently ignore - this is called from try-catch blocks in calling methods
                // Do not rethrow the exception
            }
        }

        private string GenerateID()
        {
            return Guid.NewGuid().ToString("N")[..10].ToUpper();
        }

        // Temporary endpoint to create admin user - Remove after use
        [HttpGet]
        public async Task<IActionResult> CreateAdminUser()
        {
            try
            {
                // Ensure Admin role exists
                var adminRole = await _context.ACLs.FirstOrDefaultAsync(r => r.RoleID == "ADMIN001");
                if (adminRole == null)
                {
                    adminRole = new ACL
                    {
                        RoleID = "ADMIN001",
                        RoleName = "Admin",
                        Permissions = "All"
                    };
                    _context.ACLs.Add(adminRole);
                    await _context.SaveChangesAsync();
                }

                // Check if user already exists
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "abbas@pms.com");
                if (existingUser != null)
                {
                    // Update existing user
                    existingUser.FullName = "Abbas";
                    existingUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123");
                    existingUser.RoleID = "ADMIN001";
                    existingUser.IsActive = true;
                    await _context.SaveChangesAsync();
                    return Content($"User updated successfully! Email: abbas@pms.com, Password: Admin@123, UserID: {existingUser.UserID}");
                }

                // Generate unique UserID
                string userId = "USER00002";
                int attempts = 0;
                while (await _context.Users.AnyAsync(u => u.UserID == userId) && attempts < 100)
                {
                    attempts++;
                    var random = new Random();
                    userId = "USER" + random.Next(10000, 99999).ToString();
                }

                // Create the user
                var user = new User
                {
                    UserID = userId,
                    FullName = "Abbas",
                    Email = "abbas@pms.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                    RoleID = "ADMIN001",
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                return Content($"User created successfully! Email: abbas@pms.com, Password: Admin@123, UserID: {userId}");
            }
            catch (Exception ex)
            {
                return Content($"Error creating user: {ex.Message}<br/>Stack Trace: {ex.StackTrace}");
            }
        }

        // Temporary debug endpoint to list all users - Remove after use
        [HttpGet]
        public async Task<IActionResult> ListUsers()
        {
            try
            {
                var users = await _context.Users
                    .Include(u => u.Role)
                    .ToListAsync();

                var html = "<h2>All Users in Database:</h2><table border='1' cellpadding='5'><tr><th>UserID</th><th>Email</th><th>FullName</th><th>Role</th><th>IsActive</th></tr>";
                
                foreach (var user in users)
                {
                    html += $"<tr><td>{user.UserID}</td><td>{user.Email}</td><td>{user.FullName}</td><td>{user.Role?.RoleName ?? "No Role"}</td><td>{user.IsActive}</td></tr>";
                }
                
                html += "</table>";
                return Content(html, "text/html");
            }
            catch (Exception ex)
            {
                return Content($"Error listing users: {ex.Message}");
            }
        }
    }
}
