namespace Prixo.Application.Features.Masters.Units;

public record UnitDoctorDto(long DoctorId, string DoctorName);
public record ReplaceUnitDoctorsRequest(IReadOnlyList<long> DoctorIds);
