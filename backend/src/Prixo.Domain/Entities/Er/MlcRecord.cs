using Prixo.Domain.Common;
using Prixo.Domain.Entities.Admin;

namespace Prixo.Domain.Entities.Er;

/// <summary>Medico-legal case documentation (plan §16's "MLC Register" statutory report) — one row per visit, only created when EmergencyVisit.IsMlc is set.</summary>
public class MlcRecord : BaseEntity
{
    public long EmergencyVisitId { get; set; }
    public EmergencyVisit? EmergencyVisit { get; set; }

    public required string MlcNumber { get; set; }
    public string? PoliceStationName { get; set; }
    public DateTimeOffset? PoliceInformedAt { get; set; }
    public string? FirNumber { get; set; }
    public string? BroughtByName { get; set; }
    public string? BroughtByDesignation { get; set; }
    public string? InjuryDescription { get; set; }
    public string? Notes { get; set; }

    public long RecordedByUserId { get; set; }
    public User? RecordedByUser { get; set; }
}

/// <summary>Accident Case Register (plan §16) — a separate statutory report from the MLC register even though the same visit is often both.</summary>
public class AccidentCaseRecord : BaseEntity
{
    public long EmergencyVisitId { get; set; }
    public EmergencyVisit? EmergencyVisit { get; set; }

    public required string AccidentType { get; set; }
    public DateTimeOffset? AccidentAt { get; set; }
    public string? AccidentLocation { get; set; }
    public bool VehicleInvolved { get; set; }
    public string? Notes { get; set; }

    public long RecordedByUserId { get; set; }
    public User? RecordedByUser { get; set; }
}
