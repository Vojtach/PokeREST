using Microsoft.EntityFrameworkCore;
using PokeREST.DTOs;
using PokeREST.Models;

namespace PokeREST.Services;

public class PokemonService : IPokemonService
{
    private readonly PokemonContext _context;

    public PokemonService(PokemonContext context)
    {
        _context = context;
    }

    public async Task<PokemonResponseDto?> AddExistingPokemonToTrainerAsync(int trainerId, int pokemonId)
    {
        // Verify trainer exists
        var trainer = await _context.Trainers.FindAsync(trainerId);
        if (trainer == null)
            return null;

        // Find the Pokemon and verify it exists
        var pokemon = await _context.Pokemons
            .Include(p => p.Type)
            .Include(p => p.PokemonMoves)
            .ThenInclude(pm => pm.Move)
            .ThenInclude(m => m.Type)
            .FirstOrDefaultAsync(p => p.Id == pokemonId);

        if (pokemon == null)
            return null;

        // Update the Pokemon's owner
        pokemon.OwnerId = trainerId;
        // Assign the tracked trainer entity to avoid an extra DB roundtrip to load Owner
        pokemon.Owner = trainer;
        pokemon.CaughtAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

        await _context.SaveChangesAsync();

        return MapToPokemonResponseDto(pokemon);
    }

    public async Task<List<PokemonResponseDto>> GetAllPokemonAsync()
    {
        var pokemon = await _context.Pokemons
            .AsNoTracking()
            .Include(p => p.Type)
            .Include(p => p.Owner)
            .Include(p => p.PokemonMoves)
            .ThenInclude(pm => pm.Move)
            .ThenInclude(m => m.Type)
            .ToListAsync();

        return pokemon.Select(MapToPokemonResponseDto).ToList();
    }

    private static PokemonResponseDto MapToPokemonResponseDto(Pokemon pokemon)
    {
        return new PokemonResponseDto
        {
            Id = pokemon.Id,
            Name = pokemon.Name,
            Level = pokemon.Level,
            Type = pokemon.Type.Name,
            Owner = pokemon.Owner?.Name,
            Health = pokemon.Health,
            CaughtAt = pokemon.CaughtAt,
            Moves = pokemon.PokemonMoves.Select(pm => new MoveResponseDto
            {
                Id = pm.Move.Id,
                Name = pm.Move.Name,
                Power = pm.Move.Power,
                Type = pm.Move.Type.Name
            }).ToList()
        };
    }
}
