/*
  Optional: create acc.vw_BudgetVsActual (per AMS_plan.md).
  The app report AmsReporting/BudgetVsActual uses equivalent LINQ without requiring this view.
*/
IF OBJECT_ID(N'acc.vw_BudgetVsActual', N'V') IS NOT NULL
    DROP VIEW acc.vw_BudgetVsActual;
GO

CREATE VIEW acc.vw_BudgetVsActual AS
SELECT
    bl.BudgetID,
    bl.AccountHeadID,
    ah.AccountName,
    bl.CostCenterID,
    bl.PeriodID,
    ISNULL(bl.RevisedAmount, bl.BudgetedAmount) AS BudgetAmount,
    ISNULL(SUM(vl.DebitAmount) - SUM(vl.CreditAmount), 0) AS ActualAmount,
    ISNULL(bl.RevisedAmount, bl.BudgetedAmount)
        - ISNULL(SUM(vl.DebitAmount) - SUM(vl.CreditAmount), 0) AS Variance
FROM acc.BudgetLine bl
JOIN acc.AccountHead ah ON bl.AccountHeadID = ah.AccountHeadID
LEFT JOIN acc.VoucherLine vl ON bl.AccountHeadID = vl.AccountHeadID
LEFT JOIN acc.Voucher v ON vl.VoucherID = v.VoucherID AND v.Status = N'Posted'
GROUP BY bl.BudgetID, bl.AccountHeadID, ah.AccountName,
         bl.CostCenterID, bl.PeriodID,
         bl.BudgetedAmount, bl.RevisedAmount;
GO
