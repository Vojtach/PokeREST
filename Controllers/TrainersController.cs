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

    /// <summary>
    /// Initializes a new instance of the <see cref="TrainersController"/> class.
    /// </summary>
    /// <param name="trainerService">Service handling trainer persistence, retrieval, and search.</param>
    public TrainersController(ITrainerService trainerService)
    {
        _trainerService = trainerService;
    }

    /// <summary>
    /// Creates a new trainer.
    /// </summary>
    /// <remarks>
    /// <c>POST /api/trainers</c>
    ///
    /// On success the response includes a <c>Location</c> header pointing to <c>GET /api/trainers/{id}</c> for the new trainer.
    ///
    /// Sample request:
    ///
    ///     POST /api/trainers
    ///     {
    ///         "name": "Ash Ketchum",
    ///         "region": "Kanto",
    ///         "age": 10
    ///     }
    /// </remarks>
    /// <param name="createTrainerDto">
    /// Request body with the trainer's <c>name</c> (required, 1–100 characters), optional <c>region</c> (max 50 characters),
    /// and optional <c>age</c> (1–99).
    /// </param>
    /// <returns>An <see cref="ApiResponse{T}"/> wrapping the created <see cref="TrainerResponseDto"/>.</returns>
    /// <response code="201">Trainer was created.</response>
    /// <response code="400">Request body is invalid or the trainer could not be created.</response>
    /// <response code="500">Unexpected server error.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<TrainerResponseDto>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<ActionResult<ApiResponse<TrainerResponseDto>>> CreateTrainer([FromBody] CreateTrainerDto createTrainerDto)
    {
        // Body validation (CreateTrainerDto data annotations):
        //   - Name: required, 1..100 characters
        //   - Region: optional, max 50 characters
        //   - Age: optional, 1..99
        // Because of [ApiController] (SuppressModelStateInvalidFilter = false in Program.cs), an invalid
        // body is normally rejected with a 400 ProblemDetails before this action runs. This check is a
        // defensive fallback that returns our ApiResponse envelope when the action is invoked directly.
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("Invalid input data", 400));
        }

        try
        {
            // Defensive: the current service never returns null on success, but a null result is
            // still mapped to 400 so the endpoint never responds 201 without a created resource.
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
    /// Gets all trainers.
    /// </summary>
    /// <remarks>
    /// <c>GET /api/trainers</c>
    ///
    /// Returns trainer summaries, including how many Pokemon each one owns, but not the Pokemon themselves.
    /// Use <c>GET /api/trainers/{id}</c> for a trainer's full Pokemon list, or <c>GET /api/trainers/paginated</c> to filter and sort.
    /// </remarks>
    /// <returns>An <see cref="ApiResponse{T}"/> wrapping a list of <see cref="TrainerResponseDto"/> (empty if no trainers exist).</returns>
    /// <response code="200">Trainers were retrieved.</response>
    /// <response code="500">Unexpected server error.</response>
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
    /// Gets a single trainer together with the Pokemon they own.
    /// </summary>
    /// <remarks>
    /// <c>GET /api/trainers/{id}</c>
    ///
    /// Sample request:
    ///
    ///     GET /api/trainers/1
    /// </remarks>
    /// <param name="id">ID of the trainer to retrieve (route parameter, positive integer).</param>
    /// <returns>
    /// An <see cref="ApiResponse{T}"/> wrapping a <see cref="TrainerWithPokemonDto"/>: the trainer's details plus their Pokemon and those Pokemon's moves.
    /// </returns>
    /// <response code="200">Trainer was found.</response>
    /// <response code="404">Trainer with the given ID does not exist.</response>
    /// <response code="500">Unexpected server error.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<TrainerWithPokemonDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<ActionResult<ApiResponse<TrainerWithPokemonDto>>> GetTrainerById(int id)
    {
        try
        {
            // Route validation happens in the service: ids <= 0 throw ArgumentException, which is caught
            // below and surfaces as 500 (not 400). Positive but unknown ids return null -> 404.
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
    /// Partially updates an existing trainer.
    /// </summary>
    /// <remarks>
    /// <c>PATCH /api/trainers/{id}</c>
    ///
    /// Only fields present in the request body are changed; omitted or <c>null</c> fields keep their current values.
    ///
    /// Sample request:
    ///
    ///     PATCH /api/trainers/1
    ///     {
    ///         "wins": 12,
    ///         "losses": 3
    ///     }
    /// </remarks>
    /// <param name="id">ID of the trainer to update (route parameter, positive integer).</param>
    /// <param name="updateTrainerDto">
    /// Request body with any of: <c>name</c> (1–100 characters), <c>region</c> (max 50 characters), <c>age</c> (1–99),
    /// <c>wins</c> and <c>losses</c> (non-negative integers).
    /// </param>
    /// <returns>An <see cref="ApiResponse{T}"/> wrapping the updated <see cref="TrainerResponseDto"/>.</returns>
    /// <response code="200">Trainer was updated.</response>
    /// <response code="400">Request body is invalid.</response>
    /// <response code="404">Trainer with the given ID does not exist.</response>
    /// <response code="500">Unexpected server error.</response>
    [HttpPatch("{id}")]
    [ProducesResponseType(typeof(ApiResponse<TrainerResponseDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<ActionResult<ApiResponse<TrainerResponseDto>>> UpdateTrainer(int id, [FromBody] UpdateTrainerDto updateTrainerDto)
    {
        // Body validation (UpdateTrainerDto data annotations). Every field is optional (PATCH semantics),
        // but any field that is supplied must satisfy its rule:
        //   - Name: 1..100 characters
        //   - Region: max 50 characters
        //   - Age: 1..99
        //   - Wins / Losses: non-negative
        // As with CreateTrainer, [ApiController] normally rejects invalid bodies before this point; this
        // check is a defensive fallback that returns our ApiResponse envelope.
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("Invalid input data", 400));
        }

        try
        {
            // Edge cases handled by the service:
            //   - ids <= 0 throw ArgumentException, which is caught below and surfaces as 500 (not 400).
            //   - Omitted (null) fields are left unchanged. An empty Name is ignored, while an empty
            //     Region string is applied and clears the region.
            //   - Positive but unknown ids return null -> 404.
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
    /// Deletes a trainer.
    /// </summary>
    /// <remarks>
    /// <c>DELETE /api/trainers/{id}</c>
    ///
    /// The trainer's Pokemon are not deleted; they are released and become unowned (wild) Pokemon,
    /// so another trainer can catch them later.
    /// </remarks>
    /// <param name="id">ID of the trainer to delete (route parameter, positive integer).</param>
    /// <returns>An <see cref="ApiResponse{T}"/> with a confirmation message and <c>null</c> data.</returns>
    /// <response code="200">Trainer was deleted.</response>
    /// <response code="404">Trainer with the given ID does not exist.</response>
    /// <response code="500">Unexpected server error.</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteTrainer(int id)
    {
        try
        {
            // Route validation happens in the service: ids <= 0 throw ArgumentException, which is caught
            // below and surfaces as 500 (not 400). Positive but unknown ids return false -> 404.
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
    /// Gets a single page of trainers with optional filtering and sorting.
    /// </summary>
    /// <remarks>
    /// <c>GET /api/trainers/paginated</c>
    ///
    /// Text filters use case-insensitive substring matching. All range filters are inclusive, and each minimum
    /// must not exceed its maximum. Filters are applied first, then sorting, then pagination.
    /// Sort field and sort order values are case-insensitive.
    ///
    /// Sample request:
    ///
    ///     GET /api/trainers/paginated?region=kanto&amp;minWins=5&amp;sortBy=wins&amp;sortOrder=desc&amp;pageSize=20
    /// </remarks>
    /// <param name="name">Optional substring that the trainer name must contain.</param>
    /// <param name="region">Optional substring that the trainer region must contain. Trainers without a region are excluded when set.</param>
    /// <param name="minAge">Optional inclusive minimum age (1–99).</param>
    /// <param name="maxAge">Optional inclusive maximum age (1–99).</param>
    /// <param name="minWins">Optional inclusive minimum number of wins (non-negative).</param>
    /// <param name="maxWins">Optional inclusive maximum number of wins (non-negative).</param>
    /// <param name="minPokemonCount">Optional inclusive minimum number of owned Pokemon (non-negative).</param>
    /// <param name="maxPokemonCount">Optional inclusive maximum number of owned Pokemon (non-negative).</param>
    /// <param name="pageNumber">1-based page number to return (default: 1).</param>
    /// <param name="pageSize">Number of items per page, 1–100 (default: 10).</param>
    /// <param name="sortBy">
    /// Sort field: <c>name</c>, <c>region</c>, <c>age</c>, <c>wins</c>, <c>losses</c>, <c>pokemonCount</c>, or <c>createdAt</c> (default: <c>name</c>).
    /// </param>
    /// <param name="sortOrder">Sort direction: <c>asc</c> or <c>desc</c> (default: <c>asc</c>).</param>
    /// <returns>
    /// An <see cref="ApiResponse{T}"/> wrapping a <see cref="PaginatedResponse{T}"/> of <see cref="TrainerResponseDto"/>,
    /// which holds the page items and pagination metadata (total items, total pages, previous/next page flags).
    /// </returns>
    /// <response code="200">The requested page was retrieved (it may contain no items).</response>
    /// <response code="400">Search criteria are invalid: a min/max range is inverted, or the sort field or sort order is unknown.</response>
    /// <response code="500">Unexpected server error.</response>
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

            // Validate search criteria. PaginatedTrainerSearchDto.IsValid() enforces:
            //   - MinAge <= MaxAge, MinWins <= MaxWins, MinPokemonCount <= MaxPokemonCount
            //     (each pair only when both bounds are supplied; a single bound is always accepted)
            //   - sortBy (case-insensitive) is one of: name, region, age, wins, losses, pokemonCount, createdAt
            //   - sortOrder (case-insensitive) is "asc" or "desc"
            // Whitelisting sortBy/sortOrder prevents arbitrary values from reaching the sorting logic.
            // Edge case: the DTO's [Range] attributes (ages 1..99, non-negative counts, pageNumber >= 1,
            // pageSize 1..100) are NOT evaluated, because the DTO is built manually from query parameters.
            // pageSize > 100 is not capped, and pageNumber/pageSize values <= 0 are passed to the service.
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
