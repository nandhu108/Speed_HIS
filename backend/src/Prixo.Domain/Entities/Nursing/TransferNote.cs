using Prixo.Domain.Common;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Entities.Ipd;

namespace Prixo.Domain.Entities.Nursing;

/// <summary>Nursing handover note accompanying a ward/room/bed transfer — BedAllocation is optional since a transfer note can also be filed for a consultant-only or ICU-status change.</summary>
public class TransferNote : BaseEntity
{
    public long AdmissionId { get; set; }
    public Admission? Admission { get; set; }

    public long? BedAllocationId { get; set; }
    public BedAllocation? BedAllocation { get; set; }

    public DateTimeOffset TransferredAt { get; set; }
    public required string HandoverNotes { get; set; }

    public long TransferredByUserId { get; set; }
    public User? TransferredByUser { get; set; }
    public long? ReceivedByUserId { get; set; }
    public User? ReceivedByUser { get; set; }
}
