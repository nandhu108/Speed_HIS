namespace Prixo.Application.Features.Ot;

/// <summary>
/// Pure OT scheduling math — no I/O, so BookingService delegates here and the rule stays
/// unit-testable without a database. Same reasoning as Billing's BillMath / Lab's LabMath.
/// </summary>
public static class OtMath
{
    /// <summary>Half-open interval overlap test — a booking ending exactly when another starts does not conflict.</summary>
    public static bool Overlaps(DateTimeOffset aStart, DateTimeOffset aEnd, DateTimeOffset bStart, DateTimeOffset bEnd) =>
        aStart < bEnd && bStart < aEnd;
}
