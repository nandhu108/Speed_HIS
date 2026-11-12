using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Ipd;

public record RequestDischargeRequest(long RequestedByDoctorId, DischargeType DischargeType, string? DischargeTypeNotes);

/// <summary>
/// BillBalanceAmount is populated only on the Billing row, from the admission's latest bill —
/// advisory only (docs/PATIENT_JOURNEY_GAP_ANALYSIS.md §5.2): billing staff still self-certify
/// IsCleared, this just makes the live balance visible instead of forcing a blind checkbox.
/// </summary>
public record DischargeClearanceDto(long Id, ClearanceType ClearanceType, bool IsCleared, string? ClearedByName, DateTimeOffset? ClearedAt, string? Remarks, decimal? BillBalanceAmount);
public record ClearChecklistItemRequest(string? Remarks);

public record UpsertDischargeSummaryRequest(
    string FinalDiagnosis, string? CourseInHospital, string? MedicationsOnDischarge, string? FollowUpAdvice, string? ConditionAtDischarge);

public record DischargeSummaryDto(
    long Id, long AdmissionId, string FinalDiagnosis, string? CourseInHospital, string? MedicationsOnDischarge,
    string? FollowUpAdvice, string? ConditionAtDischarge, bool IsApproved, string? ApprovedByName, DateTimeOffset? ApprovedAt);

public record ApproveDischargeSummaryRequest(long ApprovedByDoctorId);
