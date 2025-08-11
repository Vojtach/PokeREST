using PokeREST.DTOs;

namespace PokeREST.Services;

public interface IPokemonService
{
    Task<PokemonResponseDto?> AddExistingPokemonToTrainerAsync(int trainerId, int pokemonId);
    Task<List<PokemonResponseDto>> GetAllPokemonAsync();
    Task<List<PokemonResponseDto>> SearchPokemonAsync(PokemonSearchDto searchCriteria);
}
