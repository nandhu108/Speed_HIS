using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Features.Appointments;
using Prixo.Application.Features.Portal;

namespace Prixo.Api.Controllers.Portal;

[Route("api/v1/portal/appointments")]
[PortalAuth]
public class PortalAppointmentsController(PortalAppointmentService appointments) : ApiControllerBase
{
    [HttpGet("slots")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SlotDto>>>> GetSlots([FromQuery] long doctorId, [FromQuery] DateOnly date, CancellationToken ct) =>
        OkResponse(await appointments.GetSlotsAsync(doctorId, date, ct));

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<AppointmentListItemDto>>>> GetMine([FromQuery] PagedQuery query, CancellationToken ct) =>
        OkResponse(await appointments.GetMyAppointmentsAsync(query, ct));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<AppointmentDetailDto>>> Book(PortalBookAppointmentRequest request, CancellationToken ct)
    {
        var appointment = await appointments.BookAsync(request, ct);
        return CreatedResponse($"/api/v1/portal/appointments/{appointment.Id}", appointment, "Appointment booked.");
    }

    [HttpPost("{id:long}/cancel")]
    public async Task<ActionResult<ApiResponse>> Cancel(long id, PortalCancelAppointmentRequest request, CancellationToken ct)
    {
        await appointments.CancelAsync(id, request, ct);
        return OkResponse<object?>(null, "Appointment cancelled.");
    }
}
