using Microsoft.AspNetCore.Mvc;
using PokeREST.DTOs;
using PokeREST.Services;

namespace PokeREST.Controllers;

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
}
