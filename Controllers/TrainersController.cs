using Microsoft.AspNetCore.Mvc;
using PokeREST.DTOs;
using PokeREST.Services;

namespace PokeREST.Controllers;

/// <summary>
/// Controller for managing trainer operations including CRUD operations, searching, and pagination.
/// Provides comprehensive endpoints for trainer management with advanced filtering and sorting capabilities.
/// </summary>
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

    /// <summary>
    /// Gets paginated trainers with advanced filtering and sorting
    /// </summary>
    /// <param name="name">Trainer name (supports partial matching)</param>
    /// <param name="region">Trainer region (supports partial matching)</param>
    /// <param name="minAge">Minimum age filter</param>
    /// <param name="maxAge">Maximum age filter</param>
    /// <param name="minWins">Minimum wins filter</param>
    /// <param name="maxWins">Maximum wins filter</param>
    /// <param name="minPokemonCount">Minimum Pokemon count filter</param>
    /// <param name="maxPokemonCount">Maximum Pokemon count filter</param>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 10, max: 100)</param>
    /// <param name="sortBy">Sort field: name, region, age, wins, losses, pokemonCount, createdAt (default: name)</param>
    /// <param name="sortOrder">Sort order: asc, desc (default: asc)</param>
    /// <returns>Paginated list of trainers matching the search criteria</returns>
    [HttpGet("paginated")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<TrainerResponseDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<TrainerResponseDto>>>> GetPaginatedTrainers(
        [FromQuery] string? name = null,
        [FromQuery] string? region = null,
        [FromQuery] int? minAge = null,
        [FromQuery] int? maxAge = null,
        [FromQuery] int? minWins = null,
        [FromQuery] int? maxWins = null,
        [FromQuery] int? minPokemonCount = null,
        [FromQuery] int? maxPokemonCount = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string sortBy = "name",
        [FromQuery] string sortOrder = "asc")
    {
        try
        {
            // Create search criteria object
            var searchCriteria = new PaginatedTrainerSearchDto
            {
                Name = name,
                Region = region,
                MinAge = minAge,
                MaxAge = maxAge,
                MinWins = minWins,
                MaxWins = maxWins,
                MinPokemonCount = minPokemonCount,
                MaxPokemonCount = maxPokemonCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                SortBy = sortBy,
                SortOrder = sortOrder
            };

            // Validate search criteria
            if (!searchCriteria.IsValid())
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Invalid search criteria. Check ranges and sort fields (name, region, age, wins, losses, pokemonCount, createdAt).",
                    400));
            }

            var result = await _trainerService.GetPaginatedTrainersAsync(searchCriteria);
            
            var message = result.TotalItems > 0 
                ? $"Found {result.TotalItems} trainers (Page {result.PageNumber} of {result.TotalPages})"
                : "No trainers found matching search criteria";
            
            return Ok(ApiResponse<PaginatedResponse<TrainerResponseDto>>.SuccessResponse(result, message));
        }
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error", 500));
        }
    }
}
