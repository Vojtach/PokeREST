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
    /// <response code="400">
    /// The Pokemon does not exist or is already owned (<see cref="ApiResponse{T}"/> envelope).
    /// Request body failed validation. Because of <c>[ApiController]</c>, this is returned as a standard ProblemDetails payload, not the <see cref="ApiResponse{T}"/> envelope.
    /// The action's own <c>ModelState</c> check returns the <see cref="ApiResponse{T}"/> envelope instead, but only when the
    /// automatic validation is bypassed (e.g. the action is invoked directly).
    /// </response>
    /// <response code="404">Trainer with the given ID does not exist.</response>
    /// <response code="500">Unexpected server error. Also returned for a non-positive ID, because the service rejects it with an exception instead of a 400.</response>
    [HttpPost("trainers/{trainerId}/pokemon")]
    [ProducesResponseType(typeof(ApiResponse<PokemonResponseDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<ActionResult<ApiResponse<PokemonResponseDto>>> AddPokemonToTrainer(int trainerId, [FromBody] AddExistingPokemonDto addExistingPokemonDto)
    {
        // Body validation: AddExistingPokemonDto requires PokemonId in the range 1..int.MaxValue.
        // Because of [ApiController] (SuppressModelStateInvalidFilter = false in Program.cs), an invalid
        // body is normally rejected with a 400 ProblemDetails before this action runs. This check is a
        // defensive fallback that returns our ApiResponse envelope when the action is invoked directly.
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("Invalid input data", 400));
        }

        try
        {
            // Route validation happens in the service: TrainerExistsAsync throws ArgumentException for
            // trainerId <= 0, which is caught below and surfaces as 500 (not 400). Positive but unknown
            // IDs return false -> 404.
            var trainerExists = await _trainerService.TrainerExistsAsync(trainerId);
            if (!trainerExists)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Trainer not found", 404));
            }

            // The service returns null when the Pokemon does not exist OR is already owned by a trainer.
            // Both cases are treated as a client error (400) rather than 404, since the trainer was found
            // and the request itself is what cannot be fulfilled.
            var pokemon = await _pokemonService.AddExistingPokemonToTrainerAsync(trainerId, addExistingPokemonDto.PokemonId);
            if (pokemon == null)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("Failed to add Pokemon to trainer. Please check if the Pokemon ID is valid.", 400));
            }

            return Ok(ApiResponse<PokemonResponseDto>.SuccessResponse(pokemon, "Pokemon added to trainer successfully"));
        }
        catch (Exception)
        {
            // Any unexpected failure (e.g. database errors) is hidden behind a generic 500 so internal
            // details are not leaked to the client.
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
    /// <paramref name="owner"/> are fuzzy-matched (partial matches and minor typos are tolerated). When several text
    /// filters are supplied, a Pokemon is included if <em>any</em> of them matches (OR, not AND). Results are ordered
    /// by match relevance, then by name. Known quirk: supplying <paramref name="type"/> and <paramref name="owner"/>
    /// without <paramref name="name"/> filters nothing out; every Pokemon is returned, ranked by relevance.
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
    /// <param name="minLevel">Optional inclusive minimum level. Must not exceed <paramref name="maxLevel"/>. Values outside 1–100 are not rejected.</param>
    /// <param name="maxLevel">Optional inclusive maximum level. Values outside 1–100 are not rejected.</param>
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
            // Check if any search criteria are provided. Whitespace-only strings count as "not provided",
            // so "?name=%20" behaves like no filter and returns all Pokemon.
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

                // Validate search criteria. PokemonSearchDto.IsValid() only enforces MinLevel <= MaxLevel,
                // and only when both are supplied; a single bound is always accepted.
                // Note: the DTO's [Range(1, 100)] attributes are NOT evaluated here, because the DTO is built
                // manually from query parameters rather than model-bound. Out-of-range levels (e.g. 0 or 500)
                // are therefore accepted and simply yield fewer or no results.
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
            // Unexpected failures are reported as a generic 500 without exposing internal details.
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
    /// <param name="minLevel">Optional inclusive minimum level. Must not exceed <paramref name="maxLevel"/>. Values outside 1–100 are not rejected.</param>
    /// <param name="maxLevel">Optional inclusive maximum level. Values outside 1–100 are not rejected.</param>
    /// <param name="owner">Optional substring that the owner (trainer) name must contain.</param>
    /// <param name="hasOwner">Optional ownership filter: <c>true</c> for owned Pokemon only, <c>false</c> for wild (unowned) Pokemon only.</param>
    /// <param name="pageNumber">1-based page number to return (default: 1). Values below 1 are not rejected up front and result in a 500.</param>
    /// <param name="pageSize">Number of items per page (default: 10). The intended range is 1–100, but it is not enforced.</param>
    /// <param name="sortBy">
    /// Sort field: <c>name</c>, <c>level</c>, or <c>type</c> (default: <c>name</c>). <c>caughtAt</c> is advertised but
    /// is currently rejected with 400 because of a case mismatch in the whitelist check.
    /// </param>
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

            // Validate search criteria. PaginatedPokemonSearchDto.IsValid() enforces:
            //   - MinLevel <= MaxLevel when both are supplied
            //   - sortBy (case-insensitive) is one of the whitelisted fields, or empty
            //   - sortOrder (case-insensitive) is "asc" or "desc", or empty
            // Whitelisting sortBy/sortOrder prevents arbitrary values from reaching the sorting logic.
            // Edge cases:
            //   - sortBy is lower-cased before comparison against { "name", "level", "caughtAt", "type" },
            //     so "caughtAt" can never match and sorting by caught date is currently rejected with 400.
            //   - The DTO's [Range] attributes (levels 1..100, pageNumber >= 1, pageSize 1..100) are NOT
            //     evaluated, because the DTO is built manually from query parameters. pageSize > 100 is not
            //     capped, and pageNumber/pageSize values <= 0 are passed straight to the service.
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
            // Unexpected failures (including ones caused by invalid paging values that slipped past
            // IsValid()) are reported as a generic 500 without exposing internal details.
            return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error", 500));
        }
    }
}
