namespace Salzee.Models.Acc;

public class AmsBankVoucherCreateVm
{
    public string TypeCode { get; set; } = "BPV";

    public int FiscalYearID { get; set; }

    public DateTime VoucherDate { get; set; } = DateTime.UtcNow.Date;

    public string? ReferenceNo { get; set; }

    public string? Narration { get; set; }

    public decimal Amount { get; set; }

    /// <summary>BPV/BRV: operating bank account.</summary>
    public int? BankAccountID { get; set; }

    /// <summary>Non-bank GL for BPV (debit) / BRV (credit).</summary>
    public int ContraAccountHeadID { get; set; }

    /// <summary>CPV/CRV: cash/bank-in-hand style GL.</summary>
    public int CashAccountHeadID { get; set; }

    public bool UseCheque { get; set; }

    public int? ChequeRegisterID { get; set; }

    public string? NewChequeNo { get; set; }

    public DateTime? NewChequeDate { get; set; }

    public bool IsPostDated { get; set; }

    /// <summary>Two GL lines (debit/credit) shown in JV format on the create form.</summary>
    public List<AmsJvLineInputVm> Lines { get; set; } = new()
    {
        new AmsJvLineInputVm(),
        new AmsJvLineInputVm()
    };
}
