using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Icu;

namespace Prixo.Api.Controllers.Icu;

[Route("api/v1/icu/dashboard")]
public class IcuDashboardController(IcuDashboardService dashboard) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.IcuDashboardView)]
    public async Task<ActionResult<ApiResponse<IcuDashboardDto>>> Get(CancellationToken ct) =>
        OkResponse(await dashboard.GetDashboardAsync(ct));
}
