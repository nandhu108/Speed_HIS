using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Mrd;

public record MarkCodedRequest(string? Notes);

public record CodingWorklistItemDto(
    long AdmissionId, string AdmissionNumber, long PatientId, string PatientName, string PatientUhid,
    DateTimeOffset? DischargedAt, CodingStatus Status, string? CodedByName, DateTimeOffset? CodedAt, string? Notes);

public class CodingWorklistQuery
{
    public bool OnlyPending { get; set; }
}
