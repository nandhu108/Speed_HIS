namespace Prixo.Application.Features.Ot;

public record RecordImplantUsageRequest(long BatchId, string? SerialNumber, decimal Quantity);
public record RecordConsumableUsageRequest(long BatchId, decimal Quantity);

public record ImplantUsageDto(long Id, long BookingId, long BatchId, string ProductName, string? SerialNumber, decimal Quantity, decimal Rate);
public record ConsumableUsageDto(long Id, long BookingId, long BatchId, string ProductName, decimal Quantity, decimal Rate);
