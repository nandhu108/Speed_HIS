using Prixo.Domain.Common;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Finance;

/// <summary>
/// The single posting document (plan §18: "Journal / Payment / Receipt Vouchers") — every
/// ledger impact, whether entered by hand or posted automatically by a billing/pharmacy/GRN
/// event, goes through creating one of these plus its <see cref="VoucherLine"/>s. Posting is
/// immediate (no draft state) since a voucher only exists once its lines already balance;
/// correcting a mistake creates a reversal voucher (ReversalOfVoucherId) rather than editing
/// or deleting posted lines — the ledger stays append-only.
/// </summary>
public class Voucher : BranchEntity
{
    public required string VoucherNumber { get; set; }
    public VoucherType VoucherType { get; set; }
    public DateOnly VoucherDate { get; set; }
    public required string Narration { get; set; }
    public VoucherStatus Status { get; set; } = VoucherStatus.Posted;

    public long PostedByUserId { get; set; }
    public User? PostedByUser { get; set; }
    public DateTimeOffset PostedAt { get; set; }

    public long? ReversalOfVoucherId { get; set; }
    public Voucher? ReversalOfVoucher { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public string? CancelReason { get; set; }

    /// <summary>Free-text tag identifying the automatic source event ("BILL_PAYMENT:123", "PAYABLE_SETTLEMENT:45"…) — null for a manually entered voucher.</summary>
    public string? SourceReference { get; set; }

    public ICollection<VoucherLine> Lines { get; set; } = [];
}

/// <summary>One debit-or-credit leg of a voucher. Exactly one of DebitAmount/CreditAmount is non-zero.</summary>
public class VoucherLine : BaseEntity
{
    public long VoucherId { get; set; }
    public Voucher? Voucher { get; set; }
    public long AccountId { get; set; }
    public Account? Account { get; set; }

    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public string? Narration { get; set; }
}

/// <summary>
/// Denormalized, account-indexed projection of every posted VoucherLine — exists as its own
/// table (plan's `ledger_entries`, distinct from `voucher_lines`) purely so the ledger viewer
/// (running balance for one account over a date range) doesn't have to join through Voucher on
/// every read. Always written 1:1 with a VoucherLine at posting time by VoucherService.
/// </summary>
public class LedgerEntry : BranchEntity
{
    public long VoucherId { get; set; }
    public Voucher? Voucher { get; set; }
    public long VoucherLineId { get; set; }
    public VoucherLine? VoucherLine { get; set; }
    public long AccountId { get; set; }
    public Account? Account { get; set; }

    public DateOnly EntryDate { get; set; }
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public string? Narration { get; set; }
}
