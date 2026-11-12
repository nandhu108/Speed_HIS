using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Appointments;

namespace Prixo.Api.Controllers.Appointments;

[Route("api/v1/queue")]
public class QueueController(AppointmentService appointments) : ApiControllerBase
{
    /// <summary>Today's (or a given date's) token queue for a doctor and/or department, waiting + checked-in only.</summary>
    [HttpGet("display")]
    [HasPermission(PermissionRegistry.QueueView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<QueueEntryDto>>>> GetDisplay(
        [FromQuery] long? doctorId, [FromQuery] long? deptId, [FromQuery] DateOnly? date, CancellationToken ct) =>
        OkResponse(await appointments.GetQueueAsync(
            doctorId, deptId, date ?? DateOnly.FromDateTime(DateTime.UtcNow), ct));
}
