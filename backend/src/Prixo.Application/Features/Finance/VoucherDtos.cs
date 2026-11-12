using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Finance;

public record PostVoucherLineRequest(long AccountId, decimal DebitAmount, decimal CreditAmount, string? Narration);
public record PostVoucherRequest(VoucherType VoucherType, DateOnly VoucherDate, string Narration, IReadOnlyList<PostVoucherLineRequest> Lines);
public record CancelVoucherRequest(string Reason);

public record VoucherLineDto(long AccountId, string AccountCode, string AccountName, decimal DebitAmount, decimal CreditAmount, string? Narration);

public record VoucherDto(
    long Id, string VoucherNumber, VoucherType VoucherType, DateOnly VoucherDate, string Narration, VoucherStatus Status,
    string PostedByName, DateTimeOffset PostedAt, long? ReversalOfVoucherId, string? CancelReason, string? SourceReference,
    IReadOnlyList<VoucherLineDto> Lines);

public class VoucherQuery
{
    public VoucherType? VoucherType { get; set; }
    public VoucherStatus? Status { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
}
