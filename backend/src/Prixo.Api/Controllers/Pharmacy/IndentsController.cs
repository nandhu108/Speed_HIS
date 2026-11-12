using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Pharmacy;

namespace Prixo.Api.Controllers.Pharmacy;

[Route("api/v1/pharmacy/indents")]
public class IndentsController(IndentService indents) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.IndentView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<IndentListItemDto>>>> GetAll(
        [FromQuery] IndentQuery query, CancellationToken ct) =>
        OkResponse(await indents.GetPagedAsync(query, ct));

    [HttpGet("{id:long}")]
    [HasPermission(PermissionRegistry.IndentView)]
    public async Task<ActionResult<ApiResponse<IndentDetailDto>>> GetById(long id, CancellationToken ct) =>
        OkResponse(await indents.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.IndentCreate)]
    public async Task<ActionResult<ApiResponse<long>>> Create(CreateIndentRequest request, CancellationToken ct)
    {
        var id = await indents.CreateAsync(request, ct);
        return CreatedResponse($"/api/v1/pharmacy/indents/{id}", id, "Indent raised.");
    }

    [HttpPost("{id:long}/decide")]
    [HasPermission(PermissionRegistry.IndentApprove)]
    public async Task<ActionResult<ApiResponse>> Decide(long id, DecideIndentRequest request, CancellationToken ct)
    {
        await indents.DecideAsync(id, request, ct);
        return OkResponse<object?>(null, request.Approve ? "Indent approved." : "Indent rejected.");
    }
}
