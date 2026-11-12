namespace Prixo.Domain.Common;

/// <summary>
/// Shape shared by simple code+name reference masters, enabling the generic
/// master CRUD framework (one service/DTO/validator reused across entities).
/// </summary>
public interface ISimpleMaster
{
    string Code { get; set; }
    string Name { get; set; }
    string? Description { get; set; }
}
