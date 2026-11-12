namespace Prixo.Application.Features.Mrd;

/// <summary>Pure MRD rules — no I/O, so the services delegate here and the rules stay unit-testable without a database.</summary>
public static class MrdMath
{
    /// <summary>An issued file request is overdue once today is past its expected return date.</summary>
    public static bool IsOverdue(DateOnly? expectedReturnDate, DateOnly today) =>
        expectedReturnDate is { } due && today > due;

    /// <summary>Retention window's expiry date — the day the file becomes eligible for destruction.</summary>
    public static DateOnly EligibleDate(DateOnly fromDate, int retentionYears) => fromDate.AddYears(retentionYears);

    /// <summary>A discharge file audit is complete only when every checklist item is present.</summary>
    public static bool IsAuditComplete(
        bool dischargeSummarySigned, bool consentsPresent, bool progressNotesComplete,
        bool investigationReportsPresent, bool nursingRecordsComplete) =>
        dischargeSummarySigned && consentsPresent && progressNotesComplete && investigationReportsPresent && nursingRecordsComplete;
}
