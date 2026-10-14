using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PokeREST.DTOs;

/// <summary>
/// Data transfer object for creating a new trainer.
/// </summary>
/// <remarks>
/// Request body for <c>POST /api/trainers</c>. Only <see cref="Name"/> is required. The server assigns the ID,
/// sets <c>createdAt</c> to the current UTC time and initializes <c>wins</c> and <c>losses</c> to 0.
/// </remarks>
/// <example>
/// <code>
/// POST /api/trainers
/// { "name": "Ash Ketchum", "region": "Kanto", "age": 10 }
/// </code>
/// </example>
public class CreateTrainerDto
{
    /// <summary>
    /// Display name of the trainer.
    /// </summary>
    /// <remarks>Required. Must be 1-100 characters long.</remarks>
    /// <example>"Ash Ketchum"</example>
    [Required]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 100 characters")]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Home region of the trainer.
    /// </summary>
    /// <remarks>Optional. At most 50 characters.</remarks>
    /// <example>"Kanto"</example>
    [StringLength(50)]
    [JsonPropertyName("region")]
    public string? Region { get; set; }

    /// <summary>
    /// Age of the trainer in years.
    /// </summary>
    /// <remarks>Optional. Must be between 1 and 99.</remarks>
    /// <example>10</example>
    [Range(1, 99, ErrorMessage = "Age must be between 1 and 99")]
    [JsonPropertyName("age")]
    public int? Age { get; set; }
}

/// <summary>
/// Data transfer object for updating an existing trainer. All fields are optional.
/// </summary>
/// <remarks>
/// Request body for <c>PATCH /api/trainers/{id}</c>. Performs a partial update: only fields that are present
/// are applied, while omitted or <c>null</c> fields leave the stored value unchanged. An empty <see cref="Name"/>
/// is also ignored. Because <c>null</c> means "unchanged", optional fields cannot be cleared through this DTO.
/// </remarks>
/// <example>
/// Change only the trainer's region and win count:
/// <code>
/// PATCH /api/trainers/1
/// { "region": "Johto", "wins": 5 }
/// </code>
/// </example>
public class UpdateTrainerDto
{
    /// <summary>
    /// New display name of the trainer.
    /// </summary>
    /// <remarks>Optional. When provided, must be 1-100 characters long.</remarks>
    /// <example>"Ash"</example>
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 100 characters")]
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// New home region of the trainer.
    /// </summary>
    /// <remarks>Optional. At most 50 characters.</remarks>
    /// <example>"Johto"</example>
    [StringLength(50)]
    [JsonPropertyName("region")]
    public string? Region { get; set; }

    /// <summary>
    /// New age of the trainer in years.
    /// </summary>
    /// <remarks>Optional. Must be between 1 and 99.</remarks>
    [Range(1, 99, ErrorMessage = "Age must be between 1 and 99")]
    [JsonPropertyName("age")]
    public int? Age { get; set; }

    /// <summary>
    /// New total number of battles won. Replaces the stored value (it is not added to it).
    /// </summary>
    /// <remarks>Optional. Must be 0 or greater.</remarks>
    [Range(0, int.MaxValue, ErrorMessage = "Wins must be a non-negative number")]
    [JsonPropertyName("wins")]
    public int? Wins { get; set; }

    /// <summary>
    /// New total number of battles lost. Replaces the stored value (it is not added to it).
    /// </summary>
    /// <remarks>Optional. Must be 0 or greater.</remarks>
    [Range(0, int.MaxValue, ErrorMessage = "Losses must be a non-negative number")]
    [JsonPropertyName("losses")]
    public int? Losses { get; set; }
}

/// <summary>
/// Data transfer object representing trainer information in API responses.
/// </summary>
/// <remarks>
/// Returned by trainer create, update, list and paginated search endpoints. Includes the number of owned
/// Pokemon but not the Pokemon themselves; see <see cref="TrainerWithPokemonDto"/> for the detailed view.
/// </remarks>
/// <example>
/// <code>
/// {
///   "id": 1,
///   "name": "Ash Ketchum",
///   "region": "Kanto",
///   "age": 10,
///   "createdAt": "2024-05-01T09:00:00",
///   "wins": 5,
///   "losses": 2,
///   "pokemonCount": 6
/// }
/// </code>
/// </example>
public class TrainerResponseDto
{
    /// <summary>
    /// Unique identifier of the trainer.
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// Display name of the trainer.
    /// </summary>
    /// <example>"Ash Ketchum"</example>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Home region of the trainer, or <c>null</c> if not set.
    /// </summary>
    /// <example>"Kanto"</example>
    [JsonPropertyName("region")]
    public string? Region { get; set; }

    /// <summary>
    /// Age of the trainer in years, or <c>null</c> if not set.
    /// </summary>
    [JsonPropertyName("age")]
    public int? Age { get; set; }

    /// <summary>
    /// When the trainer was created, stored as UTC without time zone information.
    /// </summary>
    /// <example>"2024-05-01T09:00:00"</example>
    [JsonPropertyName("createdAt")]
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// Total number of battles won. New trainers start at 0.
    /// </summary>
    [JsonPropertyName("wins")]
    public int? Wins { get; set; }

    /// <summary>
    /// Total number of battles lost. New trainers start at 0.
    /// </summary>
    [JsonPropertyName("losses")]
    public int? Losses { get; set; }

    /// <summary>
    /// Number of Pokemon currently owned by the trainer.
    /// </summary>
    [JsonPropertyName("pokemonCount")]
    public int PokemonCount { get; set; }
}

/// <summary>
/// Data transfer object representing trainer information with detailed Pokemon collection in API responses.
/// </summary>
/// <remarks>Returned by <c>GET /api/trainers/{id}</c>. Contains every field of <see cref="TrainerResponseDto"/> plus the owned Pokemon.</remarks>
/// <example>
/// <code>
/// {
///   "id": 1,
///   "name": "Ash Ketchum",
///   ...
///   "pokemonCount": 1,
///   "pokemons": [ { "id": 25, "name": "Pikachu", "level": 12, "type": "Electric", ... } ]
/// }
/// </code>
/// </example>
public class TrainerWithPokemonDto : TrainerResponseDto
{
    /// <summary>
    /// Full details of every Pokemon owned by the trainer, including their moves. Empty list when the trainer owns none.
    /// </summary>
    [JsonPropertyName("pokemons")]
    public List<PokemonResponseDto> Pokemons { get; set; } = new();
}

/// <summary>
/// Data transfer object for paginated trainer search with comprehensive filtering, sorting, and pagination parameters.
/// </summary>
/// <remarks>
/// Built from the query string of <c>GET /api/trainers/paginated</c>. All filters are optional and combined with
/// AND semantics; text filters use case-insensitive substring ("contains") matching and all range bounds are inclusive.
/// <para>Call <see cref="IsValid"/> to check cross-field and sort-option rules not covered by data annotations.</para>
/// </remarks>
/// <example>
/// <code>
/// GET /api/trainers/paginated?region=kanto&amp;minWins=3&amp;minPokemonCount=1&amp;sortBy=wins&amp;sortOrder=desc&amp;pageSize=5
/// </code>
/// </example>
public class PaginatedTrainerSearchDto
{
    /// <summary>
    /// Substring to match against the trainer's name (case-insensitive).
    /// </summary>
    /// <example>"ash"</example>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Substring to match against the trainer's region (case-insensitive). Trainers without a region never match.
    /// </summary>
    /// <example>"kanto"</example>
    [JsonPropertyName("region")]
    public string? Region { get; set; }

    /// <summary>
    /// Inclusive lower bound on the trainer's age.
    /// </summary>
    /// <remarks>Optional. Must be between 1 and 99, and not greater than <see cref="MaxAge"/> when both are set.</remarks>
    [JsonPropertyName("minAge")]
    [Range(1, 99, ErrorMessage = "MinAge must be between 1 and 99")]
    public int? MinAge { get; set; }

    /// <summary>
    /// Inclusive upper bound on the trainer's age.
    /// </summary>
    /// <remarks>Optional. Must be between 1 and 99, and not less than <see cref="MinAge"/> when both are set.</remarks>
    [JsonPropertyName("maxAge")]
    [Range(1, 99, ErrorMessage = "MaxAge must be between 1 and 99")]
    public int? MaxAge { get; set; }

    /// <summary>
    /// Inclusive lower bound on the number of battles won.
    /// </summary>
    /// <remarks>Optional. Must be 0 or greater, and not greater than <see cref="MaxWins"/> when both are set.</remarks>
    [JsonPropertyName("minWins")]
    [Range(0, int.MaxValue, ErrorMessage = "MinWins must be non-negative")]
    public int? MinWins { get; set; }

    /// <summary>
    /// Inclusive upper bound on the number of battles won.
    /// </summary>
    /// <remarks>Optional. Must be 0 or greater, and not less than <see cref="MinWins"/> when both are set.</remarks>
    [JsonPropertyName("maxWins")]
    [Range(0, int.MaxValue, ErrorMessage = "MaxWins must be non-negative")]
    public int? MaxWins { get; set; }

    /// <summary>
    /// Inclusive lower bound on the number of Pokemon the trainer owns.
    /// </summary>
    /// <remarks>Optional. Must be 0 or greater, and not greater than <see cref="MaxPokemonCount"/> when both are set.</remarks>
    [JsonPropertyName("minPokemonCount")]
    [Range(0, int.MaxValue, ErrorMessage = "MinPokemonCount must be non-negative")]
    public int? MinPokemonCount { get; set; }

    /// <summary>
    /// Inclusive upper bound on the number of Pokemon the trainer owns.
    /// </summary>
    /// <remarks>Optional. Must be 0 or greater, and not less than <see cref="MinPokemonCount"/> when both are set.</remarks>
    [JsonPropertyName("maxPokemonCount")]
    [Range(0, int.MaxValue, ErrorMessage = "MaxPokemonCount must be non-negative")]
    public int? MaxPokemonCount { get; set; }

    /// <summary>
    /// 1-based page number to return. Defaults to 1.
    /// </summary>
    /// <remarks>Must be 1 or greater. Pages past the last page return an empty list.</remarks>
    [JsonPropertyName("pageNumber")]
    [Range(1, int.MaxValue, ErrorMessage = "Page number must be greater than 0")]
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Number of items per page. Defaults to 10.
    /// </summary>
    /// <remarks>Must be between 1 and 100.</remarks>
    [JsonPropertyName("pageSize")]
    [Range(1, 100, ErrorMessage = "Page size must be between 1 and 100")]
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// Field to sort by. Defaults to <c>"name"</c>.
    /// </summary>
    /// <remarks>
    /// Case-insensitive; one of <c>name</c>, <c>region</c>, <c>age</c>, <c>wins</c>, <c>losses</c>,
    /// <c>pokemonCount</c>, <c>createdAt</c>.
    /// </remarks>
    [JsonPropertyName("sortBy")]
    public string? SortBy { get; set; } = "name";

    /// <summary>
    /// Sort direction. Defaults to <c>"asc"</c>.
    /// </summary>
    /// <remarks>Case-insensitive; either <c>asc</c> or <c>desc</c>.</remarks>
    [JsonPropertyName("sortOrder")]
    public string? SortOrder { get; set; } = "asc";

    /// <summary>
    /// Validates cross-field rules and sort options that data annotations cannot express.
    /// </summary>
    /// <returns>
    /// <c>true</c> when each min/max pair (age, wins, Pokemon count) is ordered correctly where both are set,
    /// and <see cref="SortBy"/> and <see cref="SortOrder"/> are empty or one of the allowed values.
    /// </returns>
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
