using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Er;

namespace Prixo.Api.Controllers.Er;

[Route("api/v1/emergency/visits/{visitId:long}")]
public class MlcController(MlcService mlc) : ApiControllerBase
{
    [HttpGet("mlc")]
    [HasPermission(PermissionRegistry.EmergencyVisitView)]
    public async Task<ActionResult<ApiResponse<MlcRecordDto>>> GetMlc(long visitId, CancellationToken ct) =>
        OkResponse(await mlc.GetMlcAsync(visitId, ct));

    [HttpPut("mlc")]
    [HasPermission(PermissionRegistry.EmergencyMlcManage)]
    public async Task<ActionResult<ApiResponse<long>>> SaveMlc(long visitId, SaveMlcRequest request, CancellationToken ct)
    {
        var id = await mlc.SaveMlcAsync(visitId, request, ct);
        return OkResponse(id, "MLC record saved.");
    }

    [HttpGet("accident")]
    [HasPermission(PermissionRegistry.EmergencyVisitView)]
    public async Task<ActionResult<ApiResponse<AccidentCaseRecordDto>>> GetAccident(long visitId, CancellationToken ct) =>
        OkResponse(await mlc.GetAccidentAsync(visitId, ct));

    [HttpPut("accident")]
    [HasPermission(PermissionRegistry.EmergencyMlcManage)]
    public async Task<ActionResult<ApiResponse<long>>> SaveAccident(long visitId, SaveAccidentRequest request, CancellationToken ct)
    {
        var id = await mlc.SaveAccidentAsync(visitId, request, ct);
        return OkResponse(id, "Accident case record saved.");
    }
}
