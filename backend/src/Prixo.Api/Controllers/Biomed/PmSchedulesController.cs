using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Biomed;

namespace Prixo.Api.Controllers.Biomed;

[Route("api/v1/biomed/pm-schedules")]
public class PmSchedulesController(PmScheduleService schedules) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.BiomedPmView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PmScheduleDto>>>> GetAll([FromQuery] bool dueOnly, CancellationToken ct) =>
        OkResponse(await schedules.GetAllAsync(dueOnly, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.BiomedPmManage)]
    public async Task<ActionResult<ApiResponse<long>>> Create(SavePmScheduleRequest request, CancellationToken ct)
    {
        var id = await schedules.CreateAsync(request, ct);
        return CreatedResponse($"/api/v1/biomed/pm-schedules/{id}", id, "PM schedule created.");
    }

    [HttpPost("generate-due")]
    [HasPermission(PermissionRegistry.BiomedPmManage)]
    public async Task<ActionResult<ApiResponse<int>>> GenerateDue(CancellationToken ct)
    {
        var count = await schedules.GenerateDueAsync(ct);
        return OkResponse(count, $"{count} work order(s) generated.");
    }
}
