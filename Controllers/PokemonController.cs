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

    /// <summary>
    /// Initializes a new instance of the <see cref="PokemonController"/> class.
    /// </summary>
    /// <param name="pokemonService">Service handling Pokemon retrieval, search, and trainer assignment.</param>
    /// <param name="trainerService">Service used to verify that a target trainer exists.</param>
    public PokemonController(IPokemonService pokemonService, ITrainerService trainerService)
    {
        _pokemonService = pokemonService;
        _trainerService = trainerService;
    }

    /// <summary>
    /// Assigns an existing, currently unowned Pokemon to a trainer ("catches" it).
    /// </summary>
    /// <remarks>
    /// <c>POST /api/trainers/{trainerId}/pokemon</c>
    ///
    /// Sets the Pokemon's owner to the given trainer and stamps its <c>caughtAt</c> time with the current UTC time.
    /// The assignment fails if the Pokemon does not exist or already belongs to a trainer.
    ///
    /// Sample request:
    ///
    ///     POST /api/trainers/1/pokemon
    ///     {
    ///         "pokemonId": 25
    ///     }
    /// </remarks>
    /// <param name="trainerId">ID of the trainer who receives the Pokemon (route parameter).</param>
    /// <param name="addExistingPokemonDto">Request body containing the <c>pokemonId</c> (positive integer) of the Pokemon to assign.</param>
    /// <returns>
    /// An <see cref="ApiResponse{T}"/> wrapping the updated <see cref="PokemonResponseDto"/>, including its new owner and moves.
    /// </returns>
    /// <response code="200">Pokemon was assigned to the trainer.</response>
    /// <response code="400">Request body is invalid, or the Pokemon does not exist or is already owned.</response>
    /// <response code="404">Trainer with the given ID does not exist.</response>
    /// <response code="500">Unexpected server error.</response>
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
    /// Gets all Pokemon, or searches Pokemon using fuzzy matching when any filter is supplied.
    /// </summary>
    /// <remarks>
    /// <c>GET /api/pokemon</c>
    ///
    /// With no query parameters, every Pokemon is returned. When at least one parameter is supplied, a search is performed:
    /// level and ownership filters are applied exactly, while <paramref name="name"/>, <paramref name="type"/>, and
    /// <paramref name="owner"/> are fuzzy-matched (partial matches and minor typos are tolerated). Search results are
    /// ordered by match relevance, then by name.
    ///
    /// Sample requests:
    ///
    ///     GET /api/pokemon
    ///     GET /api/pokemon?name=pikchu
    ///     GET /api/pokemon?type=fire&amp;minLevel=10&amp;maxLevel=50
    ///     GET /api/pokemon?hasOwner=false
    /// </remarks>
    /// <param name="name">Optional Pokemon name to fuzzy-match (partial matches and typos are tolerated).</param>
    /// <param name="type">Optional Pokemon type name to fuzzy-match (e.g. <c>fire</c>, <c>water</c>).</param>
    /// <param name="minLevel">Optional inclusive minimum level (1–100). Must not exceed <paramref name="maxLevel"/>.</param>
    /// <param name="maxLevel">Optional inclusive maximum level (1–100).</param>
    /// <param name="owner">Optional owner (trainer) name to fuzzy-match. Only Pokemon that have an owner can match.</param>
    /// <param name="hasOwner">Optional ownership filter: <c>true</c> for owned Pokemon only, <c>false</c> for wild (unowned) Pokemon only.</param>
    /// <returns>
    /// An <see cref="ApiResponse{T}"/> wrapping a list of <see cref="PokemonResponseDto"/>. The list is empty when nothing matches.
    /// </returns>
    /// <response code="200">Pokemon were retrieved (the list may be empty).</response>
    /// <response code="400">Search criteria are invalid, e.g. <c>minLevel</c> is greater than <c>maxLevel</c>.</response>
    /// <response code="500">Unexpected server error.</response>
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
    /// Gets a single page of Pokemon with optional filtering and sorting.
    /// </summary>
    /// <remarks>
    /// <c>GET /api/pokemon/paginated</c>
    ///
    /// Unlike <c>GET /api/pokemon</c>, text filters here use case-insensitive substring matching (no typo tolerance),
    /// and the query runs on the database. Filters are applied first, then sorting, then pagination.
    /// Sort field and sort order values are case-insensitive.
    ///
    /// Sample request:
    ///
    ///     GET /api/pokemon/paginated?type=water&amp;pageNumber=2&amp;pageSize=20&amp;sortBy=level&amp;sortOrder=desc
    /// </remarks>
    /// <param name="name">Optional substring that the Pokemon name must contain.</param>
    /// <param name="type">Optional substring that the Pokemon type name must contain.</param>
    /// <param name="minLevel">Optional inclusive minimum level (1–100). Must not exceed <paramref name="maxLevel"/>.</param>
    /// <param name="maxLevel">Optional inclusive maximum level (1–100).</param>
    /// <param name="owner">Optional substring that the owner (trainer) name must contain.</param>
    /// <param name="hasOwner">Optional ownership filter: <c>true</c> for owned Pokemon only, <c>false</c> for wild (unowned) Pokemon only.</param>
    /// <param name="pageNumber">1-based page number to return (default: 1).</param>
    /// <param name="pageSize">Number of items per page, 1–100 (default: 10).</param>
    /// <param name="sortBy">Sort field: <c>name</c>, <c>level</c>, <c>caughtAt</c>, or <c>type</c> (default: <c>name</c>).</param>
    /// <param name="sortOrder">Sort direction: <c>asc</c> or <c>desc</c> (default: <c>asc</c>).</param>
    /// <returns>
    /// An <see cref="ApiResponse{T}"/> wrapping a <see cref="PaginatedResponse{T}"/> of <see cref="PokemonResponseDto"/>,
    /// which holds the page items and pagination metadata (total items, total pages, previous/next page flags).
    /// </returns>
    /// <response code="200">The requested page was retrieved (it may contain no items).</response>
    /// <response code="400">Search criteria are invalid: bad level range, unknown sort field, or unknown sort order.</response>
    /// <response code="500">Unexpected server error.</response>
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
