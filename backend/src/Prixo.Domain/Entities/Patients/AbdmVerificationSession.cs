using Prixo.Domain.Common;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Patients;

/// <summary>
/// Bridges the two-step ABDM OTP handshake: step 1 requests an OTP (ABDM returns a
/// transaction id), step 2 submits the OTP against that transaction. The ABDM txn id is
/// held server-side against a short-lived session so the client never handles it directly
/// and cannot swap it. Rows are single-use and expire; they are not a business record.
/// </summary>
public class AbdmVerificationSession : BaseEntity
{
    public long PatientId { get; set; }
    public Patient? Patient { get; set; }

    /// <summary>The transaction id returned by ABDM for this OTP challenge.</summary>
    public required string AbdmTxnId { get; set; }

    public AbdmVerificationMethod Method { get; set; }

    /// <summary>Masked target the OTP was sent to (e.g. XXXXXX1234), for display only.</summary>
    public string? MaskedTarget { get; set; }

    /// <summary>True once the OTP has been consumed — a session is single-use.</summary>
    public bool IsConsumed { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }
}
