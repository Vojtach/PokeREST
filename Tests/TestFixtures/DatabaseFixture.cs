using Microsoft.EntityFrameworkCore;
using PokeREST.Models;

namespace PokeREST.Tests.TestFixtures;

public class DatabaseFixture : IDisposable
{
    private readonly DbContextOptions<PokemonContext> _options;
    public PokemonContext Context { get; private set; }

    public DatabaseFixture()
    {
        _options = new DbContextOptionsBuilder<PokemonContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        Context = new PokemonContext(_options);
        SeedDatabase();
    }
    
    public PokemonContext CreateFreshContext()
    {
        var newOptions = new DbContextOptionsBuilder<PokemonContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var newContext = new PokemonContext(newOptions);
        SeedDatabaseForContext(newContext);
        return newContext;
    }

    private void SeedDatabase()
    {
        SeedDatabaseForContext(Context);
    }
    
    private static void SeedDatabaseForContext(PokemonContext context)
    {
        // Seed Elements (Types)
        var fireType = new Element { Id = 1, Name = "Fire" };
        var waterType = new Element { Id = 2, Name = "Water" };
        var grassType = new Element { Id = 3, Name = "Grass" };
        var electricType = new Element { Id = 4, Name = "Electric" };

        context.Elements.AddRange(fireType, waterType, grassType, electricType);

        // Seed Trainers
        var trainer1 = new Trainer
        {
            Id = 1,
            Name = "Ash Ketchum",
            Region = "Kanto",
            Age = 10,
            Wins = 50,
            Losses = 20,
            CreatedAt = DateTime.UtcNow.AddDays(-100)
        };

        var trainer2 = new Trainer
        {
            Id = 2,
            Name = "Misty",
            Region = "Kanto", 
            Age = 12,
            Wins = 30,
            Losses = 15,
            CreatedAt = DateTime.UtcNow.AddDays(-90)
        };

        context.Trainers.AddRange(trainer1, trainer2);

        // Seed Moves
        var moves = new[]
        {
            new Move { Id = 1, Name = "Tackle", Power = 40, TypeId = 1 },
            new Move { Id = 2, Name = "Thunderbolt", Power = 90, TypeId = 4 },
            new Move { Id = 3, Name = "Water Gun", Power = 40, TypeId = 2 },
            new Move { Id = 4, Name = "Vine Whip", Power = 45, TypeId = 3 }
        };

        context.Moves.AddRange(moves);

        // Seed Pokemon
        var pikachu = new Pokemon
        {
            Id = 1,
            Name = "Pikachu",
            Level = 25,
            TypeId = 4,
            OwnerId = 1,
            Health = 100,
            CaughtAt = DateTime.UtcNow.AddDays(-50)
        };

        var squirtle = new Pokemon
        {
            Id = 2,
            Name = "Squirtle",
            Level = 15,
            TypeId = 2,
            OwnerId = 2,
            Health = 80,
            CaughtAt = DateTime.UtcNow.AddDays(-30)
        };

        var wildCharmander = new Pokemon
        {
            Id = 3,
            Name = "Charmander",
            Level = 12,
            TypeId = 1,
            OwnerId = null,
            Health = 70,
            CaughtAt = null
        };
        
        var wildBulbasaur = new Pokemon
        {
            Id = 4,
            Name = "Bulbasaur",
            Level = 8,
            TypeId = 3,
            OwnerId = null,
            Health = 60,
            CaughtAt = null
        };

        context.Pokemons.AddRange(pikachu, squirtle, wildCharmander, wildBulbasaur);

        // Seed Pokemon Moves
        var pokemonMoves = new[]
        {
            new PokemonMove { PokemonId = 1, MoveId = 2 }, // Pikachu knows Thunderbolt
            new PokemonMove { PokemonId = 2, MoveId = 3 }, // Squirtle knows Water Gun
            new PokemonMove { PokemonId = 3, MoveId = 1 }  // Charmander knows Tackle
        };

        context.PokemonMoves.AddRange(pokemonMoves);

        context.SaveChanges();
    }

    public void Dispose()
    {
        Context.Dispose();
    }
}
