using Microsoft.EntityFrameworkCore;
using PMS.Data;
using PMS.Models;

namespace PMS.Services
{
    /// <summary>
    /// Loads the same Account Statement graph used by Customer/AccountStatement (no new SQL/business rules).
    /// </summary>
    public interface IAccountStatementService
    {
        Task<AccountStatementData?> GetAsync(string accountNo, CancellationToken cancellationToken = default);
    }

    public sealed class AccountStatementData
    {
        public required Customer Customer { get; init; }
        public IReadOnlyDictionary<string, SurchargeComputationRow> SurchargeBySchedule { get; init; }
            = new Dictionary<string, SurchargeComputationRow>(StringComparer.OrdinalIgnoreCase);
        public IReadOnlyList<Payment> OtherAccountHeadPayments { get; init; } = Array.Empty<Payment>();
    }

    public class AccountStatementService : IAccountStatementService
    {
        private readonly PMSDbContext _context;
        private readonly ISurchargeService _surchargeService;

        public AccountStatementService(PMSDbContext context, ISurchargeService surchargeService)
        {
            _context = context;
            _surchargeService = surchargeService;
        }

        public async Task<AccountStatementData?> GetAsync(string accountNo, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(accountNo))
                return null;

            var customerIdTrimmed = accountNo.Trim();

            var paymentsTableExists = false;
            try
            {
                var tableExists = await _context.Database
                    .SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Payments'")
                    .FirstOrDefaultAsync(cancellationToken);
                paymentsTableExists = tableExists > 0;
            }
            catch
            {
                paymentsTableExists = false;
            }

            Customer? customer;
            if (paymentsTableExists)
            {
                customer = await _context.Customers
                    .Include(c => c.Project)
                    .Include(c => c.PaymentPlan)
                        .ThenInclude(pp => pp!.Project)
                    .Include(c => c.PaymentPlan)
                        .ThenInclude(pp => pp!.PaymentSchedules)
                            .ThenInclude(ps => ps.Payments)
                    .Include(c => c.JointOwners)
                    .Include(c => c.Allotments)
                        .ThenInclude(a => a.Property)
                    .FirstOrDefaultAsync(c => c.CustomerID != null && c.CustomerID.Trim() == customerIdTrimmed, cancellationToken);
            }
            else
            {
                customer = await _context.Customers
                    .Include(c => c.Project)
                    .Include(c => c.PaymentPlan)
                        .ThenInclude(pp => pp!.Project)
                    .Include(c => c.PaymentPlan)
                        .ThenInclude(pp => pp!.PaymentSchedules)
                    .Include(c => c.JointOwners)
                    .Include(c => c.Allotments)
                        .ThenInclude(a => a.Property)
                    .FirstOrDefaultAsync(c => c.CustomerID != null && c.CustomerID.Trim() == customerIdTrimmed, cancellationToken);
            }

            if (customer == null)
                return null;

            var schedules = customer.PaymentPlan?.PaymentSchedules ?? new List<PaymentSchedule>();

            // Attach payments by ScheduleID + CustomerID (plan schedules are shared across customers).
            if (paymentsTableExists)
                await AttachPaymentsByScheduleAsync(customerIdTrimmed, schedules, cancellationToken);

            var surchargeBySchedule = _surchargeService.ComputeBySchedule(
                schedules,
                customer.CustomerID,
                DateTime.Now.Date);

            List<Payment> otherPayments = new();
            if (paymentsTableExists)
            {
                var otherPaymentsRaw = await _context.Payments
                    .AsNoTracking()
                    .Where(p => p.CustomerID != null
                        && p.CustomerID.Trim() == customerIdTrimmed
                        && p.ScheduleID == null
                        && (p.AuditStatus == null
                            || p.AuditStatus == "Approved"
                            || p.AuditStatus == "Pending"))
                    .OrderBy(p => p.PaymentDate)
                    .ToListAsync(cancellationToken);
                otherPayments = otherPaymentsRaw
                    .Where(p =>
                        p.Amount < 0
                        || string.Equals((p.AccountHead ?? string.Empty).Trim(), "Surcharge Payment", StringComparison.OrdinalIgnoreCase)
                        || (p.Remarks ?? string.Empty).Contains("Surcharge payment", StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return new AccountStatementData
            {
                Customer = customer,
                SurchargeBySchedule = surchargeBySchedule,
                OtherAccountHeadPayments = otherPayments
            };
        }

        /// <summary>
        /// Loads payments for this customer and assigns each to its PaymentSchedule by ScheduleID
        /// (aligned to that schedule's PaymentDescription / InstallmentNo).
        /// </summary>
        private async Task AttachPaymentsByScheduleAsync(
            string customerIdTrimmed,
            IEnumerable<PaymentSchedule> schedules,
            CancellationToken cancellationToken)
        {
            var scheduleList = schedules as IList<PaymentSchedule> ?? schedules.ToList();
            if (scheduleList.Count == 0)
                return;

            var scheduleIds = scheduleList
                .Select(s => s.ScheduleID)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (scheduleIds.Count == 0)
                return;

            var paymentsList = await _context.Payments.AsNoTracking()
                .Where(p => p.CustomerID != null
                    && p.CustomerID.Trim() == customerIdTrimmed
                    && p.ScheduleID != null
                    && (p.AuditStatus == null || p.AuditStatus != "Declined"))
                .ToListAsync(cancellationToken);

            // Keep only payments whose ScheduleID belongs to this customer's plan schedules
            // (each schedule row is PaymentDescription + InstallmentNo).
            var scheduleIdSet = new HashSet<string>(scheduleIds, StringComparer.OrdinalIgnoreCase);
            paymentsList = paymentsList
                .Where(p => scheduleIdSet.Contains(p.ScheduleID!.Trim()))
                .ToList();

            var bySchedule = paymentsList
                .GroupBy(p => p.ScheduleID!.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => (ICollection<Payment>)g.ToList(), StringComparer.OrdinalIgnoreCase);

            foreach (var schedule in scheduleList)
            {
                var key = schedule.ScheduleID?.Trim();
                if (!string.IsNullOrWhiteSpace(key) && bySchedule.TryGetValue(key, out var matched))
                    schedule.Payments = matched;
                else
                    schedule.Payments = new List<Payment>();
            }
        }
    }
}
