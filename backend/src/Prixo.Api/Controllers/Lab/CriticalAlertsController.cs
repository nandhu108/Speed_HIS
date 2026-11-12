using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Lab;

namespace Prixo.Api.Controllers.Lab;

[Route("api/v1/lab/critical-alerts")]
public class CriticalAlertsController(LabOrderService orders) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.LabAlertAcknowledge)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CriticalAlertDto>>>> GetPending(CancellationToken ct) =>
        OkResponse(await orders.GetPendingCriticalAlertsAsync(ct));

    [HttpPost("{id:long}/acknowledge")]
    [HasPermission(PermissionRegistry.LabAlertAcknowledge)]
    public async Task<ActionResult<ApiResponse>> Acknowledge(long id, AcknowledgeAlertRequest request, CancellationToken ct)
    {
        await orders.AcknowledgeAlertAsync(id, request, ct);
        return OkResponse<object?>(null, "Alert acknowledged.");
    }
}
