using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PokeREST.DTOs;

public class CreateTrainerDto
{
    [Required]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 100 characters")]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(50)]
    [JsonPropertyName("region")]
    public string? Region { get; set; }

    [Range(1, 99, ErrorMessage = "Age must be between 1 and 99")]
    [JsonPropertyName("age")]
    public int? Age { get; set; }
}

public class UpdateTrainerDto
{
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 100 characters")]
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [StringLength(50)]
    [JsonPropertyName("region")]
    public string? Region { get; set; }

    [Range(1, 99, ErrorMessage = "Age must be between 1 and 99")]
    [JsonPropertyName("age")]
    public int? Age { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Wins must be a non-negative number")]
    [JsonPropertyName("wins")]
    public int? Wins { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Losses must be a non-negative number")]
    [JsonPropertyName("losses")]
    public int? Losses { get; set; }
}

public class TrainerResponseDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("region")]
    public string? Region { get; set; }

    [JsonPropertyName("age")]
    public int? Age { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime? CreatedAt { get; set; }

    [JsonPropertyName("wins")]
    public int? Wins { get; set; }

    [JsonPropertyName("losses")]
    public int? Losses { get; set; }

    [JsonPropertyName("pokemonCount")]
    public int PokemonCount { get; set; }
}

public class TrainerWithPokemonDto : TrainerResponseDto
{
    [JsonPropertyName("pokemons")]
    public List<PokemonResponseDto> Pokemons { get; set; } = new();
}

public class PaginatedTrainerSearchDto
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("region")]
    public string? Region { get; set; }

    [JsonPropertyName("minAge")]
    [Range(1, 99, ErrorMessage = "MinAge must be between 1 and 99")]
    public int? MinAge { get; set; }

    [JsonPropertyName("maxAge")]
    [Range(1, 99, ErrorMessage = "MaxAge must be between 1 and 99")]
    public int? MaxAge { get; set; }

    [JsonPropertyName("minWins")]
    [Range(0, int.MaxValue, ErrorMessage = "MinWins must be non-negative")]
    public int? MinWins { get; set; }

    [JsonPropertyName("maxWins")]
    [Range(0, int.MaxValue, ErrorMessage = "MaxWins must be non-negative")]
    public int? MaxWins { get; set; }

    [JsonPropertyName("minPokemonCount")]
    [Range(0, int.MaxValue, ErrorMessage = "MinPokemonCount must be non-negative")]
    public int? MinPokemonCount { get; set; }

    [JsonPropertyName("maxPokemonCount")]
    [Range(0, int.MaxValue, ErrorMessage = "MaxPokemonCount must be non-negative")]
    public int? MaxPokemonCount { get; set; }

    [JsonPropertyName("pageNumber")]
    [Range(1, int.MaxValue, ErrorMessage = "Page number must be greater than 0")]
    public int PageNumber { get; set; } = 1;

    [JsonPropertyName("pageSize")]
    [Range(1, 100, ErrorMessage = "Page size must be between 1 and 100")]
    public int PageSize { get; set; } = 10;

    [JsonPropertyName("sortBy")]
    public string? SortBy { get; set; } = "name";

    [JsonPropertyName("sortOrder")]
    public string? SortOrder { get; set; } = "asc";

    public bool IsValid()
    {
        var validSortFields = new[] { "name", "region", "age", "wins", "losses", "pokemoncount", "createdat" };
        var validSortOrders = new[] { "asc", "desc" };
        
        return (MinAge == null || MaxAge == null || MinAge <= MaxAge) &&
               (MinWins == null || MaxWins == null || MinWins <= MaxWins) &&
               (MinPokemonCount == null || MaxPokemonCount == null || MinPokemonCount <= MaxPokemonCount) &&
               (string.IsNullOrEmpty(SortBy) || validSortFields.Contains(SortBy.ToLower())) &&
               (string.IsNullOrEmpty(SortOrder) || validSortOrders.Contains(SortOrder.ToLower()));
    }
}

