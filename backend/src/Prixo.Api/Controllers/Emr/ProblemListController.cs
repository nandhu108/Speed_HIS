using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Emr;
using Prixo.Domain.Enums;

namespace Prixo.Api.Controllers.Emr;

[Route("api/v1/emr/patients/{patientId:long}/problem-list")]
public class ProblemListController(ProblemListService problemList) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.EmrProblemListView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ProblemListEntryDto>>>> Get(
        long patientId, [FromQuery] ProblemStatus? status, CancellationToken ct) =>
        OkResponse(await problemList.GetAsync(patientId, status, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.EmrProblemListManage)]
    public async Task<ActionResult<ApiResponse<long>>> Add(long patientId, AddProblemRequest request, CancellationToken ct)
    {
        var id = await problemList.AddAsync(patientId, request, ct);
        return CreatedResponse($"/api/v1/emr/patients/{patientId}/problem-list/{id}", id, "Problem added.");
    }

    [HttpPost("{problemId:long}/resolve")]
    [HasPermission(PermissionRegistry.EmrProblemListManage)]
    public async Task<ActionResult<ApiResponse>> Resolve(long patientId, long problemId, CancellationToken ct)
    {
        await problemList.ResolveAsync(patientId, problemId, ct);
        return OkResponse<object?>(null, "Problem resolved.");
    }
}
