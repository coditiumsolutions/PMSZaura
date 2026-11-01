namespace Salzee.Models.Acc;

public class AmsBudgetVsActualRowVm
{
    public int BudgetLineID { get; set; }

    public int AccountHeadID { get; set; }

    public string AccountCode { get; set; } = string.Empty;

    public string AccountName { get; set; } = string.Empty;

    public int? CostCenterID { get; set; }

    public int? PeriodID { get; set; }

    public string? PeriodName { get; set; }

    public decimal BudgetAmount { get; set; }

    public decimal ActualAmount { get; set; }

    public decimal Variance => BudgetAmount - ActualAmount;
}

public class AmsCashFlowMonthVm
{
    public int Year { get; set; }

    public int Month { get; set; }

    public string Label => $"{Year}-{Month:D2}";

    public decimal PostedDebits { get; set; }

    public decimal PostedCredits { get; set; }

    public decimal NetMovement => PostedDebits - PostedCredits;
}

public class AmsProjectPlRowVm
{
    public string CategoryName { get; set; } = string.Empty;

    public decimal NetAmount { get; set; }
}
