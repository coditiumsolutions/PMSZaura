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

        private readonly IReportServiceClient _reportService;
        private readonly IModulePermissionService _modulePermission;
        private readonly ILogger<ReportController> _logger;

        public ReportController(
            IReportServiceClient reportService,
            IModulePermissionService modulePermission,
            ILogger<ReportController> logger)
        {
            _reportService = reportService;
            _modulePermission = modulePermission;
            _logger = logger;
        }

        /// <summary>
        /// Proxies Account Statement PDF from the Coditium Python report service.
        /// API: GET /api/reports/account-statement?customerId=…
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> AccountStatement(string accountNo, string? customerId, CancellationToken cancellationToken)
        {
            var denied = await EnsureCustomerReadAsync();
            if (denied != null) return denied;

            var id = (customerId ?? accountNo)?.Trim();
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            try
            {
                var pdf = await _reportService.GetAccountStatementPdfAsync(id, cancellationToken);
                if (pdf == null || pdf.Length == 0)
                    return NotFound();

                var fileName = $"AccountStatement_{id}.pdf";
                Response.Headers["Content-Disposition"] = $"inline; filename=\"{fileName}\"";
                return File(pdf, "application/pdf");
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Account statement PDF unavailable for {CustomerId}", id);
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status502BadGateway);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Report service unreachable for {CustomerId}", id);
                return Problem(
                    detail: "Report service is unreachable. Please try again later.",
                    statusCode: StatusCodes.Status502BadGateway);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Report service timed out for {CustomerId}", id);
                return Problem(
                    detail: "Report service timed out while generating the PDF.",
                    statusCode: StatusCodes.Status504GatewayTimeout);
            }
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
