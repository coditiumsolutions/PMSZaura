using System.ComponentModel.DataAnnotations;

namespace PMS.Models
{
    public class AddPlanViewModel
    {
        public string? PlanID { get; set; }

        [Required(ErrorMessage = "Plan Name is required.")]
        [StringLength(150)]
        [Display(Name = "Plan Name")]
        public string PlanName { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Size")]
        public string? Size { get; set; }

        [Display(Name = "Price per unit")]
        public string? PricePerUnit { get; set; }

        [Required(ErrorMessage = "Total Amount is required.")]
        [Range(1, 1000000000, ErrorMessage = "Total Amount must be between 1 and 1,000,000,000.")]
        [Display(Name = "Total Amount (PKR)")]
        public decimal TotalAmount { get; set; }

        [StringLength(50)]
        [Display(Name = "Frequency")]
        public string? Frequency { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "First Due Date")]
        public DateTime? FirstDueDate { get; set; } = DateTime.Today;

        [StringLength(250)]
        [Display(Name = "Payment Title")]
        public string? PaymentTitle { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Due Date")]
        public DateTime? DueDate { get; set; } = DateTime.Today;

        [Display(Name = "Start Range")]
        public int? StartRange { get; set; }

        [Display(Name = "End Range")]
        public int? EndRange { get; set; }

        [Display(Name = "Due Amount")]
        public decimal? Amount { get; set; }

        [Display(Name = "Installment No")]
        public int? InstallmentNo { get; set; } = 1;

        [Display(Name = "Is Applied")]
        public bool SurchargePolicyStatus { get; set; } = true;

        [Range(0, 1, ErrorMessage = "Surcharge Rate must be between 0 and 1.")]
        [Display(Name = "Surcharge Rate")]
        public decimal SurchargeRate { get; set; } = 0.05m;

        public bool PlanSaved => !string.IsNullOrWhiteSpace(PlanID);

        public List<PaymentSchedule> Schedules { get; set; } = new();
    }
}
