using Prixo.Application.Common.Interfaces;
using Prixo.Application.Common.Models;
using Prixo.Application.Features.Appointments;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Portal;

/// <summary>
/// Portal Appointment Booking (plan §21: "public slot view → book/cancel", "My Appointments").
/// A thin wrapper over the exact same AppointmentService the staff booking screen uses — slot
/// availability, token generation and the notification-queue write are all the one engine;
/// this class only adds the patient-ownership check the portal audience needs (a patient can
/// only see/cancel their own appointments) and the branch choice (the patient picks it — the
/// portal JWT carries no branch claim, unlike a staff session).
/// </summary>
public class PortalAppointmentService(AppointmentService appointments, IPortalCurrentUser currentUser)
{
    public Task<IReadOnlyList<SlotDto>> GetSlotsAsync(long doctorId, DateOnly date, CancellationToken ct = default) =>
        appointments.GetSlotsAsync(doctorId, date, ct);

    public async Task<AppointmentDetailDto> BookAsync(PortalBookAppointmentRequest request, CancellationToken ct = default) =>
        await appointments.BookForPortalAsync(
            new BookAppointmentRequest(currentUser.PatientId, request.DoctorId, request.AppointmentDate, request.StartTime, AppointmentBookingType.Booked, request.Notes),
            request.BranchId, ct);

    public async Task<PagedResult<AppointmentListItemDto>> GetMyAppointmentsAsync(PagedQuery query, CancellationToken ct = default) =>
        await appointments.GetPagedAsync(new AppointmentQuery { PatientId = currentUser.PatientId, Page = query.Page, PageSize = query.PageSize }, ct);

    public async Task CancelAsync(long id, PortalCancelAppointmentRequest request, CancellationToken ct = default)
    {
        var appointment = await appointments.GetByIdAsync(id, ct);
        if (appointment.PatientId != currentUser.PatientId)
        {
            throw new DomainException("You can only cancel your own appointments.");
        }
        await appointments.CancelAsync(id, new CancelAppointmentRequest(request.Reason), ct);
    }
}
