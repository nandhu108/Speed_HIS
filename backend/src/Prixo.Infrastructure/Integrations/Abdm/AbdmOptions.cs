namespace Prixo.Infrastructure.Integrations.Abdm;

/// <summary>
/// ABDM Gateway connection settings, bound from the "Abdm" configuration section. These are
/// secrets — supply them from a secret store / environment, never a committed appsettings
/// file. When <see cref="IsConfigured"/> is false the gateway stays inert and the ABHA
/// verify flow reports "not configured" instead of calling out.
/// </summary>
public class AbdmOptions
{
    public const string SectionName = "Abdm";

    /// <summary>Master switch; even with a base URL set, the gateway stays inert unless this is true.</summary>
    public bool Enabled { get; set; }

    /// <summary>ABDM Gateway base URL (sandbox e.g. https://healthidsbx.abdm.gov.in). Empty by default.</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>ABDM-issued client id (from the sandbox/PHR registration).</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>ABDM-issued client secret. Keep in a secret store.</summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>This facility's HIP id registered with ABDM (used by later M2/M3 milestones).</summary>
    public string HipId { get; set; } = string.Empty;

    public bool IsConfigured =>
        Enabled
        && !string.IsNullOrWhiteSpace(BaseUrl)
        && !string.IsNullOrWhiteSpace(ClientId)
        && !string.IsNullOrWhiteSpace(ClientSecret);
}
