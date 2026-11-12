using Prixo.Domain.Common;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Er;

/// <summary>
/// One row per visit — triage happens once, right after registration (a re-triage just
/// updates this row rather than appending a new one, since ER doesn't need a full history
/// of triage assessments the way vitals need one). Basic vitals are captured inline rather
/// than reusing IPD's IpVitals, since there's no Admission yet at triage time and the set
/// needed here is small. EsiLevel drives the queue's auto-priority ordering (plan §16).
/// </summary>
public class Triage : BaseEntity
{
    public long EmergencyVisitId { get; set; }
    public EmergencyVisit? EmergencyVisit { get; set; }

    public DateTimeOffset TriagedAt { get; set; }
    public long TriagedByUserId { get; set; }
    public User? TriagedByUser { get; set; }

    public EsiLevel EsiLevel { get; set; }
    public required string ChiefComplaint { get; set; }

    public decimal? TemperatureF { get; set; }
    public int? PulseRate { get; set; }
    public int? RespiratoryRate { get; set; }
    public int? BloodPressureSystolic { get; set; }
    public int? BloodPressureDiastolic { get; set; }
    public int? SpO2 { get; set; }

    public string? Notes { get; set; }
}
