using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Biomed;

public record RaiseComplaintRequest(long EquipmentId, string Description);
public record AssignComplaintRequest(long AssignedToUserId);
public record ResolveComplaintRequest(string Resolution);

public record BreakdownComplaintDto(
    long Id, string ComplaintNumber, long EquipmentId, string EquipmentName, string ReportedByName, DateTimeOffset ReportedAt,
    string Description, ComplaintStatus Status, string? AssignedToName, DateTimeOffset? AssignedAt,
    DateTimeOffset? ResolvedAt, int? DowntimeMinutes, string? Resolution);

public class ComplaintQuery
{
    public ComplaintStatus? Status { get; set; }
    public long? EquipmentId { get; set; }
}
