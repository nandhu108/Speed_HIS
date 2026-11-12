namespace Prixo.Domain.Enums;

/// <summary>Which register a CAPA was raised against — untyped polymorphic association (SourceId, no FK), same idiom as BillItem.SourceType/SourceId.</summary>
public enum CapaSourceType
{
    Incident = 1,
    NonConformance = 2,
    Audit = 3,
    Risk = 4,
}
