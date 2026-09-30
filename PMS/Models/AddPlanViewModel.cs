using System.ComponentModel.DataAnnotations;

namespace PMS.Models
{
    public class AddPlanViewModel
    {
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

        [Required(ErrorMessage = "Payment Title is required.")]
        [StringLength(250)]
        [Display(Name = "Payment Title")]
        public string PaymentTitle { get; set; } = string.Empty;

        [Required(ErrorMessage = "Amount is required.")]
        [Range(0.01, 1000000000, ErrorMessage = "Amount must be greater than zero.")]
        [Display(Name = "Amount")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Due Date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Due Date")]
        public DateTime DueDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Installment No is required.")]
        [Display(Name = "Installment No")]
        public int InstallmentNo { get; set; } = 1;

        [Display(Name = "Surcharge Policy Status")]
        public bool SurchargePolicyStatus { get; set; } = true;

        [Range(0, 1, ErrorMessage = "Surcharge Rate must be between 0 and 1.")]
        [Display(Name = "Surcharge Rate")]
        public decimal SurchargeRate { get; set; } = 0.05m;
    }
}
