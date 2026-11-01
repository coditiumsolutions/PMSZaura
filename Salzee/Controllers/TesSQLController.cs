using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Salzee.Data;
using Salzee.Services;
using System.Data;
using System.Security.Claims;

namespace Salzee.Controllers
{
    /// <summary>
    /// Mini SQL runner for MS SQL Server - run queries and view table data.
    /// </summary>
    [Authorize]
    public class TesSQLController : Controller
    {
        private const string ModuleKey = "TesSQL";
        private readonly PMSDbContext _context;
        private readonly IModulePermissionService _modulePermission;

        public TesSQLController(PMSDbContext context, IModulePermissionService modulePermission)
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

        public async Task<IActionResult> Index()
        {
            var denied = await EnsurePermissionAsync("Admin");
            if (denied != null) return denied;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Execute([FromBody] TesSQLExecuteRequest request, CancellationToken cancellationToken = default)
        {
            var denied = await EnsurePermissionAsync("Admin");
            if (denied != null) return denied;
            if (request?.Sql == null || string.IsNullOrWhiteSpace(request.Sql))
            {
                return Json(new { success = false, error = "SQL query is required." });
            }

            var sql = request.Sql.Trim();
            var isSelect = sql.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) ||
                           sql.StartsWith("WITH", StringComparison.OrdinalIgnoreCase); // CTE

            try
            {
                var conn = _context.Database.GetDbConnection();
                await conn.OpenAsync(cancellationToken);

                try
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = sql;
                    cmd.CommandTimeout = 120;

                    if (isSelect)
                    {
                        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                        var columns = new List<string>();
                        for (var i = 0; i < reader.FieldCount; i++)
                            columns.Add(reader.GetName(i));

                        var rows = new List<Dictionary<string, object>>();
                        while (await reader.ReadAsync(cancellationToken))
                        {
                            var row = new Dictionary<string, object>();
                            for (var i = 0; i < reader.FieldCount; i++)
                            {
                                var name = reader.GetName(i);
                                row[name] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                            }
                            rows.Add(row);
                        }

                        return Json(new
                        {
                            success = true,
                            isSelect = true,
                            columns,
                            rows,
                            rowCount = rows.Count
                        });
                    }
                    else
                    {
                        var affected = await cmd.ExecuteNonQueryAsync(cancellationToken);
                        return Json(new
                        {
                            success = true,
                            isSelect = false,
                            message = $"Command executed successfully. Rows affected: {affected}."
                        });
                    }
                }
                finally
                {
                    if (conn.State == ConnectionState.Open)
                        await conn.CloseAsync();
                }
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    error = ex.Message
                });
            }
        }

        public class TesSQLExecuteRequest
        {
            public string Sql { get; set; } = "";
        }
    }
}
