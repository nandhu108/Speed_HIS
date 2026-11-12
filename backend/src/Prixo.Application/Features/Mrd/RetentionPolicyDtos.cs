namespace Prixo.Application.Features.Mrd;

public record SaveRetentionPolicyRequest(string Code, string Name, int RetentionYears, string? Description);

public record RetentionPolicyDto(long Id, string Code, string Name, int RetentionYears, string? Description, bool IsActive);
