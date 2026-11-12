using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Icu;

public record IcuCensusPatientDto(
    long AdmissionId, string AdmissionNumber, string PatientName, string WardName, string RoomName, string BedCode,
    bool IsVentilated, IcuSeverityLevel? LatestSeverity, int UnacknowledgedAlertCount);

public record IcuDashboardDto(int Census, int VentilatedCount, IReadOnlyList<IcuCensusPatientDto> Patients);
