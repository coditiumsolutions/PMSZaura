using System.Globalization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Reporting.NETCore;
using PMS.Models;
using PMS.ReportModels;

namespace PMS.Services
{
    public interface IAccountStatementReportService
    {
        Task<byte[]?> RenderPdfAsync(string accountNo, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Maps existing Account Statement data into RDLC datasets and renders PDF via ReportViewerCore.
    /// Does not run its own SQL ΓÇö delegates loading to <see cref="IAccountStatementService"/>.
    /// </summary>
    public class AccountStatementReportService : IAccountStatementReportService
    {
        private static readonly CultureInfo EnUs = CultureInfo.GetCultureInfo("en-US");

        private readonly IAccountStatementService _accountStatementService;
        private readonly ISiteConfigService _siteConfigService;
        private readonly IWebHostEnvironment _env;

        public AccountStatementReportService(
            IAccountStatementService accountStatementService,
            ISiteConfigService siteConfigService,
            IWebHostEnvironment env)
        {
            _accountStatementService = accountStatementService;
            _siteConfigService = siteConfigService;
            _env = env;
        }

        public async Task<byte[]?> RenderPdfAsync(string accountNo, CancellationToken cancellationToken = default)
        {
            var data = await _accountStatementService.GetAsync(accountNo, cancellationToken);
            if (data == null)
                return null;

            var (header, lines) = await BuildReportModelsAsync(data);
            var reportPath = ResolveReportPath();
            if (!File.Exists(reportPath))
                throw new FileNotFoundException($"RDLC report not found at '{reportPath}'.");

            using var report = new LocalReport();
            await using (var fs = File.OpenRead(reportPath))
            {
                report.LoadReportDefinition(fs);
            }

            report.EnableExternalImages = true;
            report.DataSources.Clear();
            report.DataSources.Add(new ReportDataSource("AccountStatementHeader", new[] { header }));
            report.DataSources.Add(new ReportDataSource("AccountStatementLines", lines));

            return report.Render("PDF");
        }

        private async Task<(AccountStatementReportModel Header, List<AccountStatementLineReportModel> Lines)> BuildReportModelsAsync(
            AccountStatementData data)
        {
            var customer = data.Customer;
            var site = await _siteConfigService.GetAsync();

            var jointOwnerNames = (customer.JointOwners ?? new List<JointOwner>())
                .Select(j => j.JointOwnerName?.Trim())
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .ToList();
            var customerName = jointOwnerNames.Any()
                ? string.Join(" / ", (new[] { customer.FullName }).Concat(
                    jointOwnerNames.Where(n => !string.Equals(n, customer.FullName, StringComparison.OrdinalIgnoreCase))))
                : (customer.FullName ?? "-");

            var allotment = customer.Allotments?
                .FirstOrDefault(a => string.Equals(a.WorkFlowStatus, "Approved", StringComparison.OrdinalIgnoreCase));
            var property = allotment?.Property;

            // Customer info (Address/Phone/Mobiles)
            var customerAddress = !string.IsNullOrWhiteSpace(customer.MailingAddress)
                ? customer.MailingAddress!.Trim()
                : (!string.IsNullOrWhiteSpace(customer.PermanentAddress) ? customer.PermanentAddress!.Trim() : "-");
            var customerPhone = !string.IsNullOrWhiteSpace(customer.Phone) ? customer.Phone!.Trim() : "-";
            var customerMobile = !string.IsNullOrWhiteSpace(customer.MobileNo) ? customer.MobileNo!.Trim() : "-";

            // Project info from Property (preferred) with fallbacks.
            var subProject = property?.SubProject
                ?? customer.SubProject
                ?? customer.PaymentPlan?.SubProject
                ?? "-";
            var unit = property != null
                ? $"{(string.IsNullOrWhiteSpace(property.PlotNo) ? "-" : property.PlotNo)}, {(string.IsNullOrWhiteSpace(property.Block) ? "-" : property.Block)}{(string.IsNullOrWhiteSpace(property.Street) ? string.Empty : ", Street " + property.Street)}"
                : "-";
            var registeredSize = property?.Size
                ?? customer.RegisteredSize
                ?? customer.PaymentPlan?.RegisteredSize
                ?? "-";
            var propertyType = property?.PropertyType ?? property?.PlotType ?? "-";
            var paymentPlanId = customer.PaymentPlan?.PlanName ?? "-";

            var plotNumber = property != null
                ? $"{property.PlotNo}{(string.IsNullOrWhiteSpace(property.Block) ? string.Empty : ", " + property.Block)}"
                : "-";

            var bookingDate = allotment?.AllotmentDate
                ?? customer.CreatedAt;
            var accountNo = customer.CustomerID?.Trim() ?? string.Empty;

            var schedules = customer.PaymentPlan?.PaymentSchedules?
                .OrderBy(ps => ps.InstallmentNo ?? int.MaxValue)
                .ToList() ?? new List<PaymentSchedule>();

            var lines = new List<AccountStatementLineReportModel>();
            decimal grandTotal = 0m;
            decimal totalPaid = 0m;
            decimal outstanding = 0m;
            var customerId = customer.CustomerID?.Trim();

            foreach (var sch in schedules)
            {
                var accountHead = string.IsNullOrWhiteSpace(sch.PaymentDescription)
                    ? "Installment"
                    : sch.PaymentDescription.Trim();

                // Match Due/Paid by ScheduleID + CustomerID (not AccountHead text).
                var scheduleId = sch.ScheduleID?.Trim();
                var payments = (sch.Payments ?? new List<Payment>())
                    .Where(p => !string.IsNullOrWhiteSpace(scheduleId)
                        && string.Equals(p.ScheduleID?.Trim(), scheduleId, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(p.CustomerID?.Trim(), customerId, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(p.AuditStatus, "Approved", StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(p.Status, "Pending", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                var paidAmount = Math.Round(payments.Sum(p => p.Amount), 0, MidpointRounding.AwayFromZero);
                var balance = Math.Round(Math.Max(0m, sch.Amount - paidAmount), 0, MidpointRounding.AwayFromZero);
                var status = ResolveStatus(paidAmount, balance, payments);
                var latestPayment = payments
                    .OrderByDescending(p => p.PaymentDate)
                    .ThenByDescending(p => p.PaymentID)
                    .FirstOrDefault();

                grandTotal += sch.Amount;
                totalPaid += paidAmount;
                outstanding += balance;

                lines.Add(new AccountStatementLineReportModel
                {
                    InstallmentNo = sch.InstallmentNo,
                    InstallmentNoText = (sch.InstallmentNo ?? 0) == 0 ? string.Empty : sch.InstallmentNo!.Value.ToString(EnUs),
                    DueDate = sch.DueDate.ToString("dd-MMM-yyyy", EnUs),
                    InstallmentAmount = sch.Amount,
                    PaidAmount = paidAmount,
                    Balance = balance,
                    InstallmentAmountText = sch.Amount.ToString("N0", EnUs),
                    PaidAmountText = paidAmount.ToString("N0", EnUs),
                    BalanceText = balance.ToString("N0", EnUs),
                    Surcharge = 0m,
                    SurchargeText = "0",
                    PaymentDateText = latestPayment != null ? latestPayment.PaymentDate.ToString("dd-MMM-yyyy", EnUs) : "-",
                    ReferenceNumber = !string.IsNullOrWhiteSpace(latestPayment?.ReferenceNo) ? latestPayment.ReferenceNo!.Trim() : "-",
                    Status = status,
                    AccountHead = sch.InstallmentNo.HasValue
                        ? $"{accountHead} Inst No {sch.InstallmentNo.Value.ToString(EnUs)}"
                        : accountHead
                });
            }

            var header = new AccountStatementReportModel
            {
                CompanyName = string.Empty,
                ReportTitle = "Account Statement",
                CustomerName = customerName,
                PlotNumber = plotNumber,
                CustomerCode = accountNo,
                AccountNumber = accountNo,
                CustomerAddress = customerAddress,
                CustomerPhone = customerPhone,
                CustomerMobile = customerMobile,
                CustomerMobile2 = string.Empty,
                BookingDate = bookingDate.ToString("dd-MMM-yyyy", EnUs),
                StatementDate = DateTime.Now.ToString("dd-MMM-yyyy", EnUs),
                ProjectName = customer.PaymentPlan?.Project?.ProjectName
                    ?? customer.Project?.ProjectName
                    ?? "-",
                SubProject = subProject,
                Unit = unit,
                RegisteredSize = registeredSize,
                PropertyType = propertyType,
                PaymentPlanId = paymentPlanId,
                GrandTotal = grandTotal,
                TotalPaid = totalPaid,
                OutstandingBalance = outstanding,
                GrandTotalText = Math.Round(grandTotal, 0, MidpointRounding.AwayFromZero).ToString("N0", EnUs),
                TotalPaidText = Math.Round(totalPaid, 0, MidpointRounding.AwayFromZero).ToString("N0", EnUs),
                OutstandingBalanceText = Math.Round(outstanding, 0, MidpointRounding.AwayFromZero).ToString("N0", EnUs),
                CompanyLogo = await LoadLogoBytesAsync(site.LogoPath) ?? Array.Empty<byte>()
            };

            return (header, lines);
        }

        private static string ResolveStatus(decimal paidAmount, decimal balance, List<Payment> payments)
        {
            if (paidAmount <= 0m)
                return "Unpaid";
            if (balance <= 0m)
                return "Paid";
            var lastStatus = payments
                .OrderByDescending(p => p.PaymentDate)
                .Select(p => p.Status?.Trim())
                .FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));
            if (!string.IsNullOrWhiteSpace(lastStatus)
                && !string.Equals(lastStatus, "Paid", StringComparison.OrdinalIgnoreCase))
                return lastStatus!;
            return "Partially Paid";
        }

        private async Task<byte[]?> LoadLogoBytesAsync(string? logoPath)
        {
            try
            {
                var relative = (logoPath ?? "~/images/logohome.png")
                    .Replace("~/", string.Empty)
                    .Replace('/', Path.DirectorySeparatorChar)
                    .TrimStart(Path.DirectorySeparatorChar);

                var candidates = new[]
                {
                    Path.Combine(_env.WebRootPath ?? string.Empty, relative),
                    Path.Combine(_env.ContentRootPath, "wwwroot", relative),
                    Path.Combine(_env.ContentRootPath, "wwwroot", "images", "logohome.png"),
                    Path.Combine(_env.ContentRootPath, "wwwroot", "images", "logo.png")
                };

                foreach (var path in candidates.Where(p => !string.IsNullOrWhiteSpace(p)))
                {
                    if (File.Exists(path))
                        return await File.ReadAllBytesAsync(path);
                }
            }
            catch
            {
                // Logo is optional for rendering.
            }

            return null;
        }

        private string ResolveReportPath()
        {
            var candidates = new[]
            {
                Path.Combine(_env.ContentRootPath, "Reports", "AccountStatement.rdlc"),
                Path.Combine(AppContext.BaseDirectory, "Reports", "AccountStatement.rdlc")
            };
            return candidates.FirstOrDefault(File.Exists) ?? candidates[0];
        }
    }
}
