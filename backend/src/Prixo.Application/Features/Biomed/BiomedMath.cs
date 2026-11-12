namespace Prixo.Application.Features.Biomed;

/// <summary>Pure Biomedical rules — no I/O, so the services delegate here and the rules stay unit-testable without a database.</summary>
public static class BiomedMath
{
    /// <summary>An AMC/CMC contract is due for renewal once today is within its reminder window of the end date.</summary>
    public static bool IsRenewalDue(DateOnly endDate, int reminderDays, DateOnly today) =>
        today >= endDate.AddDays(-reminderDays);

    /// <summary>A PM schedule is due once today has reached its next-due date.</summary>
    public static bool IsPmDue(DateOnly nextDueDate, DateOnly today) => today >= nextDueDate;

    /// <summary>Downtime is the whole minutes between a complaint being reported and resolved.</summary>
    public static int DowntimeMinutes(DateTimeOffset reportedAt, DateTimeOffset resolvedAt) =>
        (int)Math.Max(0, (resolvedAt - reportedAt).TotalMinutes);

    /// <summary>PM compliance = work orders completed on/before their scheduled date, as a percent of all completed PM work orders.</summary>
    public static decimal PmCompliancePercent(int totalCompleted, int completedOnTime) =>
        totalCompleted == 0 ? 0m : Math.Round(completedOnTime * 100m / totalCompleted, 1);
}
