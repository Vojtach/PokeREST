using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PokeREST.DTOs;

/// <summary>
/// Data transfer object for adding an existing Pokemon to a trainer.
/// </summary>
/// <remarks>
/// Request body for <c>POST /api/trainers/{trainerId}/pokemon</c>. The Pokemon must already exist and must not
/// have an owner; on success its owner is set to the trainer and its <c>caughtAt</c> timestamp is set to the current UTC time.
/// </remarks>
/// <example>
/// <code>
/// POST /api/trainers/1/pokemon
/// { "pokemonId": 25 }
/// </code>
/// </example>
public class AddExistingPokemonDto
{
    /// <summary>
    /// ID of the existing, currently unowned Pokemon to assign to the trainer.
    /// </summary>
    /// <remarks>Required. Must be a positive integer (1 or greater).</remarks>
    /// <example>25</example>
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "PokemonId must be a positive number")]
    [JsonPropertyName("pokemonId")]
    public int PokemonId { get; set; }
}

/// <summary>
/// Data transfer object representing Pokemon information in API responses.
/// </summary>
/// <remarks>
/// Flattens the Pokemon entity: the type and owner are returned by name rather than by ID,
/// and the learned moves are embedded as <see cref="MoveResponseDto"/> items.
/// </remarks>
/// <example>
/// <code>
/// {
///   "id": 25,
///   "name": "Pikachu",
///   "level": 12,
///   "type": "Electric",
///   "owner": "Ash Ketchum",
///   "health": 35,
///   "caughtAt": "2024-05-01T10:15:00",
///   "moves": [ { "id": 3, "name": "Thunder Shock", "power": 40, "type": "Electric" } ]
/// }
/// </code>
/// </example>
public class PokemonResponseDto
{
    /// <summary>
    /// Unique identifier of the Pokemon.
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// Name of the Pokemon (up to 100 characters).
    /// </summary>
    /// <example>"Pikachu"</example>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Current level of the Pokemon, typically 1-100.
    /// </summary>
    [JsonPropertyName("level")]
    public int Level { get; set; }

    /// <summary>
    /// Name of the Pokemon's elemental type.
    /// </summary>
    /// <example>"Electric"</example>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Name of the trainer who owns the Pokemon, or <c>null</c> if the Pokemon is wild (unowned).
    /// </summary>
    /// <example>"Ash Ketchum"</example>
    [JsonPropertyName("owner")]
    public string? Owner { get; set; }

    /// <summary>
    /// Health points (HP) of the Pokemon.
    /// </summary>
    [JsonPropertyName("health")]
    public int Health { get; set; }

    /// <summary>
    /// When the Pokemon was caught, stored as UTC without time zone information. May be <c>null</c>.
    /// </summary>
    /// <example>"2024-05-01T10:15:00"</example>
    [JsonPropertyName("caughtAt")]
    public DateTime? CaughtAt { get; set; }

    /// <summary>
    /// Moves the Pokemon has learned. Empty list when the Pokemon knows no moves.
    /// </summary>
    [JsonPropertyName("moves")]
    public List<MoveResponseDto> Moves { get; set; } = new();
}

/// <summary>
/// Data transfer object representing Pokemon move information in API responses.
/// </summary>
/// <remarks>Embedded in <see cref="PokemonResponseDto.Moves"/>.</remarks>
/// <example>
/// <code>
/// { "id": 3, "name": "Thunder Shock", "power": 40, "type": "Electric" }
/// </code>
/// </example>
public class MoveResponseDto
{
    /// <summary>
    /// Unique identifier of the move.
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// Name of the move (up to 100 characters).
    /// </summary>
    /// <example>"Thunder Shock"</example>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Base power of the move.
    /// </summary>
    /// <example>40</example>
    [JsonPropertyName("power")]
    public int Power { get; set; }

    /// <summary>
    /// Name of the move's elemental type.
    /// </summary>
    /// <example>"Electric"</example>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
}

/// <summary>
/// Data transfer object for Pokemon search criteria with fuzzy matching support.
/// </summary>
/// <remarks>
/// Built from the query string of <c>GET /api/pokemon</c> when at least one criterion is supplied; with no criteria
/// the endpoint returns all Pokemon. All fields are optional. Level and ownership filters are AND-combined, while the
/// text criteria are OR-combined: a Pokemon is kept if any supplied text criterion matches.
/// <para>
/// Text criteria (<see cref="Name"/>, <see cref="Type"/>, <see cref="Owner"/>) use case-insensitive fuzzy matching,
/// so small typos still match. Results are ordered by relevance score, where type matches are weighted at 0.8
/// and owner matches at 0.6 relative to name matches.
/// </para>
/// <para>Call <see cref="IsValid"/> to check cross-field rules not covered by data annotations.</para>
/// <para>
/// The controller builds this DTO manually from query parameters, so its <c>[Range]</c> annotations are not
/// evaluated; only <see cref="IsValid"/> is checked.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// GET /api/pokemon?name=pikchu&amp;minLevel=5&amp;maxLevel=30&amp;hasOwner=true
/// </code>
/// </example>
public class PokemonSearchDto
{
    /// <summary>
    /// Pokemon name to search for (fuzzy, case-insensitive).
    /// </summary>
    /// <example>"pikachu"</example>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Elemental type name to search for (fuzzy, case-insensitive).
    /// </summary>
    /// <example>"fire"</example>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Inclusive lower bound on the Pokemon's level.
    /// </summary>
    /// <remarks>Optional. Must not be greater than <see cref="MaxLevel"/> when both are set. The annotated range (between 1 and 100) is not enforced.</remarks>
    [JsonPropertyName("minLevel")]
    [Range(1, 100, ErrorMessage = "MinLevel must be between 1 and 100")]
    public int? MinLevel { get; set; }

    /// <summary>
    /// Inclusive upper bound on the Pokemon's level.
    /// </summary>
    /// <remarks>Optional. Must not be less than <see cref="MinLevel"/> when both are set. The annotated range (between 1 and 100) is not enforced.</remarks>
    [JsonPropertyName("maxLevel")]
    [Range(1, 100, ErrorMessage = "MaxLevel must be between 1 and 100")]
    public int? MaxLevel { get; set; }

    /// <summary>
    /// Owning trainer's name to search for (fuzzy, case-insensitive). Unowned Pokemon never match this criterion.
    /// </summary>
    /// <example>"ash"</example>
    [JsonPropertyName("owner")]
    public string? Owner { get; set; }

    /// <summary>
    /// Ownership filter: <c>true</c> returns only owned Pokemon, <c>false</c> only wild Pokemon, <c>null</c> both.
    /// </summary>
    [JsonPropertyName("hasOwner")]
    public bool? HasOwner { get; set; }

    /// <summary>
    /// Validates cross-field rules that data annotations cannot express.
    /// </summary>
    /// <returns><c>true</c> unless both level bounds are set and <see cref="MinLevel"/> exceeds <see cref="MaxLevel"/>.</returns>
    public bool IsValid()
    {
        return MinLevel == null || MaxLevel == null || MinLevel <= MaxLevel;
    }
}

/// <summary>
/// Data transfer object for paginated Pokemon search with filtering, sorting, and pagination parameters.
/// </summary>
/// <remarks>
/// Built from the query string of <c>GET /api/pokemon/paginated</c>. Unlike <see cref="PokemonSearchDto"/>, text
/// filters here use case-insensitive substring ("contains") matching rather than fuzzy matching, and results are
/// ordered by <see cref="SortBy"/> rather than relevance. All filters are optional and combined with AND semantics.
/// <para>Call <see cref="IsValid"/> to check cross-field and sort-option rules not covered by data annotations.</para>
/// <para>
/// The controller builds this DTO manually from query parameters, so its <c>[Range]</c> annotations are not
/// evaluated; only <see cref="IsValid"/> is checked.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// GET /api/pokemon/paginated?type=water&amp;minLevel=10&amp;pageNumber=2&amp;pageSize=20&amp;sortBy=level&amp;sortOrder=desc
/// </code>
/// </example>
public class PaginatedPokemonSearchDto
{
    /// <summary>
    /// Substring to match against the Pokemon's name (case-insensitive).
    /// </summary>
    /// <example>"chu"</example>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Substring to match against the Pokemon's type name (case-insensitive).
    /// </summary>
    /// <example>"water"</example>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Inclusive lower bound on the Pokemon's level.
    /// </summary>
    /// <remarks>Optional. Must not be greater than <see cref="MaxLevel"/> when both are set. The annotated range (between 1 and 100) is not enforced.</remarks>
    [JsonPropertyName("minLevel")]
    [Range(1, 100, ErrorMessage = "MinLevel must be between 1 and 100")]
    public int? MinLevel { get; set; }

    /// <summary>
    /// Inclusive upper bound on the Pokemon's level.
    /// </summary>
    /// <remarks>Optional. Must not be less than <see cref="MinLevel"/> when both are set. The annotated range (between 1 and 100) is not enforced.</remarks>
    [JsonPropertyName("maxLevel")]
    [Range(1, 100, ErrorMessage = "MaxLevel must be between 1 and 100")]
    public int? MaxLevel { get; set; }

    /// <summary>
    /// Substring to match against the owning trainer's name (case-insensitive). Unowned Pokemon never match.
    /// </summary>
    /// <example>"misty"</example>
    [JsonPropertyName("owner")]
    public string? Owner { get; set; }

    /// <summary>
    /// Ownership filter: <c>true</c> returns only owned Pokemon, <c>false</c> only wild Pokemon, <c>null</c> both.
    /// </summary>
    [JsonPropertyName("hasOwner")]
    public bool? HasOwner { get; set; }

    /// <summary>
    /// 1-based page number to return. Defaults to 1.
    /// </summary>
    /// <remarks>
    /// Intended to be 1 or greater, but not validated: values below 1 cause a server error (500).
    /// Pages past the last page return an empty list.
    /// </remarks>
    [JsonPropertyName("pageNumber")]
    [Range(1, int.MaxValue, ErrorMessage = "Page number must be greater than 0")]
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Number of items per page. Defaults to 10.
    /// </summary>
    /// <remarks>The intended range is 1-100, but it is not validated or capped.</remarks>
    [JsonPropertyName("pageSize")]
    [Range(1, 100, ErrorMessage = "Page size must be between 1 and 100")]
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// Field to sort by. Defaults to <c>"name"</c>.
    /// </summary>
    /// <remarks>
    /// Case-insensitive; one of <c>name</c>, <c>level</c>, <c>type</c> (sorts by type name).
    /// <c>caughtAt</c> is in the whitelist but is currently always rejected: <see cref="IsValid"/> lower-cases the
    /// input before comparing it with the mixed-case entry.
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
    /// <c>true</c> when <see cref="MinLevel"/> does not exceed <see cref="MaxLevel"/> (if both are set),
    /// and <see cref="SortBy"/> and <see cref="SortOrder"/> are empty or one of the allowed values.
    /// <c>caughtAt</c> never passes; see <see cref="SortBy"/>.
    /// </returns>
    public bool IsValid()
    {
        var validSortFields = new[] { "name", "level", "caughtAt", "type" };
        var validSortOrders = new[] { "asc", "desc" };

        return (MinLevel == null || MaxLevel == null || MinLevel <= MaxLevel) &&
               (string.IsNullOrEmpty(SortBy) || validSortFields.Contains(SortBy.ToLower())) &&
               (string.IsNullOrEmpty(SortOrder) || validSortOrders.Contains(SortOrder.ToLower()));
    }
}
