using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Icu;
using Prixo.Application.Features.Ipd;

namespace Prixo.Api.Controllers.Icu;

[Route("api/v1/icu")]
public class IcuVitalsController(IcuVitalsService vitals) : ApiControllerBase
{
    [HttpPost("admissions/{admissionId:long}/vitals")]
    [HasPermission(PermissionRegistry.IcuVitalsRecord)]
    public async Task<ActionResult<ApiResponse<IpVitalsDto>>> RecordVitals(long admissionId, AddVitalsRequest request, CancellationToken ct)
    {
        var dto = await vitals.RecordVitalsAsync(admissionId, request, ct);
        return CreatedResponse($"/api/v1/ipd/admissions/{admissionId}/vitals", dto, "Vitals recorded.");
    }

    [HttpGet("alerts")]
    [HasPermission(PermissionRegistry.IcuAlertView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<IcuVitalsAlertDto>>>> GetAlerts(
        [FromQuery] bool onlyUnacknowledged, CancellationToken ct) =>
        OkResponse(await vitals.GetAlertsAsync(onlyUnacknowledged, ct));

    [HttpPost("alerts/{id:long}/acknowledge")]
    [HasPermission(PermissionRegistry.IcuAlertAcknowledge)]
    public async Task<ActionResult<ApiResponse>> Acknowledge(long id, AcknowledgeAlertRequest request, CancellationToken ct)
    {
        await vitals.AcknowledgeAlertAsync(id, request, ct);
        return OkResponse<object?>(null, "Alert acknowledged.");
    }
}
