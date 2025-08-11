using PokeREST.DTOs;

namespace PokeREST.Services;

public interface ITrainerService
{
    Task<TrainerResponseDto?> CreateTrainerAsync(CreateTrainerDto createTrainerDto);
    Task<List<TrainerResponseDto>> GetAllTrainersAsync();
    Task<TrainerWithPokemonDto?> GetTrainerByIdAsync(int id);
    Task<TrainerResponseDto?> UpdateTrainerAsync(int id, UpdateTrainerDto updateTrainerDto);
    Task<bool> DeleteTrainerAsync(int id);
    Task<bool> TrainerExistsAsync(int id);
    Task<PaginatedResponse<TrainerResponseDto>> GetPaginatedTrainersAsync(PaginatedTrainerSearchDto searchCriteria);
}
