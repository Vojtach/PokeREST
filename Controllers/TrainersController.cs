using Microsoft.AspNetCore.Mvc;
using PokeREST.DTOs;
using PokeREST.Services;

namespace PokeREST.Controllers;

[ApiController]
[Route("api/trainers")]
[Produces("application/json")]
public class TrainersController : ControllerBase
{
    private readonly ITrainerService _trainerService;

    public TrainersController(ITrainerService trainerService)
    {
        _trainerService = trainerService;
    }

    /// <summary>
    /// Creates a new trainer
    /// </summary>
    /// <param name="createTrainerDto">Trainer creation data</param>
    /// <returns>Created trainer information</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<TrainerResponseDto>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<ActionResult<ApiResponse<TrainerResponseDto>>> CreateTrainer([FromBody] CreateTrainerDto createTrainerDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("Invalid input data", 400));
        }

        try
        {
            var trainer = await _trainerService.CreateTrainerAsync(createTrainerDto);
            if (trainer == null)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("Failed to create trainer", 400));
            }

            return CreatedAtAction(
                nameof(GetTrainerById),
                new { id = trainer.Id },
                ApiResponse<TrainerResponseDto>.SuccessResponse(trainer, "Trainer created successfully", 201)
            );
        }
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error", 500));
        }
    }

    /// <summary>
    /// Gets all trainers
    /// </summary>
    /// <returns>List of all trainers</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<TrainerResponseDto>>), 200)]
    public async Task<ActionResult<ApiResponse<List<TrainerResponseDto>>>> GetAllTrainers()
    {
        try
        {
            var trainers = await _trainerService.GetAllTrainersAsync();
            return Ok(ApiResponse<List<TrainerResponseDto>>.SuccessResponse(trainers, "Trainers retrieved successfully"));
        }
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error", 500));
        }
    }

    /// <summary>
    /// Gets a specific trainer with their Pokemon
    /// </summary>
    /// <param name="id">Trainer ID</param>
    /// <returns>Trainer information with Pokemon</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<TrainerWithPokemonDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<ActionResult<ApiResponse<TrainerWithPokemonDto>>> GetTrainerById(int id)
    {
        try
        {
            var trainer = await _trainerService.GetTrainerByIdAsync(id);
            if (trainer == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Trainer not found", 404));
            }

            return Ok(ApiResponse<TrainerWithPokemonDto>.SuccessResponse(trainer, "Trainer found successfully"));
        }
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error", 500));
        }
    }

    /// <summary>
    /// Updates trainer information
    /// </summary>
    /// <param name="id">Trainer ID</param>
    /// <param name="updateTrainerDto">Updated trainer data</param>
    /// <returns>Updated trainer information</returns>
    [HttpPatch("{id}")]
    [ProducesResponseType(typeof(ApiResponse<TrainerResponseDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<ActionResult<ApiResponse<TrainerResponseDto>>> UpdateTrainer(int id, [FromBody] UpdateTrainerDto updateTrainerDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("Invalid input data", 400));
        }

        try
        {
            var trainer = await _trainerService.UpdateTrainerAsync(id, updateTrainerDto);
            if (trainer == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Trainer not found", 404));
            }

            return Ok(ApiResponse<TrainerResponseDto>.SuccessResponse(trainer, "Trainer updated successfully"));
        }
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error", 500));
        }
    }

    /// <summary>
    /// Updates trainer information (PUT method)
    /// </summary>
    /// <param name="id">Trainer ID</param>
    /// <param name="updateTrainerDto">Updated trainer data</param>
    /// <returns>Updated trainer information</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<TrainerResponseDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<ActionResult<ApiResponse<TrainerResponseDto>>> PutTrainer(int id, [FromBody] UpdateTrainerDto updateTrainerDto)
    {
        return await UpdateTrainer(id, updateTrainerDto);
    }

    /// <summary>
    /// Deletes a trainer
    /// </summary>
    /// <param name="id">Trainer ID</param>
    /// <returns>Deletion confirmation</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteTrainer(int id)
    {
        try
        {
            var deleted = await _trainerService.DeleteTrainerAsync(id);
            if (!deleted)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Trainer not found", 404));
            }

            return Ok(ApiResponse<object>.SuccessResponse(null, "Trainer deleted successfully"));
        }
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error", 500));
        }
    }
}
