using PMS.Models;

namespace PMS.Services
{
    public class SurchargeService : ISurchargeService
    {
        public IReadOnlyDictionary<string, SurchargeComputationRow> ComputeBySchedule(
            IEnumerable<PaymentSchedule> schedules,
            string? customerId,
            DateTime asOfDate)
        {
            var result = new Dictionary<string, SurchargeComputationRow>(StringComparer.OrdinalIgnoreCase);
            var customerIdTrimmed = (customerId ?? string.Empty).Trim();
            var asOf = asOfDate.Date;

            foreach (var schedule in schedules)
            {
                if (string.IsNullOrWhiteSpace(schedule.ScheduleID))
                {
                    continue;
                }

                var payments = (schedule.Payments ?? new List<Payment>())
                    .Where(p => string.Equals((p.CustomerID ?? string.Empty).Trim(), customerIdTrimmed, StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(p.Status, "Pending", StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(p.AuditStatus, "Declined", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(p => p.PaymentDate)
                    .ToList();

                var amountPaid = payments.Sum(p => p.Amount);
                var outstanding = Math.Max(0m, schedule.Amount - amountPaid);
                var surcharge = 0m;
                var daysOverdue = 0;
                var dailyRatePercent = 0m;
                var dailySurchargeAmount = 0m;

                // Surcharge only when due date has passed.
                // Formula: Amount × (SurchargeRate × DaysPast / 100)
                // e.g. rate 0.05, days 50 → Amount × (0.05 × 50 / 100)
                if (schedule.SurchargeApplied && schedule.SurchargeRate > 0m)
                {
                    var dueDate = schedule.DueDate.Date;
                    var endDate = asOf;

                    // If installment is fully paid, stop accruing after the clearing payment date.
                    if (amountPaid >= schedule.Amount && payments.Count > 0)
                    {
                        endDate = payments[^1].PaymentDate.Date;
                    }

                    if (endDate > dueDate)
                    {
                        daysOverdue = (int)(endDate - dueDate).TotalDays;
                        if (daysOverdue > 0)
                        {
                            var amountForSurcharge = amountPaid >= schedule.Amount
                                ? schedule.Amount
                                : outstanding;

                            // rate × days / 100  (user-specified factor)
                            var factor = schedule.SurchargeRate * daysOverdue / 100m;
                            surcharge = Math.Round(amountForSurcharge * factor, 2, MidpointRounding.AwayFromZero);

                            dailyRatePercent = schedule.SurchargeRate > 1m
                                ? schedule.SurchargeRate
                                : schedule.SurchargeRate * 100m;
                            dailySurchargeAmount = daysOverdue > 0
                                ? Math.Round(surcharge / daysOverdue, 4, MidpointRounding.AwayFromZero)
                                : 0m;
                        }
                    }
                }

                var key = schedule.ScheduleID.Trim();
                result[key] = new SurchargeComputationRow
                {
                    ScheduleID = key,
                    AmountPaid = amountPaid,
                    Outstanding = outstanding,
                    Surcharge = surcharge,
                    DaysOverdue = daysOverdue,
                    DailyRatePercent = dailyRatePercent,
                    DailySurchargeAmount = dailySurchargeAmount
                };
            }

            return result;
        }
    }
}
