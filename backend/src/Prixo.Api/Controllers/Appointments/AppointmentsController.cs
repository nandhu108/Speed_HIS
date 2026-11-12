using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Appointments;

namespace Prixo.Api.Controllers.Appointments;

public class AppointmentsController(AppointmentService appointments) : ApiControllerBase
{
    [HttpGet("slots")]
    [HasPermission(PermissionRegistry.AppointmentView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SlotDto>>>> GetSlots(
        [FromQuery] long doctorId, [FromQuery] DateOnly date, CancellationToken ct) =>
        OkResponse(await appointments.GetSlotsAsync(doctorId, date, ct));

    [HttpGet]
    [HasPermission(PermissionRegistry.AppointmentView)]
    public async Task<ActionResult<ApiResponse<PagedResult<AppointmentListItemDto>>>> GetPaged(
        [FromQuery] AppointmentQuery query, CancellationToken ct) =>
        OkResponse(await appointments.GetPagedAsync(query, ct));

    [HttpGet("{id:long}")]
    [HasPermission(PermissionRegistry.AppointmentView)]
    public async Task<ActionResult<ApiResponse<AppointmentDetailDto>>> GetById(long id, CancellationToken ct) =>
        OkResponse(await appointments.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.AppointmentBook)]
    public async Task<ActionResult<ApiResponse<AppointmentDetailDto>>> Book(
        BookAppointmentRequest request, CancellationToken ct)
    {
        var result = await appointments.BookAsync(request, ct);
        return CreatedResponse($"/api/v1/appointments/{result.Id}", result,
            $"Appointment booked — token #{result.TokenNumber}.");
    }

    [HttpPost("{id:long}/reschedule")]
    [HasPermission(PermissionRegistry.AppointmentReschedule)]
    public async Task<ActionResult<ApiResponse<AppointmentDetailDto>>> Reschedule(
        long id, RescheduleAppointmentRequest request, CancellationToken ct)
    {
        var result = await appointments.RescheduleAsync(id, request, ct);
        return OkResponse(result, $"Appointment rescheduled — new token #{result.TokenNumber}.");
    }

    [HttpPost("{id:long}/cancel")]
    [HasPermission(PermissionRegistry.AppointmentCancel)]
    public async Task<ActionResult<ApiResponse>> Cancel(long id, CancelAppointmentRequest request, CancellationToken ct)
    {
        await appointments.CancelAsync(id, request, ct);
        return OkResponse<object?>(null, "Appointment cancelled.");
    }

    [HttpPost("{id:long}/check-in")]
    [HasPermission(PermissionRegistry.AppointmentCheckIn)]
    public async Task<ActionResult<ApiResponse>> CheckIn(long id, CancellationToken ct)
    {
        await appointments.CheckInAsync(id, ct);
        return OkResponse<object?>(null, "Patient checked in.");
    }

    [HttpPost("{id:long}/skip")]
    [HasPermission(PermissionRegistry.AppointmentSkip)]
    public async Task<ActionResult<ApiResponse>> Skip(long id, CancellationToken ct)
    {
        await appointments.SkipAsync(id, ct);
        return OkResponse<object?>(null, "Token skipped.");
    }

    [HttpPost("{id:long}/recall")]
    [HasPermission(PermissionRegistry.AppointmentSkip)]
    public async Task<ActionResult<ApiResponse>> Recall(long id, CancellationToken ct)
    {
        await appointments.RecallAsync(id, ct);
        return OkResponse<object?>(null, "Token recalled.");
    }

    [HttpPost("{id:long}/transfer")]
    [HasPermission(PermissionRegistry.AppointmentTransfer)]
    public async Task<ActionResult<ApiResponse<AppointmentDetailDto>>> Transfer(
        long id, TransferTokenRequest request, CancellationToken ct)
    {
        var result = await appointments.TransferAsync(id, request, ct);
        return OkResponse(result, $"Token transferred — new token #{result.TokenNumber}.");
    }

    /// <summary>"Tele-consulting" — set/replace the meeting link once one exists; reuses
    /// AppointmentBook (a booking-detail edit), no new permission.</summary>
    [HttpPut("{id:long}/teleconsult-link")]
    [HasPermission(PermissionRegistry.AppointmentBook)]
    public async Task<ActionResult<ApiResponse>> SetTeleconsultLink(
        long id, SaveTeleconsultLinkRequest request, CancellationToken ct)
    {
        await appointments.SetTeleconsultLinkAsync(id, request, ct);
        return OkResponse<object?>(null, "Meeting link saved.");
    }

    /// <summary>"ER Token Mapping" — mints an OPD walk-in token for an ER visit's assigned doctor.
    /// Lives here (not on the Emergency controller) and reuses AppointmentBook, same posture as
    /// Billing's generate-emergency-bill endpoint reusing the Emergency-specific billing create permission.</summary>
    [HttpPost("emergency/{emergencyVisitId:long}/map-token")]
    [HasPermission(PermissionRegistry.AppointmentBook)]
    public async Task<ActionResult<ApiResponse<AppointmentDetailDto>>> MapEmergencyToken(
        long emergencyVisitId, CancellationToken ct)
    {
        var result = await appointments.MapEmergencyTokenAsync(emergencyVisitId, ct);
        return OkResponse(result, $"ER token mapped — token #{result.TokenNumber}.");
    }
}
