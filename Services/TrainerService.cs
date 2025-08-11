using Microsoft.EntityFrameworkCore;
using PokeREST.DTOs;
using PokeREST.Models;

namespace PokeREST.Services;

public class TrainerService : ITrainerService
{
    private readonly PokemonContext _context;

    public TrainerService(PokemonContext context)
    {
        _context = context;
    }

    public async Task<TrainerResponseDto?> CreateTrainerAsync(CreateTrainerDto createTrainerDto)
    {
        var trainer = new Trainer
        {
            Name = createTrainerDto.Name,
            Region = createTrainerDto.Region,
            Age = createTrainerDto.Age,
            CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified), // TODO: Change DB column to "timestamp with time zone" for long term reliability
            Wins = 0,
            Losses = 0
        };

        _context.Trainers.Add(trainer);
        await _context.SaveChangesAsync();

        return MapToTrainerResponseDto(trainer);
    }

    public async Task<List<TrainerResponseDto>> GetAllTrainersAsync()
    {
        var trainers = await _context.Trainers
            .AsNoTracking()
            .Include(t => t.Pokemons)
            .ToListAsync();

        return trainers.Select(MapToTrainerResponseDto).ToList();
    }

    public async Task<TrainerWithPokemonDto?> GetTrainerByIdAsync(int id)
    {
        var trainer = await _context.Trainers
            .AsNoTracking()
            .Include(t => t.Pokemons)
            .ThenInclude(p => p.Type)
            .Include(t => t.Pokemons)
            .ThenInclude(p => p.PokemonMoves)
            .ThenInclude(pm => pm.Move)
            .ThenInclude(m => m.Type)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (trainer == null)
            return null;

        return MapToTrainerWithPokemonDto(trainer);
    }

    public async Task<TrainerResponseDto?> UpdateTrainerAsync(int id, UpdateTrainerDto updateTrainerDto)
    {
        var trainer = await _context.Trainers.FindAsync(id);
        if (trainer == null)
            return null;

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

        await _context.SaveChangesAsync();
        
        await _context.Entry(trainer)
            .Collection(t => t.Pokemons)
            .LoadAsync();

        return MapToTrainerResponseDto(trainer);
    }

    public async Task<bool> DeleteTrainerAsync(int id)
    {
        var trainer = await _context.Trainers
            .Include(t => t.Pokemons)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (trainer == null)
            return false;

        // Set Pokemon owner to null (release them) instead of deleting
        foreach (var pokemon in trainer.Pokemons)
        {
            pokemon.OwnerId = null;
        }

        _context.Trainers.Remove(trainer);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> TrainerExistsAsync(int id)
    {
        return await _context.Trainers
            .AsNoTracking()
            .AnyAsync(t => t.Id == id);
    }

    public async Task<PaginatedResponse<TrainerResponseDto>> GetPaginatedTrainersAsync(PaginatedTrainerSearchDto searchCriteria)
    {
        var query = _context.Trainers
            .AsNoTracking()
            .Include(t => t.Pokemons)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(searchCriteria.Name))
        {
            var searchName = searchCriteria.Name.ToLower();
            query = query.Where(t => t.Name.ToLower().Contains(searchName));
        }

        if (!string.IsNullOrWhiteSpace(searchCriteria.Region))
        {
            var searchRegion = searchCriteria.Region.ToLower();
            query = query.Where(t => t.Region != null && t.Region.ToLower().Contains(searchRegion));
        }

        if (searchCriteria.MinAge.HasValue)
        {
            query = query.Where(t => t.Age >= searchCriteria.MinAge.Value);
        }

        if (searchCriteria.MaxAge.HasValue)
        {
            query = query.Where(t => t.Age <= searchCriteria.MaxAge.Value);
        }

        if (searchCriteria.MinWins.HasValue)
        {
            query = query.Where(t => t.Wins >= searchCriteria.MinWins.Value);
        }

        if (searchCriteria.MaxWins.HasValue)
        {
            query = query.Where(t => t.Wins <= searchCriteria.MaxWins.Value);
        }

        if (searchCriteria.MinPokemonCount.HasValue)
        {
            query = query.Where(t => t.Pokemons.Count >= searchCriteria.MinPokemonCount.Value);
        }

        if (searchCriteria.MaxPokemonCount.HasValue)
        {
            query = query.Where(t => t.Pokemons.Count <= searchCriteria.MaxPokemonCount.Value);
        }

        // Get total count before pagination
        var totalItems = await query.CountAsync();

        // Apply sorting
        var sortBy = searchCriteria.SortBy?.ToLower() ?? "name";
        var sortOrder = searchCriteria.SortOrder?.ToLower() ?? "asc";

        query = sortBy switch
        {
            "name" => sortOrder == "desc" ? query.OrderByDescending(t => t.Name) : query.OrderBy(t => t.Name),
            "region" => sortOrder == "desc" ? query.OrderByDescending(t => t.Region ?? string.Empty) : query.OrderBy(t => t.Region ?? string.Empty),
            "age" => sortOrder == "desc" ? query.OrderByDescending(t => t.Age ?? 0) : query.OrderBy(t => t.Age ?? 0),
            "wins" => sortOrder == "desc" ? query.OrderByDescending(t => t.Wins ?? 0) : query.OrderBy(t => t.Wins ?? 0),
            "losses" => sortOrder == "desc" ? query.OrderByDescending(t => t.Losses ?? 0) : query.OrderBy(t => t.Losses ?? 0),
            "pokemoncount" => sortOrder == "desc" ? query.OrderByDescending(t => t.Pokemons.Count) : query.OrderBy(t => t.Pokemons.Count),
            "createdat" => sortOrder == "desc" ? query.OrderByDescending(t => t.CreatedAt ?? DateTime.MinValue) : query.OrderBy(t => t.CreatedAt ?? DateTime.MinValue),
            _ => query.OrderBy(t => t.Name)
        };

        // Apply pagination
        var skipCount = (searchCriteria.PageNumber - 1) * searchCriteria.PageSize;
        var paginatedQuery = query.Skip(skipCount).Take(searchCriteria.PageSize);

        var trainers = await paginatedQuery.ToListAsync();
        var trainerDtos = trainers.Select(MapToTrainerResponseDto).ToList();

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

    private static TrainerResponseDto MapToTrainerResponseDto(Trainer trainer)
    {
        return new TrainerResponseDto
        {
            Id = trainer.Id,
            Name = trainer.Name,
            Region = trainer.Region,
            Age = trainer.Age,
            CreatedAt = trainer.CreatedAt,
            Wins = trainer.Wins,
            Losses = trainer.Losses,
            PokemonCount = trainer.Pokemons.Count
        };
    }

    private static TrainerWithPokemonDto MapToTrainerWithPokemonDto(Trainer trainer)
    {
        return new TrainerWithPokemonDto
        {
            Id = trainer.Id,
            Name = trainer.Name,
            Region = trainer.Region,
            Age = trainer.Age,
            CreatedAt = trainer.CreatedAt,
            Wins = trainer.Wins,
            Losses = trainer.Losses,
            PokemonCount = trainer.Pokemons.Count,
            Pokemons = trainer.Pokemons.Select(p => new PokemonResponseDto
            {
                Id = p.Id,
                Name = p.Name,
                Level = p.Level,
                Type = p.Type.Name,
                Owner = trainer.Name,
                Health = p.Health,
                CaughtAt = p.CaughtAt,
                Moves = p.PokemonMoves.Select(pm => new MoveResponseDto
                {
                    Id = pm.Move.Id,
                    Name = pm.Move.Name,
                    Power = pm.Move.Power,
                    Type = pm.Move.Type.Name
                }).ToList()
            }).ToList()
        };
    }
}
