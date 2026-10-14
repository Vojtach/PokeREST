using PokeREST.DTOs;

namespace PokeREST.Services;

/// <summary>
/// Service interface for managing Pokemon operations including searching, pagination, and trainer assignments.
/// </summary>
public interface IPokemonService
{
    /// <summary>
    /// Assigns an existing Pokemon to a trainer by setting the trainer as the Pokemon's owner.
    /// </summary>
    /// <param name="trainerId">The unique identifier of the trainer who will own the Pokemon.</param>
    /// <param name="pokemonId">The unique identifier of the Pokemon to be assigned.</param>
    /// <returns>The updated Pokemon information if successful; otherwise, null if the trainer or Pokemon doesn't exist, or the Pokemon is already owned.</returns>
    Task<PokemonResponseDto?> AddExistingPokemonToTrainerAsync(int trainerId, int pokemonId);
    
    /// <summary>
    /// Retrieves all Pokemon from the database with their complete information including moves and owner details.
    /// </summary>
    /// <returns>A list of all Pokemon in the system.</returns>
    Task<List<PokemonResponseDto>> GetAllPokemonAsync();
    
    /// <summary>
    /// Searches for Pokemon using fuzzy matching algorithms with multiple filter criteria.
    /// Supports partial name matching, type filtering, level ranges, and owner-based searches.
    /// Level and ownership filters are applied exactly; name, type and owner are OR-combined fuzzy matches.
    /// </summary>
    /// <param name="searchCriteria">The search parameters including name, type, level range, and ownership filters.</param>
    /// <returns>A list of Pokemon matching the search criteria, ordered by relevance score.</returns>
    Task<List<PokemonResponseDto>> SearchPokemonAsync(PokemonSearchDto searchCriteria);
    
    /// <summary>
    /// Retrieves Pokemon with advanced filtering, sorting, and pagination support.
    /// Filtering, sorting and paging run in the database. Text filters are case-insensitive substring matches,
    /// not fuzzy matches, and paging values are used as given without validation.
    /// </summary>
    /// <param name="searchCriteria">The paginated search parameters including filters, sorting, and pagination settings.</param>
    /// <returns>A paginated response containing Pokemon data and pagination metadata.</returns>
    Task<PaginatedResponse<PokemonResponseDto>> GetPaginatedPokemonAsync(PaginatedPokemonSearchDto searchCriteria);
}
