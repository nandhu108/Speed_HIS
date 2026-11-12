using Prixo.Domain.Common;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Patients;

/// <summary>
/// Audit trail of health information shared by this facility (HIP - M2) to an external HIU
/// in response to a verified ABDM consent artifact.
/// </summary>
public class AbdmDataTransferLog : BaseEntity
{
    public required string TransactionId { get; set; }

    public required string ConsentArtifactId { get; set; }

    public required string CareContextReference { get; set; }

    public AbdmHiType HiType { get; set; }

    public required string RecipientHiuId { get; set; }

    public AbdmTransferStatus Status { get; set; }

    public DateTimeOffset TransferDate { get; set; }

    public string? FhirBundleJson { get; set; }

    public string? ErrorMessage { get; set; }
}
