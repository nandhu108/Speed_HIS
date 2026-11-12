using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Prixo.Api.Authorization;

public record PermissionRequirement(string PermissionCode) : IAuthorizationRequirement;

/// <summary>
/// Builds "PERMISSION:MODULE.RESOURCE.ACTION" policies on demand so every code in the
/// registry works without registering hundreds of policies by hand.
/// </summary>
public class PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback = new(options);

    public async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(HasPermissionAttribute.PolicyPrefix, StringComparison.Ordinal))
        {
            var code = policyName[HasPermissionAttribute.PolicyPrefix.Length..];
            return new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(code))
                .Build();
        }
        return await _fallback.GetPolicyAsync(policyName);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();
}
