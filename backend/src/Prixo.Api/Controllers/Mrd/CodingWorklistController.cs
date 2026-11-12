using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Mrd;

namespace Prixo.Api.Controllers.Mrd;

[Route("api/v1/mrd/coding-worklist")]
public class CodingWorklistController(CodingWorklistService worklist) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.MrdCodingView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CodingWorklistItemDto>>>> Get([FromQuery] CodingWorklistQuery query, CancellationToken ct) =>
        OkResponse(await worklist.GetWorklistAsync(query, ct));

    [HttpPost("{admissionId:long}/code")]
    [HasPermission(PermissionRegistry.MrdCodingManage)]
    public async Task<ActionResult<ApiResponse>> MarkCoded(long admissionId, MarkCodedRequest request, CancellationToken ct)
    {
        await worklist.MarkCodedAsync(admissionId, request, ct);
        return OkResponse<object?>(null, "Admission marked coded.");
    }
}
