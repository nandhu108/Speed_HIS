using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Appointments;

namespace Prixo.Api.Controllers.Appointments;

[Route("api/v1/doctors-on-duty")]
public class DoctorsOnDutyController(AppointmentService appointments) : ApiControllerBase
{
    /// <summary>"Doctors On Duty" — every active doctor's computed availability for a date (a
    /// projection of the existing DoctorSchedule/ScheduleOverride data, not a new roster).</summary>
    [HttpGet]
    [HasPermission(PermissionRegistry.ScheduleView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DoctorOnDutyDto>>>> Get(
        [FromQuery] DateOnly? date, [FromQuery] long? departmentId, CancellationToken ct) =>
        OkResponse(await appointments.GetDoctorsOnDutyAsync(date ?? DateOnly.FromDateTime(DateTime.UtcNow), departmentId, ct));
}
