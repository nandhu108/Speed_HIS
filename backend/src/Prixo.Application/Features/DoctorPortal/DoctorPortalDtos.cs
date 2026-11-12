namespace Prixo.Application.Features.DoctorPortal;

public record DoctorDashboardDto(
    int TodayOpCount, int ActiveInpatients, int PendingDischargeSigns, int UnacknowledgedCriticalAlerts);

public record PendingDischargeSummaryDto(
    long AdmissionId, string AdmissionNumber, long PatientId, string PatientUhid, string PatientName, string FinalDiagnosis);
