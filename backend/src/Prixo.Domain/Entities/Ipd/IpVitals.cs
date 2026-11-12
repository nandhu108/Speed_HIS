using Prixo.Domain.Common;

namespace Prixo.Domain.Entities.Ipd;

/// <summary>
/// Recorded several times a day (unlike OPD's one-per-visit VisitVitals) — the basic Phase 9
/// vitals chart; Phase 10 (Nursing) deepens this with shift-tagged notes and risk scores.
/// The four ICU-oriented fields (Phase 15) are nullable and usable by any ward — a patient's
/// vitals timeline stays on this one table across a ward↔ICU bed transfer rather than
/// forking into a parallel "ICU vitals" entity; ICU's own recording endpoint
/// (IcuVitalsService) is what additionally evaluates CriticalAlertRule thresholds against a
/// new row, but the row itself is the exact same IpVitals shape general wards use.
/// </summary>
public class IpVitals : BaseEntity
{
    public long AdmissionId { get; set; }
    public Admission? Admission { get; set; }

    public DateTimeOffset RecordedAt { get; set; }
    public long RecordedByUserId { get; set; }

    public decimal? TemperatureF { get; set; }
    public int? PulseRate { get; set; }
    public int? RespiratoryRate { get; set; }
    public int? BloodPressureSystolic { get; set; }
    public int? BloodPressureDiastolic { get; set; }
    public int? SpO2 { get; set; }
    public decimal? RandomBloodSugar { get; set; }

    public int? GlasgowComaScale { get; set; }
    public int? MeanArterialPressure { get; set; }
    public int? CentralVenousPressure { get; set; }
    public decimal? UrineOutputMl { get; set; }
}
