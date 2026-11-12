using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Biomed;

namespace Prixo.Api.Controllers.Biomed;

[Route("api/v1/biomed/complaints")]
public class BreakdownComplaintsController(BreakdownComplaintService complaints) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.BiomedComplaintView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<BreakdownComplaintDto>>>> GetAll([FromQuery] ComplaintQuery query, CancellationToken ct) =>
        OkResponse(await complaints.GetAllAsync(query, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.BiomedComplaintCreate)]
    public async Task<ActionResult<ApiResponse<long>>> Raise(RaiseComplaintRequest request, CancellationToken ct)
    {
        var id = await complaints.RaiseAsync(request, ct);
        return CreatedResponse($"/api/v1/biomed/complaints/{id}", id, "Complaint raised.");
    }

    [HttpPost("{id:long}/assign")]
    [HasPermission(PermissionRegistry.BiomedComplaintManage)]
    public async Task<ActionResult<ApiResponse>> Assign(long id, AssignComplaintRequest request, CancellationToken ct)
    {
        await complaints.AssignAsync(id, request, ct);
        return OkResponse<object?>(null, "Complaint assigned.");
    }

    [HttpPost("{id:long}/resolve")]
    [HasPermission(PermissionRegistry.BiomedComplaintManage)]
    public async Task<ActionResult<ApiResponse>> Resolve(long id, ResolveComplaintRequest request, CancellationToken ct)
    {
        await complaints.ResolveAsync(id, request, ct);
        return OkResponse<object?>(null, "Complaint resolved.");
    }

    [HttpPost("{id:long}/close")]
    [HasPermission(PermissionRegistry.BiomedComplaintManage)]
    public async Task<ActionResult<ApiResponse>> Close(long id, CancellationToken ct)
    {
        await complaints.CloseAsync(id, ct);
        return OkResponse<object?>(null, "Complaint closed.");
    }
}
