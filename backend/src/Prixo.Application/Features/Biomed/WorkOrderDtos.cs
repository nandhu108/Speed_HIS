using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Biomed;

public record CreateWorkOrderFromComplaintRequest(long BreakdownComplaintId, DateOnly ScheduledDate, string? Notes);
public record AssignWorkOrderRequest(long AssignedToUserId);
public record CompleteWorkOrderRequest(string? Notes);
public record CancelWorkOrderRequest(string Reason);
public record RecordSpareUsageRequest(long BatchId, decimal Quantity);

public record SpareUsageDto(long Id, long BatchId, string ProductName, decimal Quantity);

public record WorkOrderDto(
    long Id, string WorkOrderNumber, long EquipmentId, string EquipmentName, WorkOrderType WorkOrderType,
    long? PmScheduleId, long? BreakdownComplaintId, WorkOrderStatus Status, DateOnly ScheduledDate, DateTimeOffset? CompletedAt,
    long? AssignedToUserId, string? AssignedToName, string? Notes, IReadOnlyList<SpareUsageDto> SpareUsages);

public class WorkOrderQuery
{
    public WorkOrderStatus? Status { get; set; }
    public WorkOrderType? WorkOrderType { get; set; }
    public long? EquipmentId { get; set; }
}
