namespace Prixo.Application.Common.Interfaces;

/// <summary>
/// The "internal event handler" hook the plan describes for Phase 18 ("billing/pharmacy/GRN
/// events post to ledger via internal event handlers, double-entry, transactional"). Declared
/// in Application/Common so an existing module's service (e.g. BillingService) can depend on
/// it without taking a hard dependency on the Finance feature's concrete class — the same
/// posture as IActivityLogger/IPdfService. Implemented by Finance's VoucherService.
/// Every method is a deliberate no-op (not an exception) when the branch hasn't configured the
/// accounts a posting needs yet — Finance setup is optional, not a precondition for the
/// existing module to keep working.
/// </summary>
public interface IFinanceLedgerPoster
{
    Task PostBillPaymentAsync(long billId, string billNumber, long paymentModeId, decimal amount, DateOnly date, CancellationToken ct = default);

    /// <summary>Debit Patient Revenue (reduce recorded revenue) / Credit Accounts Receivable (reduce what's owed) for an approved bill credit note.</summary>
    Task PostCreditNoteAsync(long billId, string billNumber, string creditNoteNumber, decimal amount, DateOnly date, CancellationToken ct = default);

    /// <summary>Debit the payment mode's linked bank/cash account / Credit Patient Revenue for money
    /// received from an insurer/TPA settling a claim — same shape as PostBillPaymentAsync, just
    /// sourced from the insurer instead of the patient directly.</summary>
    Task PostClaimSettlementAsync(long claimId, string claimNumber, long paymentModeId, decimal amount, DateOnly date, CancellationToken ct = default);
}
