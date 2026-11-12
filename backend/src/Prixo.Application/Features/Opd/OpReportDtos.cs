using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Opd;

public record OpReportQuery(
    string? Mode, DateOnly? FromDate, DateOnly? ToDate, long? CampId = null,
    long? DoctorId = null, VisitType? VisitType = null, long? PatientCategoryId = null,
    long? DepartmentId = null, long? ReferrerId = null, long? ReferralDoctorId = null,
    string? RegNo = null, string? PatientName = null, bool? IsMlcCase = null);

/// <summary>"OP TAT Report" — registration→consult-start / consult-start→completion turnaround.</summary>
public record OpTatReportQuery(DateOnly? FromDate, DateOnly? ToDate, long? DoctorId);

/// <summary>"Referral Report" — every Internal/External referral raised across OPD visits.</summary>
public record ReferralReportQuery(DateOnly? FromDate, DateOnly? ToDate, ReferralDirection? Direction);

/// <summary>"OP to IP Conversion Report" — completed OPD visits vs. those converted to an
/// admission (Summary, grouped by day) or a per-visit listing (Detail).</summary>
public record ConversionReportQuery(
    DateOnly? FromDate, DateOnly? ToDate, string? Mode = "summary",
    long? DoctorId = null, long? DepartmentId = null, long? PatientCategoryId = null,
    string? RegNo = null, string? PatientName = null);

/// <summary>"MHC Package/Doctor/Referrer/Patient-wise Report" filters — `MhcDoctorId` filters the
/// panel doctor tagging list (MhcDoctor), not the regular Doctor roster; MHC visits have no
/// DoctorId or DoctorDepartments link, so a "Department" filter has no mapping here and is
/// intentionally not offered (unlike the OP/IP reports). RegNo/PatientName/PatientCategoryId only
/// meaningfully narrow the Patient List variant but are harmless no-ops on the three aggregate
/// ones, so one shared query type covers all four rather than a parallel record.</summary>
public record MhcReportQuery(
    DateOnly? FromDate, DateOnly? ToDate, long? MhcDoctorId = null, VisitType? VisitType = null,
    long? PatientCategoryId = null, string? RegNo = null, string? PatientName = null);

/// <summary>"Service List Report - MHC" — every billed Service-sourced BillItem on an MHC visit's
/// bill. `ServiceId` is required (the reference tool marks it "Service *") — see
/// OpReportService.GetMhcServiceListReportAsync.</summary>
public record MhcServiceListReportQuery(
    DateOnly? FromDate, DateOnly? ToDate, long? ServiceId,
    BillType? BillType = null, string? RegNo = null);
