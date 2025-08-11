using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PokeREST.DTOs;

/// <summary>
/// Data transfer object for adding an existing Pokemon to a trainer.
/// </summary>
public class AddExistingPokemonDto
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "PokemonId must be a positive number")]
    [JsonPropertyName("pokemonId")]
    public int PokemonId { get; set; }
}

/// <summary>
/// Data transfer object representing Pokemon information in API responses.
/// </summary>
public class PokemonResponseDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("level")]
    public int Level { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("owner")]
    public string? Owner { get; set; }

    [JsonPropertyName("health")]
    public int Health { get; set; }

    [JsonPropertyName("caughtAt")]
    public DateTime? CaughtAt { get; set; }

    [JsonPropertyName("moves")]
    public List<MoveResponseDto> Moves { get; set; } = new();
}

/// <summary>
/// Data transfer object representing Pokemon move information in API responses.
/// </summary>
public class MoveResponseDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("power")]
    public int Power { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
}

/// <summary>
/// Data transfer object for Pokemon search criteria with fuzzy matching support.
/// </summary>
public class PokemonSearchDto
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("minLevel")]
    [Range(1, 100, ErrorMessage = "MinLevel must be between 1 and 100")]
    public int? MinLevel { get; set; }

    [JsonPropertyName("maxLevel")]
    [Range(1, 100, ErrorMessage = "MaxLevel must be between 1 and 100")]
    public int? MaxLevel { get; set; }

    [JsonPropertyName("owner")]
    public string? Owner { get; set; }

    [JsonPropertyName("hasOwner")]
    public bool? HasOwner { get; set; }

    public bool IsValid()
    {
        return MinLevel == null || MaxLevel == null || MinLevel <= MaxLevel;
    }
}

/// <summary>
/// Data transfer object for paginated Pokemon search with filtering, sorting, and pagination parameters.
/// </summary>
public class PaginatedPokemonSearchDto
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("minLevel")]
    [Range(1, 100, ErrorMessage = "MinLevel must be between 1 and 100")]
    public int? MinLevel { get; set; }

    [JsonPropertyName("maxLevel")]
    [Range(1, 100, ErrorMessage = "MaxLevel must be between 1 and 100")]
    public int? MaxLevel { get; set; }

    [JsonPropertyName("owner")]
    public string? Owner { get; set; }

    [JsonPropertyName("hasOwner")]
    public bool? HasOwner { get; set; }

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
        var validSortFields = new[] { "name", "level", "caughtAt", "type" };
        var validSortOrders = new[] { "asc", "desc" };
        
        return (MinLevel == null || MaxLevel == null || MinLevel <= MaxLevel) &&
               (string.IsNullOrEmpty(SortBy) || validSortFields.Contains(SortBy.ToLower())) &&
               (string.IsNullOrEmpty(SortOrder) || validSortOrders.Contains(SortOrder.ToLower()));
    }
}
