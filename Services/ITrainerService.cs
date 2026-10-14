using PokeREST.DTOs;

namespace PokeREST.Services;

/// <summary>
/// Service interface for managing trainer operations including CRUD operations, searching, and pagination.
/// </summary>
public interface ITrainerService
{
    /// <summary>
    /// Creates a new trainer in the system with the provided information.
    /// </summary>
    /// <param name="createTrainerDto">The trainer creation data containing name, region, and age information.</param>
    /// <returns>The created trainer information. The current implementation never returns null; failures throw.</returns>
    Task<TrainerResponseDto?> CreateTrainerAsync(CreateTrainerDto createTrainerDto);
    
    /// <summary>
    /// Retrieves all trainers from the database with their basic information and Pokemon count.
    /// </summary>
    /// <returns>A list of all trainers in the system.</returns>
    Task<List<TrainerResponseDto>> GetAllTrainersAsync();
    
    /// <summary>
    /// Retrieves a specific trainer by their unique identifier, including detailed Pokemon information.
    /// </summary>
    /// <param name="id">The unique identifier of the trainer to retrieve.</param>
    /// <returns>The trainer with their Pokemon collection if found; otherwise, null.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="id"/> is zero or negative.</exception>
    Task<TrainerWithPokemonDto?> GetTrainerByIdAsync(int id);
    
    /// <summary>
    /// Updates an existing trainer's information with the provided data.
    /// Only non-null fields in the update DTO will be modified; an empty Name is also ignored, while an empty Region is applied.
    /// </summary>
    /// <param name="id">The unique identifier of the trainer to update.</param>
    /// <param name="updateTrainerDto">The update data containing fields to be modified.</param>
    /// <returns>The updated trainer information if successful; otherwise, null if trainer not found.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="id"/> is zero or negative.</exception>
    Task<TrainerResponseDto?> UpdateTrainerAsync(int id, UpdateTrainerDto updateTrainerDto);
    
    /// <summary>
    /// Deletes a trainer from the system and releases all their owned Pokemon.
    /// </summary>
    /// <param name="id">The unique identifier of the trainer to delete.</param>
    /// <returns>True if the trainer was successfully deleted; otherwise, false if trainer not found.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="id"/> is zero or negative.</exception>
    Task<bool> DeleteTrainerAsync(int id);
    
    /// <summary>
    /// Checks if a trainer exists in the system by their unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the trainer to check.</param>
    /// <returns>True if the trainer exists; otherwise, false.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="id"/> is zero or negative.</exception>
    Task<bool> TrainerExistsAsync(int id);
    
    /// <summary>
    /// Retrieves trainers with advanced filtering, sorting, and pagination support.
    /// Supports filtering by name, region, age range, wins range, and Pokemon count range, all AND-combined.
    /// Paging values are used as given and are not validated.
    /// </summary>
    /// <param name="searchCriteria">The paginated search parameters including filters, sorting, and pagination settings.</param>
    /// <returns>A paginated response containing trainer data and pagination metadata.</returns>
    Task<PaginatedResponse<TrainerResponseDto>> GetPaginatedTrainersAsync(PaginatedTrainerSearchDto searchCriteria);
}
