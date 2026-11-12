namespace Prixo.Domain.Enums;

/// <summary>The "indent → approve → issue" workflow (plan §13) sitting in front of the existing Issue engine.</summary>
public enum IndentStatus
{
    Requested = 1,
    Approved = 2,
    Rejected = 3,
    Issued = 4,
}
