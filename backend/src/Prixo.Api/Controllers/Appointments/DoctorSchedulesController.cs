using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Appointments;

namespace Prixo.Api.Controllers.Appointments;

[Route("api/v1/doctors/{doctorId:long}/schedule")]
public class DoctorSchedulesController(DoctorScheduleService schedules) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.ScheduleView)]
    public async Task<ActionResult<ApiResponse<DoctorScheduleDto>>> Get(long doctorId, CancellationToken ct) =>
        OkResponse(await schedules.GetByDoctorIdAsync(doctorId, ct));

    [HttpPut("blocks")]
    [HasPermission(PermissionRegistry.ScheduleManage)]
    public async Task<ActionResult<ApiResponse>> ReplaceBlocks(
        long doctorId, ReplaceDoctorScheduleRequest request, CancellationToken ct)
    {
        await schedules.ReplaceBlocksAsync(doctorId, request, ct);
        return OkResponse<object?>(null, "Weekly schedule updated.");
    }

    [HttpPost("overrides")]
    [HasPermission(PermissionRegistry.ScheduleManage)]
    public async Task<ActionResult<ApiResponse<long>>> AddOverride(
        long doctorId, SaveScheduleOverrideRequest request, CancellationToken ct)
    {
        var id = await schedules.AddOverrideAsync(doctorId, request, ct);
        return CreatedResponse($"/api/v1/doctors/{doctorId}/schedule", id, "Override added.");
    }

    [HttpDelete("overrides/{overrideId:long}")]
    [HasPermission(PermissionRegistry.ScheduleManage)]
    public async Task<ActionResult<ApiResponse>> DeleteOverride(
        long doctorId, long overrideId, CancellationToken ct)
    {
        await schedules.DeleteOverrideAsync(doctorId, overrideId, ct);
        return OkResponse<object?>(null, "Override removed.");
    }
}
