using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Mrd;

public record CreateRecordFileRequest(long AdmissionId);

public record RecordMovementRequest(string ToLocation, long? ToCustodianUserId, string? Reason);

public record RecordFileListItemDto(
    long Id, string FileNumber, RecordFileStatus Status, long AdmissionId, string AdmissionNumber,
    long PatientId, string PatientName, string PatientUhid, string CurrentLocation, string? CurrentCustodianName, DateTimeOffset CreatedAt);

public record RecordFileDetailDto(
    long Id, string FileNumber, RecordFileStatus Status, long AdmissionId, string AdmissionNumber,
    long PatientId, string PatientName, string PatientUhid, string CurrentLocation, long? CurrentCustodianUserId,
    string? CurrentCustodianName, string CreatedByName, DateTimeOffset CreatedAt, DateTimeOffset? DestroyedAt);

public record FileMovementDto(
    long Id, DateTimeOffset MovedAt, string FromLocation, string ToLocation,
    string? FromCustodianName, string? ToCustodianName, string? Reason, string MovedByName);

public class RecordFileQuery
{
    public RecordFileStatus? Status { get; set; }
    public string? Search { get; set; }
    public long? AdmissionId { get; set; }
}
