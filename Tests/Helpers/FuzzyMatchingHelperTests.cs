using FluentAssertions;
using PokeREST.Helpers;
using Xunit;

namespace PokeREST.Tests.Helpers;

public class FuzzyMatchingHelperTests
{
    #region LevenshteinDistance Tests

    [Fact]
    public void LevenshteinDistance_WithIdenticalStrings_ShouldReturnZero()
    {
        // Arrange
        const string source = "hello";
        const string target = "hello";

        // Act
        var distance = FuzzyMatchingHelper.LevenshteinDistance(source, target);

        // Assert
        distance.Should().Be(0);
    }

    [Fact]
    public void LevenshteinDistance_WithCompletelyDifferentStrings_ShouldReturnCorrectDistance()
    {
        // Arrange
        const string source = "abc";
        const string target = "def";

        // Act
        var distance = FuzzyMatchingHelper.LevenshteinDistance(source, target);

        // Assert
        distance.Should().Be(3); // All three characters need to be replaced
    }

    [Fact]
    public void LevenshteinDistance_WithOneCharacterDifference_ShouldReturnOne()
    {
        // Arrange
        const string source = "kitten";
        const string target = "sitten";

        // Act
        var distance = FuzzyMatchingHelper.LevenshteinDistance(source, target);

        // Assert
        distance.Should().Be(1);
    }

    [Fact]
    public void LevenshteinDistance_WithEmptyStrings_ShouldReturnZero()
    {
        // Arrange
        const string source = "";
        const string target = "";

        // Act
        var distance = FuzzyMatchingHelper.LevenshteinDistance(source, target);

        // Assert
        distance.Should().Be(0);
    }

    [Fact]
    public void LevenshteinDistance_WithOneEmptyString_ShouldReturnLengthOfOtherString()
    {
        // Arrange
        const string source = "";
        const string target = "hello";

        // Act
        var distance = FuzzyMatchingHelper.LevenshteinDistance(source, target);

        // Assert
        distance.Should().Be(5);
    }

    [Fact]
    public void LevenshteinDistance_WithNullStrings_ShouldHandleGracefully()
    {
        // Act
        var distance1 = FuzzyMatchingHelper.LevenshteinDistance(null, null);
        var distance2 = FuzzyMatchingHelper.LevenshteinDistance(null, "hello");
        var distance3 = FuzzyMatchingHelper.LevenshteinDistance("hello", null);

        // Assert
        distance1.Should().Be(0);
        distance2.Should().Be(5);
        distance3.Should().Be(5);
    }

    [Fact]
    public void LevenshteinDistance_WithTypicalMisspelling_ShouldReturnCorrectDistance()
    {
        // Arrange - Common Pokemon name misspellings
        const string source = "Pikachu";
        const string target = "Pikchu"; // Missing 'a'

        // Act
        var distance = FuzzyMatchingHelper.LevenshteinDistance(source, target);

        // Assert
        distance.Should().Be(1);
    }

    #endregion

    #region CalculateSimilarity Tests

    [Fact]
    public void CalculateSimilarity_WithIdenticalStrings_ShouldReturnOne()
    {
        // Arrange
        const string source = "Pikachu";
        const string target = "Pikachu";

        // Act
        var similarity = FuzzyMatchingHelper.CalculateSimilarity(source, target);

        // Assert
        similarity.Should().Be(1.0);
    }

    [Fact]
    public void CalculateSimilarity_WithCompletelyDifferentStrings_ShouldReturnLowValue()
    {
        // Arrange
        const string source = "abc";
        const string target = "xyz";

        // Act
        var similarity = FuzzyMatchingHelper.CalculateSimilarity(source, target);

        // Assert
        similarity.Should().Be(0.0);
    }

    [Fact]
    public void CalculateSimilarity_WithPartialMatch_ShouldReturnCorrectValue()
    {
        // Arrange
        const string source = "Pikachu";
        const string target = "Pikchu"; // Missing one character

        // Act
        var similarity = FuzzyMatchingHelper.CalculateSimilarity(source, target);

        // Assert
        similarity.Should().BeApproximately(0.857, 0.01); // 6/7 = ~0.857
    }

    [Fact]
    public void CalculateSimilarity_WithEmptyStrings_ShouldReturnOne()
    {
        // Act
        var similarity = FuzzyMatchingHelper.CalculateSimilarity("", "");

        // Assert
        similarity.Should().Be(1.0);
    }

    [Fact]
    public void CalculateSimilarity_WithOneEmptyString_ShouldReturnZero()
    {
        // Act
        var similarity1 = FuzzyMatchingHelper.CalculateSimilarity("", "hello");
        var similarity2 = FuzzyMatchingHelper.CalculateSimilarity("hello", "");

        // Assert
        similarity1.Should().Be(0.0);
        similarity2.Should().Be(0.0);
    }

    #endregion

    #region IsFuzzyMatch Tests

    [Fact]
    public void IsFuzzyMatch_WithExactMatch_ShouldReturnTrue()
    {
        // Arrange
        const string searchTerm = "Pikachu";
        const string target = "Pikachu";

        // Act
        var isMatch = FuzzyMatchingHelper.IsFuzzyMatch(searchTerm, target);

        // Assert
        isMatch.Should().BeTrue();
    }

    [Fact]
    public void IsFuzzyMatch_WithCaseInsensitiveMatch_ShouldReturnTrue()
    {
        // Arrange
        const string searchTerm = "pikachu";
        const string target = "PIKACHU";

        // Act
        var isMatch = FuzzyMatchingHelper.IsFuzzyMatch(searchTerm, target);

        // Assert
        isMatch.Should().BeTrue();
    }

    [Fact]
    public void IsFuzzyMatch_WithPartialMatch_ShouldReturnTrue()
    {
        // Arrange
        const string searchTerm = "Pika";
        const string target = "Pikachu";

        // Act
        var isMatch = FuzzyMatchingHelper.IsFuzzyMatch(searchTerm, target);

        // Assert
        isMatch.Should().BeTrue();
    }

    [Fact]
    public void IsFuzzyMatch_WithReversePartialMatch_ShouldReturnTrue()
    {
        // Arrange
        const string searchTerm = "Pikachu";
        const string target = "Pika";

        // Act
        var isMatch = FuzzyMatchingHelper.IsFuzzyMatch(searchTerm, target);

        // Assert
        isMatch.Should().BeTrue();
    }

    [Fact]
    public void IsFuzzyMatch_WithMinorTypo_ShouldReturnTrue()
    {
        // Arrange
        const string searchTerm = "Pikchu"; // Missing 'a'
        const string target = "Pikachu";

        // Act
        var isMatch = FuzzyMatchingHelper.IsFuzzyMatch(searchTerm, target);

        // Assert
        isMatch.Should().BeTrue();
    }

    [Fact]
    public void IsFuzzyMatch_WithMajorDifferences_ShouldReturnFalse()
    {
        // Arrange
        const string searchTerm = "Pikachu";
        const string target = "Squirtle";

        // Act
        var isMatch = FuzzyMatchingHelper.IsFuzzyMatch(searchTerm, target);

        // Assert
        isMatch.Should().BeFalse();
    }

    [Fact]
    public void IsFuzzyMatch_WithCustomThreshold_ShouldRespectThreshold()
    {
        // Arrange
        const string searchTerm = "Pika";
        const string target = "Pichu"; // Some similarity but not high
        const double highThreshold = 0.9;

        // Act
        var isMatch = FuzzyMatchingHelper.IsFuzzyMatch(searchTerm, target, highThreshold);

        // Assert
        isMatch.Should().BeFalse(); // Should fail with high threshold
    }

    [Fact]
    public void IsFuzzyMatch_WithWhitespace_ShouldHandleCorrectly()
    {
        // Arrange
        const string searchTerm = "  Pikachu  ";
        const string target = "Pikachu";

        // Act
        var isMatch = FuzzyMatchingHelper.IsFuzzyMatch(searchTerm, target);

        // Assert
        isMatch.Should().BeTrue();
    }

    [Fact]
    public void IsFuzzyMatch_WithEmptyOrNullStrings_ShouldReturnFalse()
    {
        // Act
        var isMatch1 = FuzzyMatchingHelper.IsFuzzyMatch("", "Pikachu");
        var isMatch2 = FuzzyMatchingHelper.IsFuzzyMatch("Pikachu", "");
        var isMatch3 = FuzzyMatchingHelper.IsFuzzyMatch(null, "Pikachu");
        var isMatch4 = FuzzyMatchingHelper.IsFuzzyMatch("Pikachu", null);

        // Assert
        isMatch1.Should().BeFalse();
        isMatch2.Should().BeFalse();
        isMatch3.Should().BeFalse();
        isMatch4.Should().BeFalse();
    }

    #endregion

    #region GetMatchScore Tests

    [Fact]
    public void GetMatchScore_WithExactMatch_ShouldReturnHighestScore()
    {
        // Arrange
        const string searchTerm = "Pikachu";
        const string target = "Pikachu";

        // Act
        var score = FuzzyMatchingHelper.GetMatchScore(searchTerm, target);

        // Assert
        score.Should().Be(100.0);
    }

    [Fact]
    public void GetMatchScore_WithStartsWith_ShouldReturnHighScore()
    {
        // Arrange
        const string searchTerm = "Pika";
        const string target = "Pikachu";

        // Act
        var score = FuzzyMatchingHelper.GetMatchScore(searchTerm, target);

        // Assert
        score.Should().Be(90.0);
    }

    [Fact]
    public void GetMatchScore_WithContains_ShouldReturnGoodScore()
    {
        // Arrange
        const string searchTerm = "chu";
        const string target = "Pikachu";

        // Act
        var score = FuzzyMatchingHelper.GetMatchScore(searchTerm, target);

        // Assert
        score.Should().Be(80.0);
    }

    [Fact]
    public void GetMatchScore_WithReverseContains_ShouldReturnDecentScore()
    {
        // Arrange
        const string searchTerm = "Pikachu";
        const string target = "Pika";

        // Act
        var score = FuzzyMatchingHelper.GetMatchScore(searchTerm, target);

        // Assert
        score.Should().Be(70.0);
    }

    [Fact]
    public void GetMatchScore_WithFuzzyMatch_ShouldReturnScaledScore()
    {
        // Arrange
        const string searchTerm = "Pikchu"; // Missing 'a' - high similarity
        const string target = "Pikachu";

        // Act
        var score = FuzzyMatchingHelper.GetMatchScore(searchTerm, target);

        // Assert
        score.Should().BeGreaterThan(50.0); // Should get a reasonable fuzzy match score
        score.Should().BeLessThan(70.0); // But less than reverse contains
    }

    [Fact]
    public void GetMatchScore_WithCaseInsensitive_ShouldWorkCorrectly()
    {
        // Arrange
        const string searchTerm = "pikachu";
        const string target = "PIKACHU";

        // Act
        var score = FuzzyMatchingHelper.GetMatchScore(searchTerm, target);

        // Assert
        score.Should().Be(100.0);
    }

    [Fact]
    public void GetMatchScore_WithCompletelyDifferentStrings_ShouldReturnLowScore()
    {
        // Arrange
        const string searchTerm = "Pikachu";
        const string target = "Squirtle";

        // Act
        var score = FuzzyMatchingHelper.GetMatchScore(searchTerm, target);

        // Assert
        score.Should().BeLessThan(20.0); // Should be very low score
    }

    [Fact]
    public void GetMatchScore_WithEmptyOrNullStrings_ShouldReturnZero()
    {
        // Act
        var score1 = FuzzyMatchingHelper.GetMatchScore("", "Pikachu");
        var score2 = FuzzyMatchingHelper.GetMatchScore("Pikachu", "");
        var score3 = FuzzyMatchingHelper.GetMatchScore(null, "Pikachu");
        var score4 = FuzzyMatchingHelper.GetMatchScore("Pikachu", null);

        // Assert
        score1.Should().Be(0.0);
        score2.Should().Be(0.0);
        score3.Should().Be(0.0);
        score4.Should().Be(0.0);
    }

    [Theory]
    [InlineData("Pikachu", "Pikachu", 100.0)] // Exact match
    [InlineData("Pika", "Pikachu", 90.0)] // Starts with
    [InlineData("chu", "Pikachu", 80.0)] // Contains
    [InlineData("Pikachu", "Pika", 70.0)] // Reverse contains
    public void GetMatchScore_WithVariousScenarios_ShouldReturnExpectedScores(
        string searchTerm, string target, double expectedScore)
    {
        // Act
        var score = FuzzyMatchingHelper.GetMatchScore(searchTerm, target);

        // Assert
        score.Should().Be(expectedScore);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void FuzzyMatchingHelper_WithPokemonNames_ShouldWorkCorrectly()
    {
        // Arrange - Real Pokemon names with common misspellings
        var pokemonNames = new[] { "Pikachu", "Charizard", "Blastoise", "Venusaur" };
        var searchTerms = new[] { "Pika", "Chariz", "Blast", "Venus" };

        // Act & Assert
        for (int i = 0; i < pokemonNames.Length; i++)
        {
            var isMatch = FuzzyMatchingHelper.IsFuzzyMatch(searchTerms[i], pokemonNames[i]);
            var score = FuzzyMatchingHelper.GetMatchScore(searchTerms[i], pokemonNames[i]);
            
            isMatch.Should().BeTrue($"'{searchTerms[i]}' should match '{pokemonNames[i]}'");
            score.Should().BeGreaterThan(80.0, $"Score for '{searchTerms[i]}' vs '{pokemonNames[i]}' should be high");
        }
    }

    [Fact]
    public void FuzzyMatchingHelper_WithTypicalTypos_ShouldMatch()
    {
        // Arrange - Common typing mistakes
        var testCases = new[]
        {
            ("Pikachu", "Pikchu"), // Missing character
            ("Charizard", "Charizrd"), // Swapped characters
            ("Blastoise", "Blastois"), // Missing last character
            ("Squirtle", "Squirtel") // Swapped last characters
        };

        // Act & Assert
        foreach (var (original, typo) in testCases)
        {
            var isMatch = FuzzyMatchingHelper.IsFuzzyMatch(typo, original);
            var score = FuzzyMatchingHelper.GetMatchScore(typo, original);
            
            isMatch.Should().BeTrue($"'{typo}' should match '{original}'");
            score.Should().BeGreaterThan(40.0, $"Score for typo '{typo}' vs '{original}' should be reasonable");
        }
    }

    #endregion
}
