using Prixo.Domain.Common;
using Prixo.Domain.Entities.Masters;

namespace Prixo.Domain.Entities.Opd;

/// <summary>One prescription per visit, holding a list of drug items.</summary>
public class Prescription : BaseEntity
{
    public long VisitId { get; set; }
    public OpVisit? Visit { get; set; }
    public string? Notes { get; set; }

    public ICollection<PrescriptionItem> Items { get; set; } = [];
}

public class PrescriptionItem : BaseEntity
{
    public long PrescriptionId { get; set; }
    public Prescription? Prescription { get; set; }
    public long DrugId { get; set; }
    public Drug? Drug { get; set; }

    public required string Dose { get; set; }
    /// <summary>Free text — e.g. "1-0-1", "OD", "QID".</summary>
    public required string Frequency { get; set; }
    /// <summary>Free text — e.g. "5 days".</summary>
    public required string Duration { get; set; }
    public string? Route { get; set; }
    public string? Instructions { get; set; }
}
