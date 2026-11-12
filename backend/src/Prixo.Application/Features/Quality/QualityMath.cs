namespace Prixo.Application.Features.Quality;

/// <summary>Pure calculations kept out of the services for unit testing, same idea as Biomed's BiomedMath.</summary>
public static class QualityMath
{
    /// <summary>
    /// Numerator/Denominator — null-guarded against a zero denominator. Deliberately not scaled
    /// by 100: an indicator's Denominator already encodes whether this is a percentage (e.g.
    /// occupied/totalBeds), a plain average (sum-of-TAT-hours/count), or a raw count
    /// (count/1) — baking in a x100 would silently corrupt the non-percentage cases. Not stored;
    /// computed on read.
    /// </summary>
    public static decimal? ComputedValue(decimal numerator, decimal denominator) =>
        denominator == 0 ? null : Math.Round(numerator / denominator, 4);

    /// <summary>Likelihood x Impact (each 1-5) — the classic risk-matrix score. Not stored; computed on read.</summary>
    public static int RiskScore(int likelihood, int impact) => likelihood * impact;

    /// <summary>Total scored / total max, as a percentage. Null when there are no responses yet.</summary>
    public static decimal? AuditScorePercent(IReadOnlyCollection<(decimal Score, decimal MaxScore)> responses)
    {
        if (responses.Count == 0) return null;
        var totalMax = responses.Sum(r => r.MaxScore);
        if (totalMax == 0) return null;
        return Math.Round(responses.Sum(r => r.Score) / totalMax * 100m, 2);
    }

    /// <summary>The chapter prefix of an NABH standard code (e.g. "AAC_1" -&gt; "AAC") — see QualityNabhStandard's doc comment.</summary>
    public static string ChapterOf(string standardCode)
    {
        var separatorIndex = standardCode.IndexOf('_');
        return separatorIndex < 0 ? standardCode : standardCode[..separatorIndex];
    }
}
