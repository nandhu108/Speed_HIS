using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Mrd;

public record CreateFileRequestRequest(
    long RecordFileId, FileRequestType RequestType, string? RequesterName, string? RequesterOrganization,
    string Purpose, DateOnly? ExpectedReturnDate);

public record RejectFileRequestRequest(string Reason);

public record FileRequestDto(
    long Id, long RecordFileId, string FileNumber, FileRequestType RequestType,
    long? RequestedByUserId, string? RequestedByName, string? RequesterName, string? RequesterOrganization,
    string Purpose, DateOnly? ExpectedReturnDate, FileRequestStatus Status, DateTimeOffset RequestedAt,
    string? ApprovedByName, DateTimeOffset? ApprovedAt, string? RejectReason,
    string? IssuedByName, DateTimeOffset? IssuedAt, string? ReturnedToName, DateTimeOffset? ReturnedAt,
    bool IsOverdue, string? Notes);

public class FileRequestQuery
{
    public FileRequestStatus? Status { get; set; }
    public bool OverdueOnly { get; set; }
    public long? RecordFileId { get; set; }
}
