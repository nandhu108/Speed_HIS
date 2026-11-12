using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Quality;

public record SaveRiskRequest(
    string Title, string? Category, string Description, int Likelihood, int Impact, string? MitigationPlan,
    long OwnerUserId, DateOnly IdentifiedDate, DateOnly? ReviewDate);

public record RiskDto(
    long Id, string Title, string? Category, string Description, int Likelihood, int Impact, int RiskScore,
    string? MitigationPlan, string OwnerName, RiskStatus Status, DateOnly IdentifiedDate, DateOnly? ReviewDate, DateTimeOffset? ClosedAt);

public record RiskQuery(RiskStatus? Status);
