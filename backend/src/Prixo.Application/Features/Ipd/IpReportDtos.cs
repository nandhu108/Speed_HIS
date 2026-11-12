using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Ipd;

public record IpReportQuery(
    string? Mode, DateOnly? FromDate, DateOnly? ToDate,
    long? DoctorId = null, long? WardId = null, long? DepartmentId = null,
    long? PatientCategoryId = null, long? ReferrerId = null, long? RoomTypeId = null,
    AdmissionStatus? Status = null, long? UnitId = null,
    string? RegNo = null, string? PatientName = null, string? IpNo = null);

/// <summary>"Doctor Admission Request" conversion report — every AdmissionRequest in range and
/// whether it converted (ApprovedAdmissionId set) — see IpReportService.GetAdmissionRequestConversionReportAsync.
/// `DoctorId` filters the *converted* admission's primary consultant (post-conversion); `AdvisedDoctorId`
/// filters the request's own RequestedConsultantId (pre-conversion) — these can differ once approved.
/// `RoomTypeId` ("Bed Type") only ever matches converted rows, since an unconverted request has no bed yet.</summary>
public record AdmissionRequestConversionReportQuery(
    DateOnly? FromDate, DateOnly? ToDate,
    long? DepartmentId = null, long? DoctorId = null, long? AdvisedDoctorId = null,
    long? WardId = null, long? RoomTypeId = null, long? PatientCategoryId = null,
    AdmissionRequestStatus? Status = null, string? RegNo = null, string? PatientName = null);

/// <summary>Bed Occupancy Report — `WardFlag` ("icu"/"observation", any other value/null = no
/// filter) maps to Ward.IsIcu/Ward.IsObservation. `BedStatus` is the bed's *current* status
/// (Bed.Status is live operational state, not a historical record), so filtering by it against a
/// past date range is necessarily approximate — flagged in the report page, not hidden.</summary>
public record WardOccupancyReportQuery(
    string? Mode, DateOnly? FromDate, DateOnly? ToDate,
    long? WardId = null, long? DoctorId = null, long? UnitId = null,
    string? WardFlag = null, BedStatus? BedStatus = null);
