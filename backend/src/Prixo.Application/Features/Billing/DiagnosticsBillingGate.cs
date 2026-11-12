using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Features.Admin.Settings;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Billing;

/// <summary>
/// "Billing verification" for OP diagnostics: before an OP lab sample is collected or an OP scan is
/// scheduled, the test must be on the patient's bill (and, in the default "Paid" mode, that bill must
/// be settled or be a credit bill). Controlled by <see cref="SettingKeys.BillingDiagnosticsGate"/>.
/// ER and inpatient orders are not gated — they are billed after the fact.
/// </summary>
public static class DiagnosticsBillingGate
{
    public static async Task EnsureBilledAsync(
        IAppDbContext db, SettingService settings, IReadOnlyCollection<long?> billItemIds, string what, CancellationToken ct)
    {
        var mode = (await settings.GetStringAsync(SettingKeys.BillingDiagnosticsGate, "Paid", ct)).Trim();
        if (mode.Equals("None", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }
        if (billItemIds.Any(id => id is null))
        {
            throw new DomainException($"{what} has not been billed yet. Send the patient to billing (OP bill › Add pending orders) first.");
        }

        var ids = billItemIds.Select(id => id!.Value).Distinct().ToList();
        var bills = await db.BillItems.AsNoTracking().Where(i => ids.Contains(i.Id))
            .Select(i => new { i.Bill!.Status, i.Bill.BalanceAmount, i.Bill.IsCreditBill, i.Bill.BillNumber })
            .ToListAsync(ct);
        if (bills.Count != ids.Count || bills.Any(b => b.Status == BillStatus.Cancelled))
        {
            throw new DomainException($"The bill line for {what} was cancelled. Re-bill the test before proceeding.");
        }
        if (mode.Equals("Paid", StringComparison.OrdinalIgnoreCase))
        {
            var unpaid = bills.FirstOrDefault(b => b.BalanceAmount > 0 && !b.IsCreditBill);
            if (unpaid is not null)
            {
                throw new DomainException($"Bill {unpaid.BillNumber} still has {unpaid.BalanceAmount:0.00} due. Collect payment before proceeding with {what}.");
            }
        }
    }
}
