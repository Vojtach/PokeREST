using Microsoft.EntityFrameworkCore;
using PokeREST.DTOs;
using PokeREST.Models;

namespace PokeREST.Services;

/// <summary>
/// Implementation of trainer management service providing comprehensive trainer operations.
/// Handles CRUD operations, advanced searching with filtering, pagination, and Pokemon ownership management.
/// Includes efficient query building and comprehensive trainer data mapping.
/// </summary>
public class TrainerService : ITrainerService
{
    private readonly PokemonContext _context;
    
    // Constants for better maintainability
    private const string DefaultSortField = "name";
    private const string DefaultSortOrder = "asc";
    private const string DescendingSortOrder = "desc";
    private const int DefaultWins = 0;
    private const int DefaultLosses = 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="TrainerService"/> class.
    /// </summary>
    /// <param name="context">Request-scoped EF Core context used for all data access.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is <c>null</c>.</exception>
    public TrainerService(PokemonContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc/>
    public async Task<TrainerResponseDto?> CreateTrainerAsync(CreateTrainerDto createTrainerDto)
    {
        ArgumentNullException.ThrowIfNull(createTrainerDto);

        var trainer = BuildNewTrainer(createTrainerDto);
        
        _context.Trainers.Add(trainer);
        await _context.SaveChangesAsync();

        // Never null in practice: failures surface as exceptions from SaveChangesAsync.
        return MapToTrainerResponseDto(trainer);
    }

    /// <inheritdoc/>
    public async Task<List<TrainerResponseDto>> GetAllTrainersAsync()
    {
        var trainers = await BuildBaseTrainerQuery().ToListAsync();
        return trainers.Select(MapToTrainerResponseDto).ToList();
    }

    /// <inheritdoc/>
    public async Task<TrainerWithPokemonDto?> GetTrainerByIdAsync(int id)
    {
        ValidateId(id);

        var trainer = await BuildDetailedTrainerQuery()
            .FirstOrDefaultAsync(t => t.Id == id);

        return trainer == null ? null : MapToTrainerWithPokemonDto(trainer);
    }

    /// <inheritdoc/>
    public async Task<TrainerResponseDto?> UpdateTrainerAsync(int id, UpdateTrainerDto updateTrainerDto)
    {
        ValidateId(id);
        ArgumentNullException.ThrowIfNull(updateTrainerDto);

        var trainer = await _context.Trainers.FindAsync(id);
        if (trainer == null)
            return null;

        ApplyUpdatesToTrainer(trainer, updateTrainerDto);
        
        await _context.SaveChangesAsync();
        // FindAsync does not load navigations; without this, PokemonCount in the response would always be 0.
        await LoadTrainerPokemonCollection(trainer);

        return MapToTrainerResponseDto(trainer);
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteTrainerAsync(int id)
    {
        ValidateId(id);

        // Pokemons are loaded so they are tracked and can be released in the same SaveChanges. The Owner FK is
        // optional (ClientSetNull), so the database does not null OwnerId itself; untracked Pokemon would
        // violate Pokemon_OwnerId_fkey when the trainer row is removed.
        var trainer = await _context.Trainers
            .Include(t => t.Pokemons)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (trainer == null)
            return false;

        ReleasePokemonFromTrainer(trainer);
        _context.Trainers.Remove(trainer);
        await _context.SaveChangesAsync();

        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> TrainerExistsAsync(int id)
    {
        ValidateId(id);
        
        return await _context.Trainers
            .AsNoTracking()
            .AnyAsync(t => t.Id == id);
    }

    /// <inheritdoc/>
    public async Task<PaginatedResponse<TrainerResponseDto>> GetPaginatedTrainersAsync(PaginatedTrainerSearchDto searchCriteria)
    {
        ArgumentNullException.ThrowIfNull(searchCriteria);

        var query = BuildBaseTrainerQuery();
        query = ApplySearchFilters(query, searchCriteria);
        
        var totalItems = await query.CountAsync();
        
        query = ApplySorting(query, searchCriteria.SortBy, searchCriteria.SortOrder);
        query = ApplyPagination(query, searchCriteria.PageNumber, searchCriteria.PageSize);
        
        var trainers = await query.ToListAsync();
        var trainerDtos = trainers.Select(MapToTrainerResponseDto).ToList();
        
        return CreatePaginatedResponse(trainerDtos, totalItems, searchCriteria);
    }

    #region Private Helper Methods - Query Building
    
    /// <summary>
    /// Builds the base query with necessary includes for trainers.
    /// </summary>
    private IQueryable<Trainer> BuildBaseTrainerQuery()
    {
        return _context.Trainers
            .AsNoTracking()
            .Include(t => t.Pokemons)
            .AsQueryable();
    }
    
    /// <summary>
    /// Builds the detailed query with all necessary includes for trainer details.
    /// </summary>
    private IQueryable<Trainer> BuildDetailedTrainerQuery()
    {
        return _context.Trainers
            .AsNoTracking()
            .Include(t => t.Pokemons)
            .ThenInclude(p => p.Type)
            .Include(t => t.Pokemons)
            .ThenInclude(p => p.PokemonMoves)
            .ThenInclude(pm => pm.Move)
            .ThenInclude(m => m.Type)
            .AsQueryable();
    }
    
    /// <summary>
    /// Creates a new trainer entity from the DTO.
    /// </summary>
    private static Trainer BuildNewTrainer(CreateTrainerDto createTrainerDto)
    {
        return new Trainer
        {
            Name = createTrainerDto.Name,
            Region = createTrainerDto.Region,
            Age = createTrainerDto.Age,
            CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified),
            Wins = DefaultWins,
            Losses = DefaultLosses
        };
    }
    
    #endregion
    
    #region Private Helper Methods - Filtering and Searching
    
    /// <summary>
    /// Applies search filters to the trainer query.
    /// </summary>
    /// <remarks>
    /// All filters are AND-combined and translated to SQL. Text filters are case-insensitive substring matches.
    /// Range filters on nullable columns (Age, Wins) exclude trainers whose value is null.
    /// </remarks>
    private static IQueryable<Trainer> ApplySearchFilters(IQueryable<Trainer> query, PaginatedTrainerSearchDto searchCriteria)
    {
        query = ApplyNameFilter(query, searchCriteria.Name);
        query = ApplyRegionFilter(query, searchCriteria.Region);
        query = ApplyAgeFilters(query, searchCriteria.MinAge, searchCriteria.MaxAge);
        query = ApplyWinFilters(query, searchCriteria.MinWins, searchCriteria.MaxWins);
        query = ApplyPokemonCountFilters(query, searchCriteria.MinPokemonCount, searchCriteria.MaxPokemonCount);
        
        return query;
    }
    
    /// <summary>
    /// Applies name filter to the query.
    /// </summary>
    private static IQueryable<Trainer> ApplyNameFilter(IQueryable<Trainer> query, string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return query;
        
        var searchName = name.ToLower();
        return query.Where(t => t.Name.ToLower().Contains(searchName));
    }
    
    /// <summary>
    /// Applies region filter to the query.
    /// </summary>
    private static IQueryable<Trainer> ApplyRegionFilter(IQueryable<Trainer> query, string? region)
    {
        if (string.IsNullOrWhiteSpace(region)) return query;
        
        var searchRegion = region.ToLower();
        return query.Where(t => t.Region != null && t.Region.ToLower().Contains(searchRegion));
    }
    
    /// <summary>
    /// Applies age range filters to the query.
    /// </summary>
    private static IQueryable<Trainer> ApplyAgeFilters(IQueryable<Trainer> query, int? minAge, int? maxAge)
    {
        if (minAge.HasValue)
        {
            query = query.Where(t => t.Age >= minAge.Value);
        }
        
        if (maxAge.HasValue)
        {
            query = query.Where(t => t.Age <= maxAge.Value);
        }
        
        return query;
    }
    
    /// <summary>
    /// Applies win range filters to the query.
    /// </summary>
    private static IQueryable<Trainer> ApplyWinFilters(IQueryable<Trainer> query, int? minWins, int? maxWins)
    {
        if (minWins.HasValue)
        {
            query = query.Where(t => t.Wins >= minWins.Value);
        }
        
        if (maxWins.HasValue)
        {
            query = query.Where(t => t.Wins <= maxWins.Value);
        }
        
        return query;
    }
    
    /// <summary>
    /// Applies Pokemon count range filters to the query.
    /// </summary>
    private static IQueryable<Trainer> ApplyPokemonCountFilters(IQueryable<Trainer> query, int? minCount, int? maxCount)
    {
        if (minCount.HasValue)
        {
            query = query.Where(t => t.Pokemons.Count >= minCount.Value);
        }
        
        if (maxCount.HasValue)
        {
            query = query.Where(t => t.Pokemons.Count <= maxCount.Value);
        }
        
        return query;
    }
    
    #endregion
    
    #region Private Helper Methods - Sorting and Pagination
    
    /// <summary>
    /// Applies sorting to the trainer query.
    /// </summary>
    /// <remarks>
    /// Nullable columns are coalesced (empty region, 0 age/wins/losses, <see cref="DateTime.MinValue"/> for createdAt),
    /// so trainers with missing values sort first in ascending order. Only a single sort key is applied, with no
    /// tie-breaker, so trainers with equal keys may appear on different pages between requests.
    /// </remarks>
    private static IQueryable<Trainer> ApplySorting(IQueryable<Trainer> query, string? sortBy, string? sortOrder)
    {
        var normalizedSortBy = sortBy?.ToLower() ?? DefaultSortField;
        var normalizedSortOrder = sortOrder?.ToLower() ?? DefaultSortOrder;
        
        return normalizedSortBy switch
        {
            "name" => ApplyNameSorting(query, normalizedSortOrder),
            "region" => ApplyRegionSorting(query, normalizedSortOrder),
            "age" => ApplyAgeSorting(query, normalizedSortOrder),
            "wins" => ApplyWinsSorting(query, normalizedSortOrder),
            "losses" => ApplyLossesSorting(query, normalizedSortOrder),
            "pokemoncount" => ApplyPokemonCountSorting(query, normalizedSortOrder),
            "createdat" => ApplyCreatedAtSorting(query, normalizedSortOrder),
            _ => query.OrderBy(t => t.Name)
        };
    }
    
    private static IQueryable<Trainer> ApplyNameSorting(IQueryable<Trainer> query, string sortOrder)
    {
        return sortOrder == DescendingSortOrder 
            ? query.OrderByDescending(t => t.Name) 
            : query.OrderBy(t => t.Name);
    }
    
    private static IQueryable<Trainer> ApplyRegionSorting(IQueryable<Trainer> query, string sortOrder)
    {
        return sortOrder == DescendingSortOrder 
            ? query.OrderByDescending(t => t.Region ?? string.Empty) 
            : query.OrderBy(t => t.Region ?? string.Empty);
    }
    
    private static IQueryable<Trainer> ApplyAgeSorting(IQueryable<Trainer> query, string sortOrder)
    {
        return sortOrder == DescendingSortOrder 
            ? query.OrderByDescending(t => t.Age ?? 0) 
            : query.OrderBy(t => t.Age ?? 0);
    }
    
    private static IQueryable<Trainer> ApplyWinsSorting(IQueryable<Trainer> query, string sortOrder)
    {
        return sortOrder == DescendingSortOrder 
            ? query.OrderByDescending(t => t.Wins ?? 0) 
            : query.OrderBy(t => t.Wins ?? 0);
    }
    
    private static IQueryable<Trainer> ApplyLossesSorting(IQueryable<Trainer> query, string sortOrder)
    {
        return sortOrder == DescendingSortOrder 
            ? query.OrderByDescending(t => t.Losses ?? 0) 
            : query.OrderBy(t => t.Losses ?? 0);
    }
    
    private static IQueryable<Trainer> ApplyPokemonCountSorting(IQueryable<Trainer> query, string sortOrder)
    {
        return sortOrder == DescendingSortOrder 
            ? query.OrderByDescending(t => t.Pokemons.Count) 
            : query.OrderBy(t => t.Pokemons.Count);
    }
    
    private static IQueryable<Trainer> ApplyCreatedAtSorting(IQueryable<Trainer> query, string sortOrder)
    {
        return sortOrder == DescendingSortOrder 
            ? query.OrderByDescending(t => t.CreatedAt ?? DateTime.MinValue) 
            : query.OrderBy(t => t.CreatedAt ?? DateTime.MinValue);
    }
    
    /// <summary>
    /// Applies pagination to the query.
    /// </summary>
    private static IQueryable<Trainer> ApplyPagination(IQueryable<Trainer> query, int pageNumber, int pageSize)
    {
        // No guard here or in the controller: pageNumber < 1 or a negative pageSize yields a negative
        // OFFSET/LIMIT, which the database rejects, so the request ends in a 500.
        var skipCount = (pageNumber - 1) * pageSize;
        return query.Skip(skipCount).Take(pageSize);
    }
    
    #endregion
    
    #region Private Helper Methods - Business Logic
    
    /// <summary>
    /// Applies updates from DTO to trainer entity.
    /// </summary>
    private static void ApplyUpdatesToTrainer(Trainer trainer, UpdateTrainerDto updateTrainerDto)
    {
        if (!string.IsNullOrEmpty(updateTrainerDto.Name))
            trainer.Name = updateTrainerDto.Name;
        
        if (updateTrainerDto.Region != null)
            trainer.Region = updateTrainerDto.Region;
        
        if (updateTrainerDto.Age.HasValue)
            trainer.Age = updateTrainerDto.Age;
        
        if (updateTrainerDto.Wins.HasValue)
            trainer.Wins = updateTrainerDto.Wins;
        
        if (updateTrainerDto.Losses.HasValue)
            trainer.Losses = updateTrainerDto.Losses;
    }
    
    /// <summary>
    /// Loads the Pokemon collection for a trainer.
    /// </summary>
    private async Task LoadTrainerPokemonCollection(Trainer trainer)
    {
        await _context.Entry(trainer)
            .Collection(t => t.Pokemons)
            .LoadAsync();
    }
    
    /// <summary>
    /// Releases all Pokemon owned by a trainer.
    /// </summary>
    private static void ReleasePokemonFromTrainer(Trainer trainer)
    {
        foreach (var pokemon in trainer.Pokemons)
        {
            pokemon.OwnerId = null;
        }
    }
    
    /// <summary>
    /// Creates a paginated response from the query results.
    /// </summary>
    private static PaginatedResponse<TrainerResponseDto> CreatePaginatedResponse(
        List<TrainerResponseDto> trainerDtos, 
        int totalItems, 
        PaginatedTrainerSearchDto searchCriteria)
    {
        // pageSize 0 is not rejected: the division yields Infinity/NaN, so TotalPages is meaningless and Data is empty.
        var totalPages = (int)Math.Ceiling((double)totalItems / searchCriteria.PageSize);
        
        return new PaginatedResponse<TrainerResponseDto>
        {
            Data = trainerDtos,
            TotalItems = totalItems,
            PageNumber = searchCriteria.PageNumber,
            PageSize = searchCriteria.PageSize,
            TotalPages = totalPages,
            HasPreviousPage = searchCriteria.PageNumber > 1,
            HasNextPage = searchCriteria.PageNumber < totalPages
        };
    }
    
    #endregion
    
    #region Private Helper Methods - Validation
    
    /// <summary>
    /// Validates that the ID is positive.
    /// </summary>
    /// <remarks>
    /// Controllers do not translate the resulting <see cref="ArgumentException"/> into a 400; it is caught by their
    /// generic handler, so a non-positive ID currently produces a 500 response.
    /// </remarks>
    private static void ValidateId(int id)
    {
        if (id <= 0)
            throw new ArgumentException("ID must be a positive integer.", nameof(id));
    }
    
    #endregion
    
    #region Private Helper Methods - Mapping
    
    /// <summary>
    /// Maps a Trainer entity to TrainerResponseDto.
    /// </summary>
    /// <remarks>
    /// PokemonCount is taken from the loaded Pokemons collection, so callers must include or load it first;
    /// otherwise the count is silently reported as 0.
    /// </remarks>
    private static TrainerResponseDto MapToTrainerResponseDto(Trainer trainer)
    {
        return new TrainerResponseDto
        {
            Id = trainer.Id,
            Name = trainer.Name ?? string.Empty,
            Region = trainer.Region,
            Age = trainer.Age,
            CreatedAt = trainer.CreatedAt,
            Wins = trainer.Wins,
            Losses = trainer.Losses,
            PokemonCount = trainer.Pokemons?.Count ?? 0
        };
    }
    
    /// <summary>
    /// Maps a Trainer entity to TrainerWithPokemonDto.
    /// </summary>
    private static TrainerWithPokemonDto MapToTrainerWithPokemonDto(Trainer trainer)
    {
        return new TrainerWithPokemonDto
        {
            Id = trainer.Id,
            Name = trainer.Name ?? string.Empty,
            Region = trainer.Region,
            Age = trainer.Age,
            CreatedAt = trainer.CreatedAt,
            Wins = trainer.Wins,
            Losses = trainer.Losses,
            PokemonCount = trainer.Pokemons?.Count ?? 0,
            Pokemons = (trainer.Pokemons ?? Enumerable.Empty<Pokemon>()).Select(MapToPokemonResponseDto).ToList()
        };
    }
    
    /// <summary>
    /// Maps a Pokemon entity to PokemonResponseDto.
    /// </summary>
    /// <remarks>
    /// Duplicates the mapping in PokemonService; keep the two in sync.
    /// Missing type, move, or move-type navigations become empty strings. A join row whose move is null is omitted.
    /// </remarks>
    private static PokemonResponseDto MapToPokemonResponseDto(Pokemon pokemon)
    {
        return new PokemonResponseDto
        {
            Id = pokemon.Id,
            Name = pokemon.Name ?? string.Empty,
            Level = pokemon.Level,
            Type = pokemon.Type?.Name ?? string.Empty,
            Owner = pokemon.Owner?.Name,
            Health = pokemon.Health,
            CaughtAt = pokemon.CaughtAt,
            Moves = (pokemon.PokemonMoves ?? Enumerable.Empty<PokemonMove>())
                .Where(pm => pm?.Move != null)
                .Select(MapToMoveResponseDto)
                .ToList()
        };
    }
    
    /// <summary>
    /// Maps a PokemonMove to MoveResponseDto.
    /// </summary>
    private static MoveResponseDto MapToMoveResponseDto(PokemonMove pokemonMove)
    {
        return new MoveResponseDto
        {
            Id = pokemonMove.Move.Id,
            Name = pokemonMove.Move.Name ?? string.Empty,
            Power = pokemonMove.Move.Power,
            Type = pokemonMove.Move.Type?.Name ?? string.Empty
        };
    }
    
    #endregion
}
