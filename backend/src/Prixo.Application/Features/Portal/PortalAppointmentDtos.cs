namespace Prixo.Application.Features.Portal;

public record PortalBookAppointmentRequest(long BranchId, long DoctorId, DateOnly AppointmentDate, TimeOnly StartTime, string? Notes);
public record PortalCancelAppointmentRequest(string Reason);
