using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using PokeREST.DTOs;
using PokeREST.Helpers;
using PokeREST.Models;

namespace PokeREST.Services;

public class PokemonService : IPokemonService
{
    private readonly PokemonContext _context;

    // Constants for better maintainability
    private const string DefaultSortField = "name";
    private const string DefaultSortOrder = "asc";
    private const string DescendingSortOrder = "desc";
    private const double TypeMatchWeighting = 0.8;
    private const double OwnerMatchWeighting = 0.6;

    public PokemonService(PokemonContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<PokemonResponseDto?> AddExistingPokemonToTrainerAsync(int trainerId, int pokemonId)
    {
        var trainer = await _context.Trainers.FindAsync(trainerId);
        if (trainer == null)
        {
            return null;
        }

        var pokemon = await _context.Pokemons
            .Include(p => p.Type)
            .Include(p => p.PokemonMoves)
                .ThenInclude(pm => pm.Move)
                    .ThenInclude(m => m.Type)
            .FirstOrDefaultAsync(p => p.Id == pokemonId);

        if (pokemon == null || pokemon.OwnerId.HasValue)
        {
            // Pokemon not found or already has an owner. Pokemon was most likely already "caught".
            return null;
        }

        pokemon.OwnerId = trainerId;
        pokemon.Owner = trainer;
        pokemon.CaughtAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified); // TODO: Change DB column to "timestamp with time zone" for long term reliability

        await _context.SaveChangesAsync();

        return MapToPokemonResponseDto(pokemon);
    }

    public async Task<List<PokemonResponseDto>> GetAllPokemonAsync()
    {
        var pokemon = await BuildBasePokemonQuery().ToListAsync();
        return pokemon.Select(MapToPokemonResponseDto).ToList();
    }

    public async Task<List<PokemonResponseDto>> SearchPokemonAsync(PokemonSearchDto searchCriteria)
    {
        ArgumentNullException.ThrowIfNull(searchCriteria);

        var query = BuildBasePokemonQuery();
        query = ApplyLevelFilters(query, searchCriteria);
        query = ApplyOwnershipFilter(query, searchCriteria.HasOwner);

        var allPokemon = await query.ToListAsync();
        var searchResults = ProcessFuzzySearch(allPokemon, searchCriteria);

        return SortSearchResults(searchResults);
    }

    public async Task<PaginatedResponse<PokemonResponseDto>> GetPaginatedPokemonAsync(PaginatedPokemonSearchDto searchCriteria)
    {
        ArgumentNullException.ThrowIfNull(searchCriteria);

        var query = BuildBasePokemonQuery();
        query = ApplyPaginatedSearchFilters(query, searchCriteria);

        var totalItems = await query.CountAsync();
        
        query = ApplySorting(query, searchCriteria.SortBy, searchCriteria.SortOrder);
        query = ApplyPagination(query, searchCriteria.PageNumber, searchCriteria.PageSize);

        var pokemon = await query.ToListAsync();
        var pokemonDtos = pokemon.Select(MapToPokemonResponseDto).ToList();

        return CreatePaginatedResponse(pokemonDtos, totalItems, searchCriteria);
    }

    /// <summary>
    /// Builds the base query with all necessary includes for Pokemon data.
    /// </summary>
    private IQueryable<Pokemon> BuildBasePokemonQuery()
    {
        return _context.Pokemons
            .AsNoTracking()
            .Include(p => p.Type)
            .Include(p => p.Owner)
            .Include(p => p.PokemonMoves)
                .ThenInclude(pm => pm.Move)
                    .ThenInclude(m => m.Type)
            .AsQueryable();
    }

    /// <summary>
    /// Applies level range filters to the query.
    /// </summary>
    private static IQueryable<Pokemon> ApplyLevelFilters(IQueryable<Pokemon> query, PokemonSearchDto searchCriteria)
    {
        if (searchCriteria.MinLevel.HasValue)
        {
            query = query.Where(p => p.Level >= searchCriteria.MinLevel.Value);
        }

        if (searchCriteria.MaxLevel.HasValue)
        {
            query = query.Where(p => p.Level <= searchCriteria.MaxLevel.Value);
        }

        return query;
    }

    /// <summary>
    /// Applies ownership filter to the query.
    /// </summary>
    private static IQueryable<Pokemon> ApplyOwnershipFilter(IQueryable<Pokemon> query, bool? hasOwner)
    {
        if (!hasOwner.HasValue) return query;

        return hasOwner.Value
            ? query.Where(p => p.OwnerId != null)
            : query.Where(p => p.OwnerId == null);
    }

    /// <summary>
    /// Processes fuzzy search logic on the filtered Pokemon list.
    /// </summary>
    private List<(PokemonResponseDto Pokemon, double Score)> ProcessFuzzySearch(
        List<Pokemon> allPokemon, PokemonSearchDto searchCriteria)
    {
        var results = new List<(PokemonResponseDto Pokemon, double Score)>();
        var hasTextCriteria = HasTextSearchCriteria(searchCriteria);

        foreach (var pokemon in allPokemon)
        {
            var (score, matchFound) = CalculatePokemonMatchScore(pokemon, searchCriteria, hasTextCriteria);

            if (matchFound)
            {
                results.Add((MapToPokemonResponseDto(pokemon), score));
            }
        }

        return results;
    }

    /// <summary>
    /// Determines if the search criteria contains any text-based filters.
    /// </summary>
    private static bool HasTextSearchCriteria(PokemonSearchDto searchCriteria)
    {
        return !string.IsNullOrWhiteSpace(searchCriteria.Name) ||
               !string.IsNullOrWhiteSpace(searchCriteria.Type) ||
               !string.IsNullOrWhiteSpace(searchCriteria.Owner);
    }

    /// <summary>
    /// Calculates the match score for a Pokemon against search criteria.
    /// </summary>
    private (double score, bool matchFound) CalculatePokemonMatchScore(
        Pokemon pokemon, PokemonSearchDto searchCriteria, bool hasTextCriteria)
    {
        var score = 0.0;
        var matchFound = false;

        if (!hasTextCriteria)
        {
            // Filter-only search (level range and/or HasOwner)
            return (score, true);
        }

        // Process name matching
        var nameResult = ProcessNameMatching(pokemon, searchCriteria);
        if (nameResult.shouldSkip) return (0, false);
        
        score += nameResult.score;
        matchFound = nameResult.matched || matchFound;

        // Process type matching
        var typeResult = ProcessTypeMatching(pokemon, searchCriteria);
        if (typeResult.shouldSkip) return (0, false);
        
        score += typeResult.score;
        matchFound = typeResult.matched || matchFound;

        // Process owner matching
        var ownerResult = ProcessOwnerMatching(pokemon, searchCriteria);
        if (ownerResult.shouldSkip) return (0, false);
        
        score += ownerResult.score;
        matchFound = ownerResult.matched || matchFound;

        return (score, matchFound);
    }

    /// <summary>
    /// Processes name-based matching logic.
    /// </summary>
    private (double score, bool matched, bool shouldSkip) ProcessNameMatching(
        Pokemon pokemon, PokemonSearchDto searchCriteria)
    {
        if (string.IsNullOrWhiteSpace(searchCriteria.Name))
        {
            return (0, true, false); // No name filter means all pass
        }

        if (FuzzyMatchingHelper.IsFuzzyMatch(searchCriteria.Name, pokemon.Name))
        {
            var score = FuzzyMatchingHelper.GetMatchScore(searchCriteria.Name, pokemon.Name);
            return (score, true, false);
        }

        // If only name is specified and it doesn't match, skip this pokemon
        var hasOtherCriteria = !string.IsNullOrWhiteSpace(searchCriteria.Type) ||
                              !string.IsNullOrWhiteSpace(searchCriteria.Owner);
        
        return (0, false, !hasOtherCriteria);
    }

    /// <summary>
    /// Processes type-based matching logic.
    /// </summary>
    private (double score, bool matched, bool shouldSkip) ProcessTypeMatching(
        Pokemon pokemon, PokemonSearchDto searchCriteria)
    {
        if (string.IsNullOrWhiteSpace(searchCriteria.Type))
        {
            return (0, false, false);
        }

        if (FuzzyMatchingHelper.IsFuzzyMatch(searchCriteria.Type, pokemon.Type.Name))
        {
            var score = FuzzyMatchingHelper.GetMatchScore(searchCriteria.Type, pokemon.Type.Name) * TypeMatchWeighting;
            return (score, true, false);
        }

        // If only type is specified and it doesn't match, skip this pokemon
        var hasOtherCriteria = !string.IsNullOrWhiteSpace(searchCriteria.Name) ||
                              !string.IsNullOrWhiteSpace(searchCriteria.Owner);
        
        return (0, false, !hasOtherCriteria);
    }

    /// <summary>
    /// Processes owner-based matching logic.
    /// </summary>
    private (double score, bool matched, bool shouldSkip) ProcessOwnerMatching(
        Pokemon pokemon, PokemonSearchDto searchCriteria)
    {
        if (string.IsNullOrWhiteSpace(searchCriteria.Owner))
        {
            return (0, false, false);
        }

        if (pokemon.Owner != null &&
            FuzzyMatchingHelper.IsFuzzyMatch(searchCriteria.Owner, pokemon.Owner.Name))
        {
            var score = FuzzyMatchingHelper.GetMatchScore(searchCriteria.Owner, pokemon.Owner.Name) * OwnerMatchWeighting;
            return (score, true, false);
        }

        // If only owner is specified and it doesn't match, skip this pokemon
        var hasOtherCriteria = !string.IsNullOrWhiteSpace(searchCriteria.Name) ||
                              !string.IsNullOrWhiteSpace(searchCriteria.Type);
        
        return (0, false, !hasOtherCriteria);
    }

    /// <summary>
    /// Sorts search results by score and then by name.
    /// </summary>
    private static List<PokemonResponseDto> SortSearchResults(
        List<(PokemonResponseDto Pokemon, double Score)> results)
    {
        return results
            .OrderByDescending(r => r.Score)
            .ThenBy(r => r.Pokemon.Name)
            .Select(r => r.Pokemon)
            .ToList();
    }

    /// <summary>
    /// Applies all search filters for paginated results.
    /// </summary>
    private static IQueryable<Pokemon> ApplyPaginatedSearchFilters(
        IQueryable<Pokemon> query, PaginatedPokemonSearchDto searchCriteria)
    {
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

        return ApplyOwnershipFilter(query, searchCriteria.HasOwner);
    }

    /// <summary>
    /// Applies sorting to the query based on the specified criteria.
    /// </summary>
    private static IQueryable<Pokemon> ApplySorting(
        IQueryable<Pokemon> query, string? sortBy, string? sortOrder)
    {
        var normalizedSortBy = sortBy?.ToLower() ?? DefaultSortField;
        var normalizedSortOrder = sortOrder?.ToLower() ?? DefaultSortOrder;
        var isDescending = normalizedSortOrder == DescendingSortOrder;

        return normalizedSortBy switch
        {
            "name" => isDescending 
                ? query.OrderByDescending(p => p.Name) 
                : query.OrderBy(p => p.Name),
            "level" => isDescending 
                ? query.OrderByDescending(p => p.Level) 
                : query.OrderBy(p => p.Level),
            "caughtat" => isDescending 
                ? query.OrderByDescending(p => p.CaughtAt ?? DateTime.MinValue) 
                : query.OrderBy(p => p.CaughtAt ?? DateTime.MinValue),
            "type" => isDescending 
                ? query.OrderByDescending(p => p.Type.Name) 
                : query.OrderBy(p => p.Type.Name),
            _ => query.OrderBy(p => p.Name)
        };
    }

    /// <summary>
    /// Applies pagination to the query.
    /// </summary>
    private static IQueryable<Pokemon> ApplyPagination(
        IQueryable<Pokemon> query, int pageNumber, int pageSize)
    {
        var skipCount = (pageNumber - 1) * pageSize;
        return query.Skip(skipCount).Take(pageSize);
    }

    /// <summary>
    /// Creates a paginated response object.
    /// </summary>
    private static PaginatedResponse<PokemonResponseDto> CreatePaginatedResponse(
        List<PokemonResponseDto> pokemonDtos, int totalItems, PaginatedPokemonSearchDto searchCriteria)
    {
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

    /// <summary>
    /// Maps a Pokemon entity to a PokemonResponseDto.
    /// </summary>
    private static PokemonResponseDto MapToPokemonResponseDto(Pokemon pokemon)
    {
        ArgumentNullException.ThrowIfNull(pokemon);

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
