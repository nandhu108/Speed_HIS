namespace Prixo.Application.Common.Utilities;

/// <summary>Small, dependency-free fuzzy-matching helpers (patient duplicate detection).</summary>
public static class TextSimilarity
{
    /// <summary>Normalized Levenshtein similarity in [0, 1]; 1 = identical.</summary>
    public static double NameSimilarity(string a, string b)
    {
        var left = Normalize(a);
        var right = Normalize(b);
        if (left.Length == 0 || right.Length == 0)
        {
            return left.Length == right.Length ? 1 : 0;
        }

        var distance = LevenshteinDistance(left, right);
        var maxLen = Math.Max(left.Length, right.Length);
        return 1.0 - (double)distance / maxLen;
    }

    public static string Normalize(string value) =>
        string.Join(' ', value.ToUpperInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

    public static int LevenshteinDistance(string a, string b)
    {
        var dp = new int[a.Length + 1, b.Length + 1];
        for (var i = 0; i <= a.Length; i++)
        {
            dp[i, 0] = i;
        }
        for (var j = 0; j <= b.Length; j++)
        {
            dp[0, j] = j;
        }
        for (var i = 1; i <= a.Length; i++)
        {
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                dp[i, j] = Math.Min(Math.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1), dp[i - 1, j - 1] + cost);
            }
        }
        return dp[a.Length, b.Length];
    }
}
