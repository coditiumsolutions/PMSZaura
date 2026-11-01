using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Salzee.Data;
using Salzee.Services;

namespace Salzee.Controllers;

/// <summary>Admin-only deletes for all AMS list/detail rows (module permission: Delete).</summary>
[Authorize]
public class AmsAdminDeleteController : AmsControllerBase
{
    private readonly AmsAdminDeleteService _delete;

    public AmsAdminDeleteController(
        PMSDbContext context,
        IModulePermissionService modulePermission,
        AmsAccessService amsAccess,
        AmsAdminDeleteService delete)
        : base(context, modulePermission, amsAccess)
    {
        _delete = delete;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(string entity, long id, string? returnUrl)
    {
        var denied = await EnsureAmsPermissionAsync("Read");
        if (denied != null) return denied;
        if (!await AmsAccess.IsAdminUserAsync(CurrentUserId))
        {
            TempData["Error"] = "Only Admin users can delete AMS records.";
            return RedirectToAction("AccessDenied", "Account");
        }

        var e = entity?.Trim().ToLowerInvariant();
        var intId = (int)id;
        var result = e switch
        {
            "accounthead" => await _delete.DeleteAccountHeadAsync(intId),
            "fiscalyear" => await _delete.DeleteFiscalYearAsync(intId),
            "period" => await _delete.DeleteAccountingPeriodAsync(intId),
            "openingbalance" => await _delete.DeleteOpeningBalanceAsync(intId),
            "bankaccount" => await _delete.DeleteBankAccountAsync(intId),
            "chequebook" => await _delete.DeleteChequeBookAsync(intId),
            "chequeregister" => await _delete.DeleteChequeRegisterAsync(intId),
            "vendor" => await _delete.DeleteVendorAsync(intId),
            "taxtype" => await _delete.DeleteTaxTypeAsync(intId),
            "costcenter" => await _delete.DeleteCostCenterAsync(intId),
            "vouchertype" => await _delete.DeleteVoucherTypeAsync(intId),
            "budget" => await _delete.DeleteBudgetAsync(intId),
            "budgetline" => await _delete.DeleteBudgetLineAsync(intId),
            "voucher" => await _delete.DeleteVoucherAsync(intId),
            "arinvoice" => await _delete.DeleteArInvoiceAsync(intId),
            "arreceipt" => await _delete.DeleteArReceiptAsync(intId),
            "apbill" => await _delete.DeleteApBillAsync(intId),
            "appayment" => await _delete.DeleteApPaymentAsync(intId),
            "taxtransaction" => await _delete.DeleteTaxTransactionAsync(intId),
            "bankreconciliation" => await _delete.DeleteBankReconciliationAsync(intId),
            "reconline" => await _delete.DeleteBankReconciliationLineAsync(intId),
            "dealercommission" => await _delete.DeleteDealerCommissionAsync(intId),
            "refundvoucher" => await _delete.DeleteRefundVoucherAsync(intId),
            "auditlog" => await _delete.DeleteAuditLogAsync(id),
            _ => AmsDeleteResult.Fail("Unknown entity type.")
        };

        if (result.Success)
            TempData["Success"] = result.Message;
        else
            TempData["Error"] = result.Message;

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "AccountsManagement");
    }
}
