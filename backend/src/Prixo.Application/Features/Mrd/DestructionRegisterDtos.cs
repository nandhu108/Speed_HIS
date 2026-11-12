using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Mrd;

public record CreateDestructionEntryRequest(long RecordFileId, long RetentionPolicyId);
public record DestroyRequest(string DestructionMethod, string? Notes);

public record EligibleFileDto(
    long RecordFileId, string FileNumber, long PatientId, string PatientName, DateTimeOffset? DischargedAt,
    long RetentionPolicyId, string RetentionPolicyName, DateOnly EligibleDate);

public record DestructionRegisterEntryDto(
    long Id, long RecordFileId, string FileNumber, long RetentionPolicyId, string RetentionPolicyName,
    DateOnly EligibleDate, DestructionStatus Status, string? ApprovedByName, DateTimeOffset? ApprovedAt,
    string? DestroyedByName, DateTimeOffset? DestroyedAt, string? DestructionMethod, string? Notes);

public class DestructionRegisterQuery
{
    public DestructionStatus? Status { get; set; }
}
