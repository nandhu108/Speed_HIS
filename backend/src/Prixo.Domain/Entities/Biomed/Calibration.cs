using Prixo.Domain.Common;

namespace Prixo.Domain.Entities.Biomed;

/// <summary>Calibration tracking (plan §19: "due tracking, certificates").</summary>
public class Calibration : BranchEntity
{
    public long EquipmentId { get; set; }
    public BiomedEquipment? Equipment { get; set; }

    public DateOnly CalibrationDate { get; set; }
    public DateOnly NextDueDate { get; set; }
    public string? PerformedBy { get; set; }
    public string? CertificateNumber { get; set; }
    /// <summary>Relative path under the file storage root, same on-disk idiom as MRD's ScannedDocument.</summary>
    public string? CertificateFilePath { get; set; }
    public string? Notes { get; set; }
}

/// <summary>Service History (plan §19) — every completed WorkOrder auto-logs one row here; ad-hoc vendor visits not tied to a work order can be logged directly.</summary>
public class ServiceHistoryEntry : BranchEntity
{
    public long EquipmentId { get; set; }
    public BiomedEquipment? Equipment { get; set; }
    public long? WorkOrderId { get; set; }
    public WorkOrder? WorkOrder { get; set; }

    public DateOnly ServiceDate { get; set; }
    public required string ServiceType { get; set; }
    public string? PerformedBy { get; set; }
    public decimal? Cost { get; set; }
    public string? Notes { get; set; }
}

/// <summary>Spare Usage (plan §19: "links Stores") — decrements a Batch via the existing Pharmacy stock-ledger writer, the same primitive OT's implant/consumable usage uses.</summary>
public class SpareUsage : BaseEntity
{
    public long WorkOrderId { get; set; }
    public WorkOrder? WorkOrder { get; set; }
    public long BatchId { get; set; }
    public Pharmacy.Batch? Batch { get; set; }
    public decimal Quantity { get; set; }
}
