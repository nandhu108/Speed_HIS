using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Mrd;

namespace Prixo.Api.Controllers.Mrd;

[Route("api/v1/mrd/requests")]
public class FileRequestsController(FileRequestService requests) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.MrdFileView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<FileRequestDto>>>> GetQueue([FromQuery] FileRequestQuery query, CancellationToken ct) =>
        OkResponse(await requests.GetQueueAsync(query, ct));

    [HttpGet("{id:long}")]
    [HasPermission(PermissionRegistry.MrdFileView)]
    public async Task<ActionResult<ApiResponse<FileRequestDto>>> GetById(long id, CancellationToken ct) =>
        OkResponse(await requests.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.MrdRequestCreate)]
    public async Task<ActionResult<ApiResponse<long>>> Create(CreateFileRequestRequest request, CancellationToken ct)
    {
        var id = await requests.CreateAsync(request, ct);
        return CreatedResponse($"/api/v1/mrd/requests/{id}", id, "File request raised.");
    }

    [HttpPost("{id:long}/approve")]
    [HasPermission(PermissionRegistry.MrdFileIssue)]
    public async Task<ActionResult<ApiResponse>> Approve(long id, CancellationToken ct)
    {
        await requests.ApproveAsync(id, ct);
        return OkResponse<object?>(null, "Request approved.");
    }

    [HttpPost("{id:long}/reject")]
    [HasPermission(PermissionRegistry.MrdFileIssue)]
    public async Task<ActionResult<ApiResponse>> Reject(long id, RejectFileRequestRequest request, CancellationToken ct)
    {
        await requests.RejectAsync(id, request, ct);
        return OkResponse<object?>(null, "Request rejected.");
    }

    [HttpPost("{id:long}/issue")]
    [HasPermission(PermissionRegistry.MrdFileIssue)]
    public async Task<ActionResult<ApiResponse>> Issue(long id, CancellationToken ct)
    {
        await requests.IssueAsync(id, ct);
        return OkResponse<object?>(null, "File issued.");
    }

    [HttpPost("{id:long}/return")]
    [HasPermission(PermissionRegistry.MrdFileIssue)]
    public async Task<ActionResult<ApiResponse>> Return(long id, CancellationToken ct)
    {
        await requests.ReturnAsync(id, ct);
        return OkResponse<object?>(null, "File returned.");
    }
}
