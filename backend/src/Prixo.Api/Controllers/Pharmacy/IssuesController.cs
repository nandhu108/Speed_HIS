using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Pharmacy;
using Prixo.Domain.Enums;

namespace Prixo.Api.Controllers.Pharmacy;

[Route("api/v1/pharmacy/issues")]
public class IssuesController(IssueService issues) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.IssueView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<IssueListItemDto>>>> GetAll(
        [FromQuery] IssueType? issueType, CancellationToken ct) =>
        OkResponse(await issues.GetPagedAsync(issueType, ct));

    [HttpGet("{id:long}")]
    [HasPermission(PermissionRegistry.IssueView)]
    public async Task<ActionResult<ApiResponse<IssueDetailDto>>> GetById(long id, CancellationToken ct) =>
        OkResponse(await issues.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.IssueCreate)]
    public async Task<ActionResult<ApiResponse<long>>> Create(CreateIssueRequest request, CancellationToken ct)
    {
        var id = await issues.CreateIssueAsync(request, ct);
        return CreatedResponse($"/api/v1/pharmacy/issues/{id}", id, "Issue created.");
    }
}
