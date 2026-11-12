using Prixo.Domain.Common;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Masters;

// Billing/clinical reference masters. Global unless noted.

/// <summary>GST slab (rate stored as percent, e.g. 12.00).</summary>
public class Tax : BaseEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public decimal RatePercent { get; set; }
}

/// <summary>
/// Discount kind with control limits: max percent and the threshold above which
/// approval is required (Phase 6 discount workflow reads these).
/// </summary>
public class DiscountType : BaseEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public decimal MaxPercent { get; set; } = 100;
    public decimal ApprovalThresholdPercent { get; set; }
}

/// <summary>Referral source. Internal referrers link to a doctor; external ones are free-standing.</summary>
public class Referrer : BranchEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public ReferrerType Type { get; set; } = ReferrerType.External;
    public long? DoctorId { get; set; }
    public Doctor? Doctor { get; set; }
    public string? Phone { get; set; }
    public string? Organization { get; set; }
}

/// <summary>ICD-10 diagnosis code. Global; bulk-loaded from CSV.</summary>
public class IcdCode : BaseEntity
{
    public required string Code { get; set; }
    public required string Description { get; set; }
    /// <summary>Chapter/block grouping, e.g. "IX Circulatory system".</summary>
    public string? Category { get; set; }
}

/// <summary>Corporate client (schema now — corporate billing settlement is a later phase).</summary>
public class Corporate : BaseEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? GstNumber { get; set; }
    /// <summary>Placeholder credit control for the held corporate-billing flow.</summary>
    public decimal CreditLimit { get; set; }
}

/// <summary>Insurance/TPA company (placeholder — Insurance/TPA is a held module; schema only).</summary>
public class InsuranceCompany : BaseEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public bool IsTpa { get; set; }
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
}

public class Uom : BaseEntity, ISimpleMaster
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
}

/// <summary>Lab specimen type (created here; Phase 7 lab orders consume it).</summary>
public class SampleType : BaseEntity, ISimpleMaster
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
}

/// <summary>Radiology modality (created here; Phase 12 consumes it).</summary>
public class Modality : BaseEntity, ISimpleMaster
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
}
