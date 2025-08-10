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
    /// Gets all Pokemon
    /// </summary>
    /// <returns>List of all Pokemon</returns>
    [HttpGet("pokemon")]
    [ProducesResponseType(typeof(ApiResponse<List<PokemonResponseDto>>), 200)]
    public async Task<ActionResult<ApiResponse<List<PokemonResponseDto>>>> GetAllPokemon()
    {
        try
        {
            var pokemon = await _pokemonService.GetAllPokemonAsync();
            return Ok(ApiResponse<List<PokemonResponseDto>>.SuccessResponse(pokemon, "Pokemon found successfully"));
        }
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error", 500));
        }
    }
}
