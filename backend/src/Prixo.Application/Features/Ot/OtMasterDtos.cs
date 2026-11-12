namespace Prixo.Application.Features.Ot;

public record TheatreDto(long Id, string Code, string Name, bool IsActive);
public record SaveTheatreRequest(string Code, string Name, bool IsActive = true);

public record EquipmentDto(long Id, string Code, string Name, long? TheatreId, string? TheatreName, bool IsActive);
public record SaveEquipmentRequest(string Code, string Name, long? TheatreId, bool IsActive = true);
