using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Quality;

namespace Prixo.Api.Controllers.Quality;

[Route("api/v1/quality/capas")]
public class CapasController(QualityCapaService capas) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.QualityCapaView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CapaDto>>>> GetAll([FromQuery] CapaQuery query, CancellationToken ct) =>
        OkResponse(await capas.GetAllAsync(query, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.QualityCapaManage)]
    public async Task<ActionResult<ApiResponse<long>>> Raise(RaiseCapaRequest request, CancellationToken ct)
    {
        var id = await capas.RaiseAsync(request, ct);
        return CreatedResponse($"/api/v1/quality/capas/{id}", id, "CAPA raised.");
    }

    [HttpPost("{id:long}/complete")]
    [HasPermission(PermissionRegistry.QualityCapaManage)]
    public async Task<ActionResult<ApiResponse>> Complete(long id, CancellationToken ct)
    {
        await capas.CompleteAsync(id, ct);
        return OkResponse<object?>(null, "CAPA action marked complete.");
    }

    [HttpPost("{id:long}/verify")]
    [HasPermission(PermissionRegistry.QualityCapaManage)]
    public async Task<ActionResult<ApiResponse>> Verify(long id, VerifyCapaRequest request, CancellationToken ct)
    {
        await capas.VerifyAsync(id, request, ct);
        return OkResponse<object?>(null, "CAPA verified.");
    }

    [HttpPost("{id:long}/close")]
    [HasPermission(PermissionRegistry.QualityCapaManage)]
    public async Task<ActionResult<ApiResponse>> Close(long id, CancellationToken ct)
    {
        await capas.CloseAsync(id, ct);
        return OkResponse<object?>(null, "CAPA closed.");
    }
}
