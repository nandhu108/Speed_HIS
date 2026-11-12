using Prixo.Domain.Common;

namespace Prixo.Domain.Entities.Opd;

/// <summary>One set of vitals per OPD visit (unlike IPD nursing charts, which record several times a day — Phase 10).</summary>
public class VisitVitals : BaseEntity
{
    public long VisitId { get; set; }
    public OpVisit? Visit { get; set; }

    public decimal? HeightCm { get; set; }
    public decimal? WeightKg { get; set; }
    /// <summary>Computed server-side from height/weight when both are present.</summary>
    public decimal? Bmi { get; set; }
    public decimal? TemperatureF { get; set; }
    public int? PulseRate { get; set; }
    public int? RespiratoryRate { get; set; }
    public int? BloodPressureSystolic { get; set; }
    public int? BloodPressureDiastolic { get; set; }
    public int? SpO2 { get; set; }
    public decimal? RandomBloodSugar { get; set; }
}
