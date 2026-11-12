namespace Prixo.Domain.Common;

public interface IAuditable
{
    DateTimeOffset CreatedAt { get; set; }
    long CreatedBy { get; set; }
    DateTimeOffset? UpdatedAt { get; set; }
    long? UpdatedBy { get; set; }
}
