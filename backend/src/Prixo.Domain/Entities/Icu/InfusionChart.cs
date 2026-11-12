using Prixo.Domain.Common;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Entities.Ipd;
using Prixo.Domain.Entities.Masters;

namespace Prixo.Domain.Entities.Icu;

/// <summary>
/// One continuous infusion order — StoppedAt null means it's still running. DrugId links to
/// the catalog when the infused agent is a Drug; DrugNameOverride covers non-catalog fluids
/// (e.g. plain "Normal Saline" titration isn't always modeled as a Drug row). Rate/titration
/// history lives in RateEntries, not here — same session+flowsheet split as
/// VentilatorSession/VentilatorObservation.
/// </summary>
public class InfusionChartEntry : BaseEntity
{
    public long AdmissionId { get; set; }
    public Admission? Admission { get; set; }
    public long? DrugId { get; set; }
    public Drug? Drug { get; set; }
    public string? DrugNameOverride { get; set; }

    public DateTimeOffset StartedAt { get; set; }
    public long StartedByUserId { get; set; }
    public User? StartedByUser { get; set; }
    public DateTimeOffset? StoppedAt { get; set; }
    public string? Notes { get; set; }

    public ICollection<InfusionRateEntry> RateEntries { get; set; } = [];
}

/// <summary>One titration flowsheet row against a running infusion.</summary>
public class InfusionRateEntry : BaseEntity
{
    public long InfusionChartEntryId { get; set; }
    public InfusionChartEntry? InfusionChartEntry { get; set; }

    public DateTimeOffset RecordedAt { get; set; }
    public long RecordedByUserId { get; set; }
    public User? RecordedByUser { get; set; }

    public decimal Rate { get; set; }
    public required string RateUnit { get; set; }
    public string? Notes { get; set; }
}
