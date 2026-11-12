namespace Prixo.Application.Features.Biomed;

public record LogServiceHistoryRequest(long EquipmentId, DateOnly ServiceDate, string ServiceType, string? PerformedBy, decimal? Cost, string? Notes);

public record ServiceHistoryDto(long Id, long EquipmentId, long? WorkOrderId, string? WorkOrderNumber, DateOnly ServiceDate, string ServiceType, string? PerformedBy, decimal? Cost, string? Notes);
