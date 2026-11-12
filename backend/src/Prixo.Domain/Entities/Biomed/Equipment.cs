using Prixo.Domain.Common;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Biomed;

/// <summary>Equipment Master (plan §19: "serial, model, criticality, department/location"). CurrentLocation is a snapshot kept in sync by EquipmentLocationLog, same idiom as MRD's RecordFile.CurrentLocation.</summary>
public class BiomedEquipment : BranchEntity
{
    public required string EquipmentCode { get; set; }
    public required string Name { get; set; }
    public string? SerialNumber { get; set; }
    public string? Model { get; set; }
    public string? Manufacturer { get; set; }
    public long? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public long? VendorId { get; set; }
    public Vendor? Vendor { get; set; }

    public EquipmentCriticality Criticality { get; set; } = EquipmentCriticality.Medium;
    public EquipmentStatus Status { get; set; } = EquipmentStatus.InService;

    public string CurrentLocation { get; set; } = "";
    public DateOnly? PurchaseDate { get; set; }
    public decimal? PurchaseCost { get; set; }
    public DateOnly? WarrantyExpiryDate { get; set; }
    public string? Notes { get; set; }
}

/// <summary>Equipment Location tracking (plan §19) — an append-only movement log, mirrors MRD's FileMovement exactly.</summary>
public class EquipmentLocationLog : BaseEntity
{
    public long EquipmentId { get; set; }
    public BiomedEquipment? Equipment { get; set; }

    public DateTimeOffset MovedAt { get; set; }
    public required string FromLocation { get; set; }
    public required string ToLocation { get; set; }
    public string? Reason { get; set; }

    public long MovedByUserId { get; set; }
    public Admin.User? MovedByUser { get; set; }
}
