using Prixo.Domain.Common;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Quality;

/// <summary>A finding raised against an NABH standard (plan §20: "Non-conformance") — distinct from Incident (a patient/staff-safety event), this is an accreditation-gap finding.</summary>
public class QualityNonConformance : BranchEntity
{
    public long? StandardId { get; set; }
    public QualityNabhStandard? Standard { get; set; }

    public long? DepartmentId { get; set; }
    public Department? Department { get; set; }

    public required string Title { get; set; }
    public required string Description { get; set; }
    public NonConformanceSeverity Severity { get; set; } = NonConformanceSeverity.Minor;

    public long RaisedByUserId { get; set; }
    public User? RaisedByUser { get; set; }

    public NonConformanceStatus Status { get; set; } = NonConformanceStatus.Open;
    public DateTimeOffset? ClosedAt { get; set; }
    public long? ClosedByUserId { get; set; }
    public User? ClosedByUser { get; set; }
}
