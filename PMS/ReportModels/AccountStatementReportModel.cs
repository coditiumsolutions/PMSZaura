namespace PMS.ReportModels
{
    /// <summary>
    /// Header / summary row for AccountStatement.rdlc (single-row dataset).
    /// </summary>
    public class AccountStatementReportModel
    {
        public string CompanyName { get; set; } = string.Empty;
        public string ReportTitle { get; set; } = "Account Statement";
        public string CustomerName { get; set; } = string.Empty;
        public string PlotNumber { get; set; } = string.Empty;
        public string CustomerCode { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string CustomerAddress { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string CustomerMobile { get; set; } = string.Empty;
        public string CustomerMobile2 { get; set; } = string.Empty;
        public string BookingDate { get; set; } = string.Empty;
        public string StatementDate { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public string SubProject { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public string RegisteredSize { get; set; } = string.Empty;
        public string PropertyType { get; set; } = string.Empty;
        public string PaymentPlanId { get; set; } = string.Empty;
        public decimal GrandTotal { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal OutstandingBalance { get; set; }
        public string GrandTotalText { get; set; } = string.Empty;
        public string TotalPaidText { get; set; } = string.Empty;
        public string OutstandingBalanceText { get; set; } = string.Empty;
        public byte[] CompanyLogo { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// Schedule detail row for AccountStatement.rdlc.
    /// </summary>
    public class AccountStatementLineReportModel
    {
        public int? InstallmentNo { get; set; }
        public string InstallmentNoText { get; set; } = string.Empty;
        public string DueDate { get; set; } = string.Empty;
        public string PaymentDateText { get; set; } = string.Empty;
        public string ReferenceNumber { get; set; } = string.Empty;
        public decimal InstallmentAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal Balance { get; set; }
        public string InstallmentAmountText { get; set; } = string.Empty;
        public string PaidAmountText { get; set; } = string.Empty;
        public string BalanceText { get; set; } = string.Empty;
        public decimal Surcharge { get; set; }
        public string SurchargeText { get; set; } = "0";
        public string Status { get; set; } = string.Empty;
        public string AccountHead { get; set; } = string.Empty;
    }
}
