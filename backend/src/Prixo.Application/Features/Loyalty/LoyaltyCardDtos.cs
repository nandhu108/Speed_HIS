using Prixo.Application.Common.Models;
using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Loyalty;

public record RegisterLoyaltyCardRequest(long PatientId, long LoyaltyCardTypeId, int DurationMonths);

public record RenewLoyaltyCardRequest(long? LoyaltyCardTypeId, int DurationMonths);

public record AddDependentRequest(long PatientId, long MemberRelationTagId);

public record DeactivateLoyaltyCardRequest(string Reason);

public class LoyaltyCardQuery : PagedQuery
{
    public long? PatientId { get; set; }
    public LoyaltyCardStatus? Status { get; set; }
}

public record LoyaltyCardListItemDto(
    long Id, string CardNumber, long PatientId, string PatientUhid, string PatientName,
    long LoyaltyCardTypeId, string LoyaltyCardTypeName, DateOnly IssuedDate, DateOnly ExpiryDate,
    decimal Amount, LoyaltyCardStatus Status);

public record LoyaltyCardMemberDto(
    long Id, long PatientId, string PatientUhid, string PatientName,
    long MemberRelationTagId, string MemberRelationTagName, DateTimeOffset AddedAt);

public record LoyaltyCardDetailDto(
    long Id, string CardNumber, long PatientId, string PatientUhid, string PatientName, string PatientMobile,
    long LoyaltyCardTypeId, string LoyaltyCardTypeName, int MemberLimit, string? CategoryName,
    DateOnly IssuedDate, int DurationMonths, DateOnly ExpiryDate, decimal Amount, LoyaltyCardStatus Status,
    string? DeactivateReason, DateTimeOffset? DeactivatedAt, int DuplicateIssueCount,
    IReadOnlyList<LoyaltyCardMemberDto> Members);
