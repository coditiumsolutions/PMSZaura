"""Add _AmsExportCsv partial to AMS list/report views."""
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1] / "Views"

# path -> export action name
EXPORTS = {
    "AmsPeriod/Index.cshtml": "FiscalYears",
    "AmsPeriod/Periods.cshtml": "Periods",
    "AmsOpeningBalance/Index.cshtml": "OpeningBalances",
    "AmsBank/Index.cshtml": "BankAccounts",
    "AmsBank/ChequeBooks.cshtml": "ChequeBooks",
    "AmsBank/ChequeRegisters.cshtml": "BankChequeRegisters",
    "AmsVendor/Index.cshtml": "Vendors",
    "AmsMasters/CostCenters.cshtml": "CostCenters",
    "AmsMasters/VoucherTypes.cshtml": "VoucherTypes",
    "AmsTaxType/Index.cshtml": "TaxTypes",
    "AmsBudget/Index.cshtml": "Budgets",
    "AmsJv/Index.cshtml": "JournalVouchers",
    "AmsBankVoucher/Index.cshtml": "BankVouchers",
    "AmsAr/Invoices.cshtml": "ArInvoices",
    "AmsAr/Aging.cshtml": "ArAging",
    "AmsAp/Bills.cshtml": "ApBills",
    "AmsAp/Aging.cshtml": "ApAging",
    "AmsTax/Transactions.cshtml": "TaxTransactions",
    "AmsTax/WhtSummary.cshtml": "WhtSummary",
    "AmsTax/GstInputOutput.cshtml": "GstInputOutput",
    "AmsChequeRegister/Index.cshtml": "ChequeRegister",
    "AmsBankReconciliation/Index.cshtml": "BankReconciliations",
    "AmsDealerCommission/Index.cshtml": "DealerCommissions",
    "AmsRefundVoucher/Index.cshtml": "RefundVouchers",
    "AmsLedger/TrialBalance.cshtml": "TrialBalance",
    "AmsLedger/GeneralLedger.cshtml": "GeneralLedger",
    "AmsLedger/PostedVouchers.cshtml": "PostedVouchers",
    "AmsReporting/BudgetVsActual.cshtml": "BudgetVsActual",
    "AmsReporting/CashFlow.cshtml": "CashFlow",
    "AmsReporting/ProjectProfitLoss.cshtml": "ProjectProfitLoss",
    "AmsAudit/Index.cshtml": "AuditLog",
}

PARTIAL = '    <partial name="_AmsExportCsv" model="{action}" />\n'


def insert_export(text: str, action: str) -> str:
    line = PARTIAL.format(action=action)
    if "_AmsExportCsv" in text:
        return text

    # Ledger/report forms: after Apply button
    m = re.search(
        r'(<button type="submit" class="btn btn-sm btn-primary">Apply</button>\s*</div>)',
        text,
    )
    if m:
        return text[: m.end()] + "\n    <motion class=\"col-auto\">\n" + line + "    </div>" + text[m.end() :]

    m = re.search(r'(<div class="mb-3 d-flex gap-2 flex-wrap">)', text)
    if m:
        return text[: m.end()] + "\n" + line + text[m.end() :]

    m = re.search(r'(<div class="d-flex justify-content-between[^"]*"[^>]*>)', text)
    if m:
        return text[: m.end()] + "\n" + line + text[m.end() :]

    m = re.search(r'(<div class="btn-group">)', text)
    if m:
        return text[: m.end()] + "\n" + line + text[m.end() :]

    return text


def main() -> None:
    for rel, action in EXPORTS.items():
        path = ROOT / rel.replace("/", "\\")
        if not path.exists():
            print("MISSING", rel)
            continue
        old = path.read_text(encoding="utf-8")
        new = insert_export(old, action)
        if new == old and "_AmsExportCsv" not in old:
            print("NO HOOK", rel)
        else:
            path.write_text(new, encoding="utf-8")
            print("OK", rel)


if __name__ == "__main__":
    main()
