using Prixo.Domain.Common;

namespace Prixo.Domain.Entities.Admin;

/// <summary>Hospital unit. Global entity — branches are what other data is scoped to.</summary>
public class Branch : BaseEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Pincode { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? GstNumber { get; set; }
    public string? ReportHeader { get; set; }
    public string TimeZone { get; set; } = "Asia/Kolkata";
    /// <summary>Menu path "/" redirects this branch's users to by default (null = no override). A Role's own DefaultLandingPath wins over this when both are set.</summary>
    public string? DefaultLandingPath { get; set; }
}
