using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PMS.Services;

namespace PMS.Controllers
{
    [Authorize]
    public class ReportController : Controller
    {
        private const string CustomerModuleKey = "Customer";

        private readonly IReportServiceClient _reportService;
        private readonly IAccountStatementReportService _localAccountStatementReport;
        private readonly ReportServiceOptions _reportOptions;
        private readonly IModulePermissionService _modulePermission;
        private readonly ILogger<ReportController> _logger;

        public ReportController(
            IReportServiceClient reportService,
            IAccountStatementReportService localAccountStatementReport,
            IOptions<ReportServiceOptions> reportOptions,
            IModulePermissionService modulePermission,
            ILogger<ReportController> logger)
        {
            _reportService = reportService;
            _localAccountStatementReport = localAccountStatementReport;
            _reportOptions = reportOptions.Value;
            _modulePermission = modulePermission;
            _logger = logger;
        }

        /// <summary>
        /// Account Statement PDF: Coditium Python report service and/or in-process RDLC.
        /// On hosts without the Python API (e.g. Zaura GCP), PreferLocalRdlc avoids a long timeout.
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
                var pdf = await TryGetAccountStatementPdfAsync(id, cancellationToken);
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Account statement PDF failed for {CustomerId}", id);
                return Problem(
                    detail: "Unable to generate the account statement PDF. Please try again later.",
                    statusCode: StatusCodes.Status502BadGateway);
            }
        }

        private async Task<byte[]?> TryGetAccountStatementPdfAsync(string id, CancellationToken cancellationToken)
        {
            if (_reportOptions.PreferLocalRdlc)
            {
                var local = await _localAccountStatementReport.RenderPdfAsync(id, cancellationToken);
                if (local != null && local.Length > 0)
                    return local;
            }

            try
            {
                // Do not pass the request CancellationToken into the external call's wait chain for
                // timeout-only failures; HttpClient timeout still applies via configured TimeoutSeconds.
                return await _reportService.GetAccountStatementPdfAsync(id, cancellationToken);
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex) when (IsExternalReportServiceFailure(ex, cancellationToken))
            {
                _logger.LogWarning(
                    ex,
                    "External report service unavailable for {CustomerId}; using in-process RDLC fallback.",
                    id);
                return await _localAccountStatementReport.RenderPdfAsync(id, cancellationToken);
            }
        }

        private static bool IsExternalReportServiceFailure(Exception ex, CancellationToken cancellationToken)
        {
            if (ex is HttpRequestException || ex is InvalidOperationException)
                return true;

            // HttpClient timeout surfaces as TaskCanceledException / OperationCanceledException.
            if (ex is OperationCanceledException && !cancellationToken.IsCancellationRequested)
                return true;

            return false;
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
