using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Biomed;

public record SaveEquipmentRequest(
    string EquipmentCode, string Name, string? SerialNumber, string? Model, string? Manufacturer,
    long? DepartmentId, long? VendorId, EquipmentCriticality Criticality, string CurrentLocation,
    DateOnly? PurchaseDate, decimal? PurchaseCost, DateOnly? WarrantyExpiryDate, string? Notes);

public record RecordEquipmentMovementRequest(string ToLocation, string? Reason);

public record EquipmentListItemDto(
    long Id, string EquipmentCode, string Name, string? SerialNumber, string? Model, string? DepartmentName,
    EquipmentCriticality Criticality, EquipmentStatus Status, string CurrentLocation, DateOnly? WarrantyExpiryDate);

public record EquipmentDetailDto(
    long Id, string EquipmentCode, string Name, string? SerialNumber, string? Model, string? Manufacturer,
    long? DepartmentId, string? DepartmentName, long? VendorId, string? VendorName, EquipmentCriticality Criticality,
    EquipmentStatus Status, string CurrentLocation, DateOnly? PurchaseDate, decimal? PurchaseCost,
    DateOnly? WarrantyExpiryDate, string? Notes);

public record EquipmentLocationLogDto(long Id, DateTimeOffset MovedAt, string FromLocation, string ToLocation, string? Reason, string MovedByName);

public class EquipmentQuery
{
    public EquipmentStatus? Status { get; set; }
    public EquipmentCriticality? Criticality { get; set; }
    public string? Search { get; set; }
}
