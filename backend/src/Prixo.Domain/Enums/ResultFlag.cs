namespace Prixo.Domain.Enums;

/// <summary>Critical takes precedence over High/Low when a value falls outside both ranges.</summary>
public enum ResultFlag
{
    Normal = 1,
    Low = 2,
    High = 3,
    Critical = 4,
}
