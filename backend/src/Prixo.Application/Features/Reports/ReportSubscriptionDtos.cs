namespace Prixo.Application.Features.Reports;

public record ReportSubscriptionDto(long Id, string ReportCode, string CronExpression, string RecipientEmail, bool IsActive, DateTimeOffset CreatedAt);
public record CreateReportSubscriptionRequest(string ReportCode, string CronExpression, string RecipientEmail);
