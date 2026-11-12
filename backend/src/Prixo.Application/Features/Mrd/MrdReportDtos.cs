namespace Prixo.Application.Features.Mrd;

public record CodingStatsDto(int TotalDischarged, int Coded, int Pending, decimal CodedPercent);

public record FileMovementReportRowDto(
    DateTimeOffset MovedAt, string FileNumber, string PatientName, string FromLocation, string ToLocation, string MovedByName);

public record IncompleteRecordRowDto(
    long AdmissionId, string AdmissionNumber, string PatientName, DateTimeOffset? DischargedAt,
    bool HasRecordFile, bool HasFileAudit, bool AuditComplete, bool IsCoded);

public class MrdReportDateRange
{
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
}
