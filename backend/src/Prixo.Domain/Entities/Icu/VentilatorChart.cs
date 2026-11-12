using Prixo.Domain.Common;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Entities.Ipd;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Icu;

/// <summary>
/// One continuous episode of mechanical ventilation — StoppedAt null means the patient is
/// currently ventilated, which is what IcuDashboardService's "ventilated count" counts.
/// Settings/observations are charted periodically underneath via Observations, not on the
/// session itself, since both change together at every round (same "session + periodic
/// flowsheet rows" shape as InfusionChartEntry/InfusionRateEntry).
/// </summary>
public class VentilatorSession : BaseEntity
{
    public long AdmissionId { get; set; }
    public Admission? Admission { get; set; }

    public DateTimeOffset StartedAt { get; set; }
    public long StartedByUserId { get; set; }
    public User? StartedByUser { get; set; }
    public DateTimeOffset? StoppedAt { get; set; }
    public string? Notes { get; set; }

    public ICollection<VentilatorObservation> Observations { get; set; } = [];
}

/// <summary>One flowsheet row — current settings and observed pressures recorded together, same as a real ICU ventilator chart round.</summary>
public class VentilatorObservation : BaseEntity
{
    public long SessionId { get; set; }
    public VentilatorSession? Session { get; set; }

    public DateTimeOffset RecordedAt { get; set; }
    public long RecordedByUserId { get; set; }
    public User? RecordedByUser { get; set; }

    public VentilatorMode Mode { get; set; }
    public int FiO2Percent { get; set; }
    public int Peep { get; set; }
    public int? TidalVolumeMl { get; set; }
    public int? RespiratoryRateSet { get; set; }
    public decimal? PeakPressure { get; set; }
    public decimal? PlateauPressure { get; set; }
    public string? Notes { get; set; }
}
