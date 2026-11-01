using Salzee.Models;

namespace Salzee.Services
{
    public interface ISurchargeService
    {
        IReadOnlyDictionary<string, SurchargeComputationRow> ComputeBySchedule(
            IEnumerable<PaymentSchedule> schedules,
            string? customerId,
            DateTime asOfDate);
    }

    public sealed class SurchargeComputationRow
    {
        public string ScheduleID { get; set; } = string.Empty;
        public decimal AmountPaid { get; set; }
        public decimal Outstanding { get; set; }
        public decimal Surcharge { get; set; }
        public int DaysOverdue { get; set; }
        public decimal DailyRatePercent { get; set; }
        public decimal DailySurchargeAmount { get; set; }
    }
}
