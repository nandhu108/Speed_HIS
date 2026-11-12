using Prixo.Domain.Common;
using Prixo.Domain.Entities.Masters;

namespace Prixo.Domain.Entities.Opd;

/// <summary>A doctor's saved favorite — applied to prefill a new prescription.</summary>
public class PrescriptionTemplate : BaseEntity
{
    public long DoctorId { get; set; }
    public Doctor? Doctor { get; set; }
    public required string Name { get; set; }

    public ICollection<PrescriptionTemplateItem> Items { get; set; } = [];
}

public class PrescriptionTemplateItem : BaseEntity
{
    public long PrescriptionTemplateId { get; set; }
    public PrescriptionTemplate? Template { get; set; }
    public long DrugId { get; set; }
    public Drug? Drug { get; set; }

    public required string Dose { get; set; }
    public required string Frequency { get; set; }
    public required string Duration { get; set; }
    public string? Route { get; set; }
    public string? Instructions { get; set; }
}
