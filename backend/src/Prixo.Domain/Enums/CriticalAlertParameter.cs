namespace Prixo.Domain.Enums;

/// <summary>The IpVitals fields a CriticalAlertRule can threshold against.</summary>
public enum CriticalAlertParameter
{
    SpO2 = 1,
    PulseRate = 2,
    RespiratoryRate = 3,
    BloodPressureSystolic = 4,
    BloodPressureDiastolic = 5,
    TemperatureF = 6,
}
