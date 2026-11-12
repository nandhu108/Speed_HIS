namespace Prixo.Application.Features.Biomed;

public class BiomedReportDateRange
{
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
}

public record DowntimeReportRowDto(long EquipmentId, string EquipmentName, int ComplaintCount, int TotalDowntimeMinutes);

public record PmComplianceRowDto(long EquipmentId, string EquipmentName, int TotalCompleted, int CompletedOnTime, decimal CompliancePercent);
