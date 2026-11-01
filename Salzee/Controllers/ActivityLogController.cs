using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Salzee.Data;
using Salzee.Models;
using Salzee.Services;
using System.Data;
using System.Security.Claims;

namespace Salzee.Controllers
{
    [Authorize]
    public class ActivityLogController : Controller
    {
        private const string ModuleKey = "ActivityLog";
        private readonly PMSDbContext _context;
        private readonly IModulePermissionService _modulePermission;

        public ActivityLogController(PMSDbContext context, IModulePermissionService modulePermission)
        {
            _context = context;
            _modulePermission = modulePermission;
        }

        private async Task<IActionResult?> EnsurePermissionAsync(string requiredLevel)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var perm = await _modulePermission.GetPermissionAsync(userId, ModuleKey);
            if (requiredLevel == "Read" && !_modulePermission.CanRead(perm))
                return RedirectToAction("AccessDenied", "Account");
            if (requiredLevel == "Edit" && !_modulePermission.CanEdit(perm))
                return RedirectToAction("AccessDenied", "Account");
            if (requiredLevel == "Admin" && !_modulePermission.CanDelete(perm))
                return RedirectToAction("AccessDenied", "Account");
            ViewBag.CanCreate = _modulePermission.CanEdit(perm);
            ViewBag.CanEdit = _modulePermission.CanEdit(perm);
            ViewBag.CanDelete = _modulePermission.CanDelete(perm);
            return null;
        }

        [HttpGet]
        public async Task<IActionResult> Index(DateTime? dateFrom, DateTime? dateTo, string? userId, int page = 1, int pageSize = 50)
        {
            var denied = await EnsurePermissionAsync("Read");
            if (denied != null) return denied;
            if (page < 1) page = 1;
            if (pageSize < 10) pageSize = 10;
            if (pageSize > 200) pageSize = 200;

            var whereClauses = new List<string>();
            var parameters = new List<object>();
            var paramIndex = 0;

            if (dateFrom.HasValue)
            {
                whereClauses.Add($"CreatedAt >= @p{paramIndex}");
                parameters.Add(dateFrom.Value.Date);
                paramIndex++;
            }
            if (dateTo.HasValue)
            {
                whereClauses.Add($"CreatedAt < @p{paramIndex}");
                parameters.Add(dateTo.Value.Date.AddDays(1));
                paramIndex++;
            }
            if (!string.IsNullOrWhiteSpace(userId))
            {
                whereClauses.Add($"UserID = @p{paramIndex}");
                parameters.Add(userId.Trim());
                paramIndex++;
            }

            var whereSql = whereClauses.Count > 0 ? " WHERE " + string.Join(" AND ", whereClauses) : "";

            var conn = _context.Database.GetDbConnection();
            await conn.OpenAsync();

            try
            {
                var totalCount = 0;
                using (var cmdCount = conn.CreateCommand())
                {
                    cmdCount.CommandText = "SELECT COUNT(*) FROM ActivityLog" + whereSql;
                    for (var i = 0; i < parameters.Count; i++)
                    {
                        var p = cmdCount.CreateParameter();
                        p.ParameterName = $"@p{i}";
                        p.Value = parameters[i];
                        cmdCount.Parameters.Add(p);
                    }
                    var obj = await cmdCount.ExecuteScalarAsync();
                    totalCount = obj != null && obj != DBNull.Value ? Convert.ToInt32(obj) : 0;
                }

                var totalPages = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)pageSize);
                if (page > totalPages) page = totalPages;
                var offset = (page - 1) * pageSize;

                var logs = new List<ActivityLogRow>();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = $@"
SELECT a.LogID, a.CreatedAt, a.UserID, a.Action, a.RefType, a.RefID, u.FullName, a.Details
FROM ActivityLog a
LEFT JOIN Users u ON a.UserID = u.UserID
{whereSql}
ORDER BY a.CreatedAt DESC, a.LogID DESC
OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY";
                    for (var i = 0; i < parameters.Count; i++)
                    {
                        var p = cmd.CreateParameter();
                        p.ParameterName = $"@p{i}";
                        p.Value = parameters[i];
                        cmd.Parameters.Add(p);
                    }

                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        logs.Add(new ActivityLogRow
                        {
                            LogId = reader.GetInt32(0),
                            CreatedAt = reader.IsDBNull(1) ? default : reader.GetDateTime(1),
                            UserId = reader.IsDBNull(2) ? null : reader.GetString(2),
                            UserName = reader.IsDBNull(6) || string.IsNullOrEmpty(reader.GetString(6)) ? (reader.IsDBNull(2) ? "—" : reader.GetString(2)) : reader.GetString(6),
                            Action = reader.IsDBNull(3) ? null : reader.GetString(3),
                            RefType = reader.IsDBNull(4) ? null : reader.GetString(4),
                            RefId = reader.IsDBNull(5) ? null : reader.GetString(5),
                            Details = !reader.IsDBNull(7) ? reader.GetString(7) : null
                        });
                    }
                }

                var usersForFilter = await _context.ActivityLogs
                    .Where(a => a.UserID != null)
                    .Select(a => a.UserID!)
                    .Distinct()
                    .Join(_context.Users, uid => uid, u => u.UserID, (uid, u) => new UserOption { UserId = uid, Label = u.FullName ?? uid })
                    .OrderBy(x => x.Label)
                    .ToListAsync();

                var vm = new ActivityLogIndexViewModel
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = totalPages,
                    Logs = logs,
                    Users = usersForFilter,
                    Actions = new List<string>(),
                    RefTypes = new List<string>(),
                    UserId = userId,
                    FromDate = dateFrom,
                    ToDate = dateTo
                };

                return View(vm);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                    await conn.CloseAsync();
            }
        }

        [HttpGet]
        public async Task<IActionResult> AuditLogs(string? btNo, int page = 1, int pageSize = 50)
        {
            var denied = await EnsurePermissionAsync("Read");
            if (denied != null) return denied;

            if (page < 1) page = 1;
            if (pageSize < 10) pageSize = 10;
            if (pageSize > 200) pageSize = 200;

            var term = (btNo ?? string.Empty).Trim();
            var vm = new ActivityLogIndexViewModel
            {
                BtNo = term,
                HasSearched = term.Length > 0,
                Page = page,
                PageSize = pageSize,
                TotalPages = 1
            };

            if (term.Length == 0)
                return View(vm);

            var relatedIds = await FindRelatedActivityRefIdsAsync(term);
            var logsQuery = _context.ActivityLogs.AsNoTracking()
                .Where(a =>
                    (a.Action != null && a.Action.Contains(term))
                    || (a.Details != null && a.Details.Contains(term))
                    || (a.RefID != null && a.RefID.Contains(term))
                    || (a.RefType != null && a.RefType.Contains(term))
                    || (a.RefID != null && relatedIds.Contains(a.RefID)));

            vm.TotalCount = await logsQuery.CountAsync();
            vm.TotalPages = vm.TotalCount == 0 ? 1 : (int)Math.Ceiling(vm.TotalCount / (double)pageSize);
            if (vm.Page > vm.TotalPages) vm.Page = vm.TotalPages;

            vm.Logs = await logsQuery
                .OrderByDescending(a => a.CreatedAt)
                .ThenByDescending(a => a.LogID)
                .Skip((vm.Page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new ActivityLogRow
                {
                    LogId = a.LogID,
                    CreatedAt = a.CreatedAt,
                    UserId = a.UserID,
                    UserName = a.User != null && a.User.FullName != null && a.User.FullName != ""
                        ? a.User.FullName
                        : (a.UserID ?? "—"),
                    Action = a.Action,
                    RefType = a.RefType,
                    RefId = a.RefID,
                    Details = a.Details
                })
                .ToListAsync();

            return View(vm);
        }

        private async Task<List<string>> FindRelatedActivityRefIdsAsync(string term)
        {
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            void AddId(string? value)
            {
                if (string.IsNullOrWhiteSpace(value)) return;
                ids.Add(value);
                ids.Add(value.Trim());
            }

            AddId(term);

            var customers = await _context.Customers.AsNoTracking()
                .Where(c => c.CustomerID == term || (c.FormNo != null && c.FormNo.Trim() == term))
                .Select(c => c.CustomerID)
                .ToListAsync();

            var customerIds = customers
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id!)
                .Distinct()
                .ToList();

            foreach (var customerId in customerIds)
                AddId(customerId);

            if (customerIds.Count == 0 || ids.Count > 500)
                return ids.Take(500).ToList();

            foreach (var id in await _context.Allotments.AsNoTracking()
                         .Where(a => a.CustomerID != null && customerIds.Contains(a.CustomerID))
                         .Select(a => a.AllotmentID)
                         .ToListAsync())
                AddId(id);

            foreach (var id in await _context.Payments.AsNoTracking()
                         .Where(p => p.CustomerID != null && customerIds.Contains(p.CustomerID))
                         .Select(p => p.PaymentID)
                         .ToListAsync())
                AddId(id);

            foreach (var id in await _context.Transfers.AsNoTracking()
                         .Where(t => customerIds.Contains(t.CustomerID))
                         .Select(t => t.TransferID)
                         .ToListAsync())
                AddId(id);

            foreach (var id in await _context.Refunds.AsNoTracking()
                         .Where(r => r.CustomerID != null && customerIds.Contains(r.CustomerID))
                         .Select(r => r.RefundID)
                         .ToListAsync())
                AddId(id);

            foreach (var id in await _context.Possessions.AsNoTracking()
                         .Where(p => p.CustomerID != null && customerIds.Contains(p.CustomerID))
                         .Select(p => p.PossessionID)
                         .ToListAsync())
                AddId(id);

            foreach (var id in await _context.NDCs.AsNoTracking()
                         .Where(n => n.CustomerID != null && customerIds.Contains(n.CustomerID))
                         .Select(n => n.NDCID)
                         .ToListAsync())
                AddId(id);

            foreach (var id in await _context.Waivers.AsNoTracking()
                         .Where(w => w.CustomerID != null && customerIds.Contains(w.CustomerID))
                         .Select(w => w.WaiverID)
                         .ToListAsync())
                AddId(id);

            foreach (var id in await _context.Penalties.AsNoTracking()
                         .Where(p => p.CustomerID != null && customerIds.Contains(p.CustomerID))
                         .Select(p => p.PenaltyID)
                         .ToListAsync())
                AddId(id);

            foreach (var id in await _context.DuplicateFileTransfers.AsNoTracking()
                         .Where(d => customerIds.Contains(d.CustomerID))
                         .Select(d => d.Id)
                         .ToListAsync())
                AddId(id);

            return ids.Take(500).ToList();
        }

        /// <summary>Blocking logs list with search filters (CustomerID, UserID, date range, NewStatus).</summary>
        [HttpGet]
        public async Task<IActionResult> BlockingLogs(string? customerId, string? userId, DateTime? dateFrom, DateTime? dateTo, string? newStatus, int page = 1, int pageSize = 25)
        {
            var denied = await EnsurePermissionAsync("Read");
            if (denied != null) return denied;
            if (page < 1) page = 1;
            if (pageSize < 10) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var query = _context.BlockingLogs
                .Include(b => b.Customer)
                .Include(b => b.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(customerId))
            {
                var cid = customerId.Trim();
                query = query.Where(b => b.CustomerID != null && b.CustomerID.Contains(cid));
            }
            if (!string.IsNullOrWhiteSpace(userId))
            {
                var uid = userId.Trim();
                query = query.Where(b => b.UserID != null && b.UserID.Contains(uid));
            }
            if (dateFrom.HasValue)
                query = query.Where(b => b.ActionDate >= dateFrom.Value.Date);
            if (dateTo.HasValue)
                query = query.Where(b => b.ActionDate < dateTo.Value.Date.AddDays(1));
            if (!string.IsNullOrWhiteSpace(newStatus))
            {
                var status = newStatus.Trim();
                query = query.Where(b => b.NewStatus != null && b.NewStatus == status);
            }

            var totalCount = await query.CountAsync();
            var totalPages = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)pageSize);
            if (page > totalPages) page = totalPages;

            var list = await query
                .OrderByDescending(b => b.ActionDate)
                .ThenByDescending(b => b.BlockingLogID)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CustomerId = customerId ?? "";
            ViewBag.UserId = userId ?? "";
            ViewBag.DateFrom = dateFrom?.ToString("yyyy-MM-dd") ?? "";
            ViewBag.DateTo = dateTo?.ToString("yyyy-MM-dd") ?? "";
            ViewBag.NewStatus = newStatus ?? "";
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCount = totalCount;
            ViewBag.TotalPages = totalPages;
            ViewBag.List = list;
            return View();
        }
    }
}
