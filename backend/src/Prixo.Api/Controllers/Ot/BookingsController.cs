using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Ot;

namespace Prixo.Api.Controllers.Ot;

[Route("api/v1/ot/bookings")]
public class BookingsController(BookingService bookings) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.OtBookingView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<BookingListItemDto>>>> GetAll([FromQuery] BookingQuery query, CancellationToken ct) =>
        OkResponse(await bookings.GetPagedAsync(query, ct));

    [HttpGet("{id:long}")]
    [HasPermission(PermissionRegistry.OtBookingView)]
    public async Task<ActionResult<ApiResponse<BookingDetailDto>>> GetById(long id, CancellationToken ct) =>
        OkResponse(await bookings.GetByIdAsync(id, ct));

    [HttpGet("register")]
    [HasPermission(PermissionRegistry.OtRegisterView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<OtRegisterRowDto>>>> GetRegister(
        [FromQuery] DateOnly fromDate, [FromQuery] DateOnly toDate, CancellationToken ct) =>
        OkResponse(await bookings.GetRegisterAsync(fromDate, toDate, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.OtBookingCreate)]
    public async Task<ActionResult<ApiResponse<long>>> Create(CreateBookingRequest request, CancellationToken ct)
    {
        var id = await bookings.CreateAsync(request, ct);
        return CreatedResponse($"/api/v1/ot/bookings/{id}", id, "Surgery booked.");
    }

    [HttpPost("{id:long}/reschedule")]
    [HasPermission(PermissionRegistry.OtBookingManage)]
    public async Task<ActionResult<ApiResponse>> Reschedule(long id, RescheduleBookingRequest request, CancellationToken ct)
    {
        await bookings.RescheduleAsync(id, request, ct);
        return OkResponse<object?>(null, "Booking rescheduled.");
    }

    [HttpPost("{id:long}/update-patient")]
    [HasPermission(PermissionRegistry.OtBookingManage)]
    public async Task<ActionResult<ApiResponse>> UpdatePatient(long id, UpdateBookingPatientRequest request, CancellationToken ct)
    {
        await bookings.UpdatePatientAsync(id, request, ct);
        return OkResponse<object?>(null, "Booking patient updated.");
    }

    [HttpPost("{id:long}/cancel")]
    [HasPermission(PermissionRegistry.OtBookingManage)]
    public async Task<ActionResult<ApiResponse>> Cancel(long id, CancelBookingRequest request, CancellationToken ct)
    {
        await bookings.CancelAsync(id, request, ct);
        return OkResponse<object?>(null, "Booking cancelled.");
    }

    [HttpPost("{id:long}/postpone")]
    [HasPermission(PermissionRegistry.OtBookingManage)]
    public async Task<ActionResult<ApiResponse>> Postpone(long id, PostponeBookingRequest request, CancellationToken ct)
    {
        await bookings.PostponeAsync(id, request, ct);
        return OkResponse<object?>(null, "Booking postponed.");
    }

    [HttpPost("{id:long}/verify-consent")]
    [HasPermission(PermissionRegistry.OtChecklistManage)]
    public async Task<ActionResult<ApiResponse>> VerifyConsent(long id, VerifyConsentRequest request, CancellationToken ct)
    {
        await bookings.VerifyConsentAsync(id, request, ct);
        return OkResponse<object?>(null, "Consent verified.");
    }

    [HttpPost("{id:long}/start")]
    [HasPermission(PermissionRegistry.OtSurgeryConduct)]
    public async Task<ActionResult<ApiResponse>> Start(long id, CancellationToken ct)
    {
        await bookings.StartAsync(id, ct);
        return OkResponse<object?>(null, "Surgery started.");
    }

    [HttpPost("{id:long}/complete")]
    [HasPermission(PermissionRegistry.OtSurgeryConduct)]
    public async Task<ActionResult<ApiResponse>> Complete(long id, CancellationToken ct)
    {
        await bookings.CompleteAsync(id, ct);
        return OkResponse<object?>(null, "Surgery completed.");
    }

    [HttpPost("{id:long}/team")]
    [HasPermission(PermissionRegistry.OtBookingManage)]
    public async Task<ActionResult<ApiResponse<long>>> AddTeamMember(long id, AddTeamMemberRequest request, CancellationToken ct)
    {
        var memberId = await bookings.AddTeamMemberAsync(id, request, ct);
        return CreatedResponse($"/api/v1/ot/bookings/{id}/team/{memberId}", memberId, "Team member added.");
    }

    [HttpDelete("{id:long}/team/{memberId:long}")]
    [HasPermission(PermissionRegistry.OtBookingManage)]
    public async Task<ActionResult<ApiResponse>> RemoveTeamMember(long id, long memberId, CancellationToken ct)
    {
        await bookings.RemoveTeamMemberAsync(id, memberId, ct);
        return OkResponse<object?>(null, "Team member removed.");
    }
}
