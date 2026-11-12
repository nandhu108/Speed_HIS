namespace Prixo.Infrastructure.Identity;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public required string Key { get; set; }
    public string Issuer { get; set; } = "Prixo";
    public string Audience { get; set; } = "Prixo";
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 7;

    /// <summary>The Patient Portal's distinct JWT audience (plan §21: "separate JWT audience") — a portal token never validates against the staff Audience, and vice versa.</summary>
    public string PortalAudience { get; set; } = "PrixoPortal";
    public int PortalAccessTokenMinutes { get; set; } = 15;
    public int PortalRefreshTokenDays { get; set; } = 14;
}
