using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Er;

/// <summary>"ER Report" — Summary (grouped by arrival date) or Detail (per-visit) breakdown of
/// Emergency/Casualty visits, mirroring OpReportService/IpReportService's shape. Date range
/// filters EmergencyVisit.ArrivedAt.</summary>
public record ErReportQuery(
    DateOnly? FromDate, DateOnly? ToDate, string? Mode = "summary",
    long? DoctorId = null, EsiLevel? EsiLevel = null, EmergencyStatus? Status = null,
    DispositionType? Disposition = null, bool? IsMlc = null, long? PatientCategoryId = null,
    string? RegNo = null, string? PatientName = null);
