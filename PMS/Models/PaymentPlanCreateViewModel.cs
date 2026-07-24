using System.ComponentModel.DataAnnotations;

namespace PMS.Models
{
    public class PaymentPlanCreateViewModel
    {
        public PaymentPlanData? PaymentPlan { get; set; }
        public PaymentScheduleData? PaymentSchedules { get; set; }
    }

    public class PaymentPlanData
    {
        public string? PlanName { get; set; }
        [Required(ErrorMessage = "Project is required.")]
        public string? ProjectID { get; set; }
        public string? RegisteredSize { get; set; }
        [Required(ErrorMessage = "SubProject is required.")]
        public string? SubProject { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalAmountUSD { get; set; }
        public decimal ExchangeRate { get; set; }
        public string Currency { get; set; } = "SSP";
        public int DurationMonths { get; set; }
        public string? Frequency { get; set; }
        public string? Description { get; set; }
    }

    public class PaymentScheduleData
    {
        public bool IncludeToken { get; set; }
        public decimal? TokenAmount { get; set; }
        public decimal? TokenAmountUSD { get; set; }
        public string? TokenPaymentDescription { get; set; }
        public DateTime? TokenDueDate { get; set; }
        public decimal? PossessionAmount { get; set; }
        public decimal? PossessionAmountUSD { get; set; }
        public string? PossessionPaymentDescription { get; set; }
        public DateTime? PossessionDueDate { get; set; }
        public int TotalInstallments { get; set; }
        public decimal InstallmentAmount { get; set; }
        public decimal InstallmentAmountUSD { get; set; }
        public string? Frequency { get; set; }
        public DateTime FirstInstallmentDueDate { get; set; }
        public string? PaymentDescription { get; set; }
        public bool SurchargeApplied { get; set; } = true;
        public decimal SurchargeRate { get; set; } = 0.05m;
    }
}

