using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Appointments;

namespace Prixo.Api.Controllers.Appointments;

/// <summary>"Schedule Unit - Day Wise" — mirrors DoctorSchedulesController minus the overrides sub-resource.</summary>
[Route("api/v1/units/{unitId:long}/schedule")]
public class UnitSchedulesController(UnitScheduleService schedules) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.AppointmentUnitScheduleView)]
    public async Task<ActionResult<ApiResponse<UnitScheduleDto>>> Get(long unitId, CancellationToken ct) =>
        OkResponse(await schedules.GetByUnitIdAsync(unitId, ct));

    [HttpPut("blocks")]
    [HasPermission(PermissionRegistry.AppointmentUnitScheduleManage)]
    public async Task<ActionResult<ApiResponse>> ReplaceBlocks(
        long unitId, ReplaceUnitScheduleRequest request, CancellationToken ct)
    {
        await schedules.ReplaceBlocksAsync(unitId, request, ct);
        return OkResponse<object?>(null, "Weekly schedule updated.");
    }
}
