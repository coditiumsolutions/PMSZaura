using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMS.Services;

namespace PMS.Controllers
{
    [Authorize]
    public class ReportController : Controller
    {
        private const string CustomerModuleKey = "Customer";

        private readonly IAccountStatementReportService _accountStatementReportService;
        private readonly IModulePermissionService _modulePermission;

        public ReportController(
            IAccountStatementReportService accountStatementReportService,
            IModulePermissionService modulePermission)
        {
            _accountStatementReportService = accountStatementReportService;
            _modulePermission = modulePermission;
        }

        /// <summary>
        /// Renders Account Statement as PDF using the same data as /Customer/AccountStatement/{accountNo}.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> AccountStatement(string accountNo, CancellationToken cancellationToken)
        {
            var denied = await EnsureCustomerReadAsync();
            if (denied != null) return denied;

            if (string.IsNullOrWhiteSpace(accountNo))
                return NotFound();

            byte[]? pdf;
            try
            {
                pdf = await _accountStatementReportService.RenderPdfAsync(accountNo.Trim(), cancellationToken);
            }
            catch (FileNotFoundException ex)
            {
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }

            if (pdf == null || pdf.Length == 0)
                return NotFound();

            var fileName = $"AccountStatement_{accountNo.Trim()}.pdf";
            Response.Headers["Content-Disposition"] = $"inline; filename=\"{fileName}\"";
            return File(pdf, "application/pdf");
        }

        private async Task<IActionResult?> EnsureCustomerReadAsync()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var perm = await _modulePermission.GetPermissionAsync(userId, CustomerModuleKey);
            if (!_modulePermission.CanRead(perm))
                return RedirectToAction("AccessDenied", "Account");
            return null;
        }
    }
}
