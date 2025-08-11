using Microsoft.AspNetCore.Mvc;
using PokeREST.DTOs;
using PokeREST.Services;

namespace PokeREST.Controllers;

/// <summary>
/// Controller for managing Pokemon operations including trainer assignments, searching, and pagination.
/// Provides endpoints for adding Pokemon to trainers, searching with fuzzy matching, and paginated retrieval.
/// </summary>
[ApiController]
[Route("api")]
[Produces("application/json")]
public class PokemonController : ControllerBase
{
    private readonly IPokemonService _pokemonService;
    private readonly ITrainerService _trainerService;

    public PokemonController(IPokemonService pokemonService, ITrainerService trainerService)
    {
        _pokemonService = pokemonService;
        _trainerService = trainerService;
    }

    /// <summary>
    /// Adds an existing Pokemon to a trainer
    /// </summary>
    /// <param name="trainerId">Trainer ID</param>
    /// <param name="addExistingPokemonDto">Pokemon assignment data</param>
    /// <returns>Updated Pokemon information</returns>
    [HttpPost("trainers/{trainerId}/pokemon")]
    [ProducesResponseType(typeof(ApiResponse<PokemonResponseDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<ActionResult<ApiResponse<PokemonResponseDto>>> AddPokemonToTrainer(int trainerId, [FromBody] AddExistingPokemonDto addExistingPokemonDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("Invalid input data", 400));
        }

        try
        {
            // Check if trainer exists
            var trainerExists = await _trainerService.TrainerExistsAsync(trainerId);
            if (!trainerExists)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Trainer not found", 404));
            }

            var pokemon = await _pokemonService.AddExistingPokemonToTrainerAsync(trainerId, addExistingPokemonDto.PokemonId);
            if (pokemon == null)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("Failed to add Pokemon to trainer. Please check if the Pokemon ID is valid.", 400));
            }

            return Ok(ApiResponse<PokemonResponseDto>.SuccessResponse(pokemon, "Pokemon added to trainer successfully"));
        }
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error", 500));
        }
    }

    /// <summary>
    /// Gets all Pokemon or searches Pokemon with optional filters
    /// </summary>
    /// <param name="name">Pokemon name (supports partial matching and typos)</param>
    /// <param name="type">Pokemon type (supports partial matching)</param>
    /// <param name="minLevel">Minimum level filter</param>
    /// <param name="maxLevel">Maximum level filter</param>
    /// <param name="owner">Owner name (supports partial matching)</param>
    /// <param name="hasOwner">Filter by ownership status</param>
    /// <returns>List of Pokemon matching the search criteria</returns>
    [HttpGet("pokemon")]
    [ProducesResponseType(typeof(ApiResponse<List<PokemonResponseDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<ActionResult<ApiResponse<List<PokemonResponseDto>>>> GetAllPokemon(
        [FromQuery] string? name = null,
        [FromQuery] string? type = null,
        [FromQuery] int? minLevel = null,
        [FromQuery] int? maxLevel = null,
        [FromQuery] string? owner = null,
        [FromQuery] bool? hasOwner = null)
    {
        try
        {
            // Check if any search criteria are provided
            var hasSearchCriteria = !string.IsNullOrWhiteSpace(name) ||
                                   !string.IsNullOrWhiteSpace(type) ||
                                   minLevel.HasValue ||
                                   maxLevel.HasValue ||
                                   !string.IsNullOrWhiteSpace(owner) ||
                                   hasOwner.HasValue;

            if (hasSearchCriteria)
            {
                // Create search criteria object
                var searchCriteria = new PokemonSearchDto
                {
                    Name = name,
                    Type = type,
                    MinLevel = minLevel,
                    MaxLevel = maxLevel,
                    Owner = owner,
                    HasOwner = hasOwner
                };

                // Validate search criteria
                if (!searchCriteria.IsValid())
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("Invalid search criteria: MinLevel must be less than or equal to MaxLevel", 400));
                }

                var searchResults = await _pokemonService.SearchPokemonAsync(searchCriteria);
                var message = searchResults.Any() 
                    ? $"Found {searchResults.Count} Pokemon matching search criteria"
                    : "No Pokemon found matching search criteria";
                
                return Ok(ApiResponse<List<PokemonResponseDto>>.SuccessResponse(searchResults, message));
            }
            else
            {
                // No search criteria, return all Pokemon
                var pokemon = await _pokemonService.GetAllPokemonAsync();
                return Ok(ApiResponse<List<PokemonResponseDto>>.SuccessResponse(pokemon, "Pokemon retrieved successfully"));
            }
        }
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error", 500));
        }
    }

    /// <summary>
    /// Gets paginated Pokemon with advanced filtering and sorting
    /// </summary>
    /// <param name="name">Pokemon name (supports partial matching)</param>
    /// <param name="type">Pokemon type (supports partial matching)</param>
    /// <param name="minLevel">Minimum level filter</param>
    /// <param name="maxLevel">Maximum level filter</param>
    /// <param name="owner">Owner name (supports partial matching)</param>
    /// <param name="hasOwner">Filter by ownership status</param>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 10, max: 100)</param>
    /// <param name="sortBy">Sort field: name, level, caughtAt, type (default: name)</param>
    /// <param name="sortOrder">Sort order: asc, desc (default: asc)</param>
    /// <returns>Paginated list of Pokemon matching the search criteria</returns>
    [HttpGet("pokemon/paginated")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<PokemonResponseDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<PokemonResponseDto>>>> GetPaginatedPokemon(
        [FromQuery] string? name = null,
        [FromQuery] string? type = null,
        [FromQuery] int? minLevel = null,
        [FromQuery] int? maxLevel = null,
        [FromQuery] string? owner = null,
        [FromQuery] bool? hasOwner = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string sortBy = "name",
        [FromQuery] string sortOrder = "asc")
    {
        try
        {
            // Create search criteria object
            var searchCriteria = new PaginatedPokemonSearchDto
            {
                Name = name,
                Type = type,
                MinLevel = minLevel,
                MaxLevel = maxLevel,
                Owner = owner,
                HasOwner = hasOwner,
                PageNumber = pageNumber,
                PageSize = pageSize,
                SortBy = sortBy,
                SortOrder = sortOrder
            };

            // Validate search criteria
            if (!searchCriteria.IsValid())
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Invalid search criteria. Check level ranges, sort fields (name, level, caughtAt, type), and sort order (asc, desc).", 
                    400));
            }

            var result = await _pokemonService.GetPaginatedPokemonAsync(searchCriteria);
            
            var message = result.TotalItems > 0 
                ? $"Found {result.TotalItems} Pokemon (Page {result.PageNumber} of {result.TotalPages})"
                : "No Pokemon found matching search criteria";
            
            return Ok(ApiResponse<PaginatedResponse<PokemonResponseDto>>.SuccessResponse(result, message));
        }
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error", 500));
        }
    }
}
