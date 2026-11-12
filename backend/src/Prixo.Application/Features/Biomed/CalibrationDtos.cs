namespace Prixo.Application.Features.Biomed;

public record SaveCalibrationRequest(long EquipmentId, DateOnly CalibrationDate, DateOnly NextDueDate, string? PerformedBy, string? CertificateNumber, string? Notes);

public record CalibrationDto(
    long Id, long EquipmentId, string EquipmentName, DateOnly CalibrationDate, DateOnly NextDueDate,
    string? PerformedBy, string? CertificateNumber, bool HasCertificateFile, bool IsDue, string? Notes);

public class CalibrationQuery
{
    public long? EquipmentId { get; set; }
    public bool DueOnly { get; set; }
}
