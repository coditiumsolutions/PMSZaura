using Microsoft.EntityFrameworkCore;
using Salzee.Models.Acc;

namespace Salzee.Data;

public static class AccVoucherQueryableExtensions
{
    /// <summary>
    /// Includes <see cref="AccVoucher.BankAccount"/> only when acc.Voucher.BankAccountID is mapped (see <see cref="PMSDbContextAccCompat.MapVoucherBankAccountColumn"/>).
    /// </summary>
    public static IQueryable<AccVoucher> IncludeVoucherBankAccountIfMapped(this IQueryable<AccVoucher> query)
    {
        if (PMSDbContextAccCompat.MapVoucherBankAccountColumn)
            return query.Include(v => v.BankAccount);
        return query;
    }
}
