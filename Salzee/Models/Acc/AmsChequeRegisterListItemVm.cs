namespace Salzee.Models.Acc;

/// <summary>Read-only register row: cheque plus optional AR receipt link.</summary>
public class AmsChequeRegisterListItemVm
{
    public AccChequeRegister Cheque { get; set; } = null!;

    public AccARReceipt? Receipt { get; set; }
}
