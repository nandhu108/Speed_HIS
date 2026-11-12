using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Reports;

/// <summary>
/// OPIP Census Report — department-wise New/Review×Gender/Child counts, and the patient-wise
/// listing behind it. `Scope` picks which side(s) of the census a query covers: Both (the
/// combined "OPIP" table), Op-only, or Ip-only — one query shape serves all three "Report Type"
/// views from the reference tool rather than three separate endpoints.
/// </summary>
public record CensusReportQuery(
    DateOnly? FromDate, DateOnly? ToDate, CensusScope Scope = CensusScope.Both,
    long? DepartmentId = null, long? WardId = null, long? PatientCategoryId = null);
