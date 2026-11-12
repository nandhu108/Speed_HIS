using Prixo.Domain.Common;

namespace Prixo.Domain.Entities.Billing;

/// <summary>Append-only remark log against a bill — cashier/front-desk communication, not an editable field.</summary>
public class BillNote : BaseEntity
{
    public long BillId { get; set; }
    public Bill? Bill { get; set; }
    public required string Note { get; set; }
}
