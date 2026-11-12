using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Pharmacy;

namespace Prixo.Api.Controllers.Pharmacy;

[Route("api/v1/pharmacy/issue-returns")]
public class IssueReturnsController(IssueService issues) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.IssueView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<IssueReturnListItemDto>>>> GetAll(CancellationToken ct) =>
        OkResponse(await issues.GetIssueReturnsAsync(ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.IssueReturnCreate)]
    public async Task<ActionResult<ApiResponse<long>>> Create(CreateIssueReturnRequest request, CancellationToken ct)
    {
        var id = await issues.CreateIssueReturnAsync(request, ct);
        return CreatedResponse($"/api/v1/pharmacy/issue-returns/{id}", id, "Issue return posted.");
    }
}
