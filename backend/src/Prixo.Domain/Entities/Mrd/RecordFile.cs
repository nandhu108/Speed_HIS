using Prixo.Domain.Common;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Entities.Ipd;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Mrd;

/// <summary>
/// The physical medical record file (plan §17: "physical file no per admission") — one row
/// per Admission, created by MRD once the file is opened. Current location/custodian live
/// here for a quick lookup; the full movement history is <see cref="FileMovement"/>.
/// </summary>
public class RecordFile : BranchEntity
{
    public long AdmissionId { get; set; }
    public Admission? Admission { get; set; }

    public required string FileNumber { get; set; }
    public RecordFileStatus Status { get; set; } = RecordFileStatus.Active;

    public string CurrentLocation { get; set; } = "MRD Storage";
    public long? CurrentCustodianUserId { get; set; }
    public User? CurrentCustodianUser { get; set; }

    public long CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }

    public DateTimeOffset? DestroyedAt { get; set; }
}

/// <summary>File Tracking (plan §17) — an append-only movement log; each entry also updates RecordFile's current-location snapshot.</summary>
public class FileMovement : BaseEntity
{
    public long RecordFileId { get; set; }
    public RecordFile? RecordFile { get; set; }

    public DateTimeOffset MovedAt { get; set; }
    public required string FromLocation { get; set; }
    public required string ToLocation { get; set; }
    public long? FromCustodianUserId { get; set; }
    public User? FromCustodianUser { get; set; }
    public long? ToCustodianUserId { get; set; }
    public User? ToCustodianUser { get; set; }
    public string? Reason { get; set; }

    public long MovedByUserId { get; set; }
    public User? MovedByUser { get; set; }
}

/// <summary>
/// File Issue &amp; Return, and Record Requests — internal/external/legal (plan §17) share one
/// table, distinguished by <see cref="FileRequestType"/>. Lifecycle: Requested → Approved/
/// Rejected → Issued → Returned. Overdue is a computed read (Issued and past ExpectedReturnDate),
/// not a stored status.
/// </summary>
public class FileRequest : BaseEntity
{
    public long RecordFileId { get; set; }
    public RecordFile? RecordFile { get; set; }

    public FileRequestType RequestType { get; set; } = FileRequestType.Internal;
    public long? RequestedByUserId { get; set; }
    public User? RequestedByUser { get; set; }
    public string? RequesterName { get; set; }
    public string? RequesterOrganization { get; set; }
    public required string Purpose { get; set; }
    public DateOnly? ExpectedReturnDate { get; set; }

    public FileRequestStatus Status { get; set; } = FileRequestStatus.Requested;
    public DateTimeOffset RequestedAt { get; set; }

    public long? ApprovedByUserId { get; set; }
    public User? ApprovedByUser { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public string? RejectReason { get; set; }

    public long? IssuedByUserId { get; set; }
    public User? IssuedByUser { get; set; }
    public DateTimeOffset? IssuedAt { get; set; }

    public long? ReturnedToUserId { get; set; }
    public User? ReturnedToUser { get; set; }
    public DateTimeOffset? ReturnedAt { get; set; }

    public string? Notes { get; set; }
}
