using System.Globalization;

namespace PokeREST.Helpers;

public static class FuzzyMatchingHelper
{
    /// <summary>
    /// Calculates the Levenshtein distance between two strings
    /// </summary>
    /// <param name="source">First string</param>
    /// <param name="target">Second string</param>
    /// <returns>The number of single-character edits required to change source into target</returns>
    public static int LevenshteinDistance(string source, string target)
    {
        if (string.IsNullOrEmpty(source))
            return string.IsNullOrEmpty(target) ? 0 : target.Length;

        if (string.IsNullOrEmpty(target))
            return source.Length;

        var sourceLength = source.Length;
        var targetLength = target.Length;
        var distance = new int[sourceLength + 1, targetLength + 1];

        // Initialize the distance matrix
        for (var i = 0; i <= sourceLength; distance[i, 0] = i++) { }
        for (var j = 0; j <= targetLength; distance[0, j] = j++) { }

        for (var i = 1; i <= sourceLength; i++)
        {
            for (var j = 1; j <= targetLength; j++)
            {
                var cost = target[j - 1] == source[i - 1] ? 0 : 1;
                distance[i, j] = Math.Min(
                    Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1),
                    distance[i - 1, j - 1] + cost);
            }
        }

        return distance[sourceLength, targetLength];
    }

    /// <summary>
    /// Calculates a similarity score between two strings (0.0 to 1.0)
    /// </summary>
    /// <param name="source">First string</param>
    /// <param name="target">Second string</param>
    /// <returns>Similarity score where 1.0 is exact match and 0.0 is completely different</returns>
    public static double CalculateSimilarity(string source, string target)
    {
        if (string.IsNullOrEmpty(source) && string.IsNullOrEmpty(target))
            return 1.0;

        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target))
            return 0.0;

        var maxLength = Math.Max(source.Length, target.Length);
        var distance = LevenshteinDistance(source, target);
        return 1.0 - (double)distance / maxLength;
    }

    /// <summary>
    /// Checks if a search term matches a target string with fuzzy matching
    /// </summary>
    /// <param name="searchTerm">The term being searched for</param>
    /// <param name="targetString">The string to match against</param>
    /// <param name="threshold">Minimum similarity threshold (default 0.6)</param>
    /// <returns>True if the strings match within the threshold</returns>
    public static bool IsFuzzyMatch(string searchTerm, string targetString, double threshold = 0.6)
    {
        if (string.IsNullOrWhiteSpace(searchTerm) || string.IsNullOrWhiteSpace(targetString))
            return false;

        var normalizedSearchTerm = searchTerm.Trim().ToLowerInvariant();
        var normalizedTarget = targetString.Trim().ToLowerInvariant();

        // Exact match
        if (normalizedTarget.Equals(normalizedSearchTerm))
            return true;

        // Contains match (partial)
        if (normalizedTarget.Contains(normalizedSearchTerm) || normalizedSearchTerm.Contains(normalizedTarget))
            return true;

        // Fuzzy match using similarity score
        var similarity = CalculateSimilarity(normalizedSearchTerm, normalizedTarget);
        return similarity >= threshold;
    }

    /// <summary>
    /// Gets a match score for ranking search results
    /// Higher score = better match
    /// </summary>
    /// <param name="searchTerm">The term being searched for</param>
    /// <param name="targetString">The string to match against</param>
    /// <returns>Match score (higher is better)</returns>
    public static double GetMatchScore(string searchTerm, string targetString)
    {
        if (string.IsNullOrWhiteSpace(searchTerm) || string.IsNullOrWhiteSpace(targetString))
            return 0.0;

        var normalizedSearchTerm = searchTerm.Trim().ToLowerInvariant();
        var normalizedTarget = targetString.Trim().ToLowerInvariant();

        // Exact match gets highest score
        if (normalizedTarget.Equals(normalizedSearchTerm))
            return 100.0;

        // Starts with search term gets high score
        if (normalizedTarget.StartsWith(normalizedSearchTerm))
            return 90.0;

        // Contains search term gets good score
        if (normalizedTarget.Contains(normalizedSearchTerm))
            return 80.0;

        // Search term contains target (partial match from other direction)
        if (normalizedSearchTerm.Contains(normalizedTarget))
            return 70.0;

        // Fuzzy match score based on similarity
        var similarity = CalculateSimilarity(normalizedSearchTerm, normalizedTarget);
        return similarity * 60.0; // Scale to 0-60 range for fuzzy matches
    }
}
