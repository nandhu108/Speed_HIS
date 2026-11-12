using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Finance;

/// <summary>Pure Finance rules — no I/O, so the services delegate here and the rules stay unit-testable without a database.</summary>
public static class FinanceMath
{
    /// <summary>Double-entry check: every voucher's debits must equal its credits, and it must have at least two lines.</summary>
    public static bool IsBalanced(IReadOnlyList<(decimal Debit, decimal Credit)> lines) =>
        lines.Count >= 2 && lines.Sum(l => l.Debit) == lines.Sum(l => l.Credit);

    public static (decimal Balance, PayableStatus Status) RecomputePayable(decimal invoiceAmount, decimal paidAmount)
    {
        var balance = Math.Max(invoiceAmount - paidAmount, 0);
        var status = balance <= 0 ? PayableStatus.Paid : paidAmount > 0 ? PayableStatus.PartiallyPaid : PayableStatus.Open;
        return (balance, status);
    }

    public static (decimal Balance, ReceivableStatus Status) RecomputeReceivable(decimal billAmount, decimal collectedAmount)
    {
        var balance = Math.Max(billAmount - collectedAmount, 0);
        var status = balance <= 0 ? ReceivableStatus.Collected : collectedAmount > 0 ? ReceivableStatus.PartiallyCollected : ReceivableStatus.Open;
        return (balance, status);
    }

    /// <summary>Standard 0-30/31-60/61-90/90+ aging buckets used by both Payables and Receivables aging reports.</summary>
    public static string AgingBucket(DateOnly asOf, DateOnly referenceDate)
    {
        var days = asOf.DayNumber - referenceDate.DayNumber;
        return days switch
        {
            <= 30 => "0-30",
            <= 60 => "31-60",
            <= 90 => "61-90",
            _ => "90+",
        };
    }
}
