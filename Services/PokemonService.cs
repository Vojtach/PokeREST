using Microsoft.EntityFrameworkCore;
using PokeREST.DTOs;
using PokeREST.Models;
using PokeREST.Helpers;

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

    public async Task<List<PokemonResponseDto>> SearchPokemonAsync(PokemonSearchDto searchCriteria)
    {
        var query = _context.Pokemons
            .AsNoTracking()
            .Include(p => p.Type)
            .Include(p => p.Owner)
            .Include(p => p.PokemonMoves)
            .ThenInclude(pm => pm.Move)
            .ThenInclude(m => m.Type)
            .AsQueryable();

        // Apply exact filters first for performance
        if (searchCriteria.MinLevel.HasValue)
        {
            query = query.Where(p => p.Level >= searchCriteria.MinLevel.Value);
        }

        if (searchCriteria.MaxLevel.HasValue)
        {
            query = query.Where(p => p.Level <= searchCriteria.MaxLevel.Value);
        }

        if (searchCriteria.HasOwner.HasValue)
        {
            if (searchCriteria.HasOwner.Value)
            {
                query = query.Where(p => p.OwnerId != null);
            }
            else
            {
                query = query.Where(p => p.OwnerId == null);
            }
        }

        // Get all potential matches
        var allPokemon = await query.ToListAsync();
        var results = new List<(PokemonResponseDto Pokemon, double Score)>();

        foreach (var pokemon in allPokemon)
        {
            var score = 0.0;
            var matchFound = false;

            // Name matching (primary criteria)
            if (!string.IsNullOrWhiteSpace(searchCriteria.Name))
            {
                if (FuzzyMatchingHelper.IsFuzzyMatch(searchCriteria.Name, pokemon.Name))
                {
                    score += FuzzyMatchingHelper.GetMatchScore(searchCriteria.Name, pokemon.Name);
                    matchFound = true;
                }
                else if (string.IsNullOrWhiteSpace(searchCriteria.Type) && 
                         string.IsNullOrWhiteSpace(searchCriteria.Owner))
                {
                    // If only name is specified and it doesn't match, skip this pokemon
                    continue;
                }
            }
            else
            {
                matchFound = true; // No name filter means all pass
            }

            // Type matching
            if (!string.IsNullOrWhiteSpace(searchCriteria.Type))
            {
                if (FuzzyMatchingHelper.IsFuzzyMatch(searchCriteria.Type, pokemon.Type.Name))
                {
                    score += FuzzyMatchingHelper.GetMatchScore(searchCriteria.Type, pokemon.Type.Name) * 0.8;
                    matchFound = true;
                }
                else if (string.IsNullOrWhiteSpace(searchCriteria.Name) && 
                         string.IsNullOrWhiteSpace(searchCriteria.Owner))
                {
                    // If only type is specified and it doesn't match, skip this pokemon
                    continue;
                }
            }

            // Owner matching
            if (!string.IsNullOrWhiteSpace(searchCriteria.Owner))
            {
                if (pokemon.Owner != null && 
                    FuzzyMatchingHelper.IsFuzzyMatch(searchCriteria.Owner, pokemon.Owner.Name))
                {
                    score += FuzzyMatchingHelper.GetMatchScore(searchCriteria.Owner, pokemon.Owner.Name) * 0.6;
                    matchFound = true;
                }
                else if (string.IsNullOrWhiteSpace(searchCriteria.Name) && 
                         string.IsNullOrWhiteSpace(searchCriteria.Type))
                {
                    // If only owner is specified and it doesn't match, skip this pokemon
                    continue;
                }
            }

            if (matchFound)
            {
                results.Add((MapToPokemonResponseDto(pokemon), score));
            }
        }

        // Sort by score (descending) and then by name for consistency
        return results
            .OrderByDescending(r => r.Score)
            .ThenBy(r => r.Pokemon.Name)
            .Select(r => r.Pokemon)
            .ToList();
    }

    public async Task<PaginatedResponse<PokemonResponseDto>> GetPaginatedPokemonAsync(PaginatedPokemonSearchDto searchCriteria)
    {
        var query = _context.Pokemons
            .AsNoTracking()
            .Include(p => p.Type)
            .Include(p => p.Owner)
            .Include(p => p.PokemonMoves)
            .ThenInclude(pm => pm.Move)
            .ThenInclude(m => m.Type)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(searchCriteria.Name))
        {
            var searchName = searchCriteria.Name.ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(searchName));
        }

        if (!string.IsNullOrWhiteSpace(searchCriteria.Type))
        {
            var searchType = searchCriteria.Type.ToLower();
            query = query.Where(p => p.Type.Name.ToLower().Contains(searchType));
        }

        if (searchCriteria.MinLevel.HasValue)
        {
            query = query.Where(p => p.Level >= searchCriteria.MinLevel.Value);
        }

        if (searchCriteria.MaxLevel.HasValue)
        {
            query = query.Where(p => p.Level <= searchCriteria.MaxLevel.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchCriteria.Owner))
        {
            var searchOwner = searchCriteria.Owner.ToLower();
            query = query.Where(p => p.Owner != null && p.Owner.Name.ToLower().Contains(searchOwner));
        }

        if (searchCriteria.HasOwner.HasValue)
        {
            if (searchCriteria.HasOwner.Value)
            {
                query = query.Where(p => p.OwnerId != null);
            }
            else
            {
                query = query.Where(p => p.OwnerId == null);
            }
        }

        // Get total count before pagination
        var totalItems = await query.CountAsync();

        // Apply sorting
        var sortBy = searchCriteria.SortBy?.ToLower() ?? "name";
        var sortOrder = searchCriteria.SortOrder?.ToLower() ?? "asc";

        query = sortBy switch
        {
            "name" => sortOrder == "desc" ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
            "level" => sortOrder == "desc" ? query.OrderByDescending(p => p.Level) : query.OrderBy(p => p.Level),
            "caughtat" => sortOrder == "desc" ? query.OrderByDescending(p => p.CaughtAt ?? DateTime.MinValue) : query.OrderBy(p => p.CaughtAt ?? DateTime.MinValue),
            "type" => sortOrder == "desc" ? query.OrderByDescending(p => p.Type.Name) : query.OrderBy(p => p.Type.Name),
            _ => query.OrderBy(p => p.Name)
        };

        // Apply pagination
        var skipCount = (searchCriteria.PageNumber - 1) * searchCriteria.PageSize;
        var paginatedQuery = query.Skip(skipCount).Take(searchCriteria.PageSize);

        var pokemon = await paginatedQuery.ToListAsync();
        var pokemonDtos = pokemon.Select(MapToPokemonResponseDto).ToList();

        var totalPages = (int)Math.Ceiling((double)totalItems / searchCriteria.PageSize);

        return new PaginatedResponse<PokemonResponseDto>
        {
            Data = pokemonDtos,
            TotalItems = totalItems,
            PageNumber = searchCriteria.PageNumber,
            PageSize = searchCriteria.PageSize,
            TotalPages = totalPages,
            HasPreviousPage = searchCriteria.PageNumber > 1,
            HasNextPage = searchCriteria.PageNumber < totalPages
        };
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
