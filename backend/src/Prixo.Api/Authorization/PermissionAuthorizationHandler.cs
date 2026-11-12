using Microsoft.AspNetCore.Authorization;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Features.Admin.Auth;

namespace Prixo.Api.Authorization;

public class PermissionAuthorizationHandler(
    PermissionService permissions, ICurrentUser currentUser)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (!currentUser.IsAuthenticated)
        {
            return;
        }

        if (await permissions.HasPermissionAsync(currentUser.UserId, requirement.PermissionCode))
        {
            context.Succeed(requirement);
        }
    }
}
