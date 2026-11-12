using System.Globalization;

namespace Prixo.Application.Features.Emr;

/// <summary>
/// Pure lab-value parsing and allergen name matching — no I/O, so TimelineService/HistoryService
/// delegate here and the rules stay unit-testable without a database. Same reasoning as
/// Billing's BillMath / Lab's LabMath / Pharmacy's StockMath / IPD's IpdMath / Nursing's NursingMath.
/// </summary>
public static class EmrMath
{
    /// <summary>
    /// Results trend needs numeric values only — ResultValue.Value is a free-text string
    /// because some results are qualitative ("Positive"/"Negative"). Returns null for
    /// anything that doesn't parse, so the caller can silently skip it rather than crash.
    /// </summary>
    public static decimal? TryParseTrendValue(string rawValue) =>
        decimal.TryParse(rawValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var value) ? value : null;

    /// <summary>
    /// Case-insensitive, whitespace-trimmed substring match either direction — no drug/allergen
    /// master link exists (Allergy.AllergenName is free text), so this is a best-effort safety
    /// check, not an exact one. Empty/whitespace names never match (avoids a blank allergen
    /// name matching every drug).
    /// </summary>
    public static bool MatchesAllergen(string candidateName, string allergenName)
    {
        var candidate = candidateName.Trim();
        var allergen = allergenName.Trim();
        if (candidate.Length == 0 || allergen.Length == 0)
        {
            return false;
        }
        return candidate.Contains(allergen, StringComparison.OrdinalIgnoreCase)
            || allergen.Contains(candidate, StringComparison.OrdinalIgnoreCase);
    }
}
