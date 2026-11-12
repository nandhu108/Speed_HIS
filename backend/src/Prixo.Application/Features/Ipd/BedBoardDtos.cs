using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Ipd;

public record BedBoardItemDto(
    long BedId, string BedCode, BedStatus Status, string WardName, string RoomName, string RoomTypeName,
    long? AdmissionId, string? AdmissionNumber, string? PatientName, string? PrimaryConsultantName, DateTimeOffset? AllocatedAt);

public record SetBedStatusRequest(BedStatus Status, string? Reason);

public record BedOccupancyDashboardDto(
    int TotalBeds, int Occupied, int Vacant, int Reserved, int Cleaning, int Blocked, decimal OccupancyRatePercent,
    IReadOnlyList<WardOccupancyDto> ByWard);

public record WardOccupancyDto(long WardId, string WardName, int TotalBeds, int Occupied, decimal OccupancyRatePercent);
