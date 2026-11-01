using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PokeREST.DTOs;
using PokeREST.Models;
using PokeREST.Services;
using PokeREST.Tests.TestFixtures;
using Xunit;

namespace PokeREST.Tests.Services;

public class TrainerServiceTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;
    private readonly TrainerService _service;

    public TrainerServiceTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _service = new TrainerService(_fixture.Context);
    }

    [Fact]
    public async Task CreateTrainerAsync_WithValidData_ShouldCreateTrainer()
    {
        // Arrange
        var createDto = new CreateTrainerDto
        {
            Name = "Brock",
            Region = "Kanto",
            Age = 15
        };

        // Act
        var result = await _service.CreateTrainerAsync(createDto);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Brock");
        result.Region.Should().Be("Kanto");
        result.Age.Should().Be(15);
        result.Id.Should().BeGreaterThan(0);
        result.PokemonCount.Should().Be(0);
        result.Wins.Should().Be(0);
        result.Losses.Should().Be(0);
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task CreateTrainerAsync_WithMinimalData_ShouldCreateTrainerWithDefaults()
    {
        // Arrange
        var createDto = new CreateTrainerDto
        {
            Name = "Gary Oak"
        };

        // Act
        var result = await _service.CreateTrainerAsync(createDto);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Gary Oak");
        result.Region.Should().BeNull();
        result.Age.Should().BeNull();
        result.Wins.Should().Be(0);
        result.Losses.Should().Be(0);
    }

    [Fact]
    public async Task GetAllTrainersAsync_ShouldReturnAllTrainers()
    {
        // Act
        var result = await _service.GetAllTrainersAsync();

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().BeGreaterOrEqualTo(2); // From seeded data
        result.Should().Contain(t => t.Name == "Ash Ketchum");
        result.Should().Contain(t => t.Name == "Misty");
    }

    [Fact]
    public async Task GetTrainerByIdAsync_WithExistingId_ShouldReturnTrainerWithPokemon()
    {
        // Act
        var result = await _service.GetTrainerByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Ash Ketchum");
        result.Region.Should().Be("Kanto");
        result.Age.Should().Be(10);
        result.Pokemons.Should().NotBeEmpty();
        result.Pokemons.Should().Contain(p => p.Name == "Pikachu");
    }

    [Fact]
    public async Task GetTrainerByIdAsync_WithNonExistingId_ShouldReturnNull()
    {
        // Act
        var result = await _service.GetTrainerByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateTrainerAsync_WithValidData_ShouldUpdateTrainer()
    {
        // Arrange
        var updateDto = new UpdateTrainerDto
        {
            Name = "Ash Ketchum Updated",
            Region = "Johto",
            Age = 11,
            Wins = 60,
            Losses = 25
        };

        // Act
        var result = await _service.UpdateTrainerAsync(1, updateDto);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Ash Ketchum Updated");
        result.Region.Should().Be("Johto");
        result.Age.Should().Be(11);
        result.Wins.Should().Be(60);
        result.Losses.Should().Be(25);
    }

    [Fact]
    public async Task UpdateTrainerAsync_WithPartialData_ShouldUpdateOnlyProvidedFields()
    {
        // Arrange
        var updateDto = new UpdateTrainerDto
        {
            Wins = 55
        };

        // Act
        var result = await _service.UpdateTrainerAsync(2, updateDto);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Misty"); // Should remain unchanged
        result.Region.Should().Be("Kanto"); // Should remain unchanged
        result.Wins.Should().Be(55); // Should be updated
    }

    [Fact]
    public async Task UpdateTrainerAsync_WithNonExistingId_ShouldReturnNull()
    {
        // Arrange
        var updateDto = new UpdateTrainerDto
        {
            Name = "Non Existing"
        };

        // Act
        var result = await _service.UpdateTrainerAsync(999, updateDto);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteTrainerAsync_WithExistingId_ShouldDeleteTrainerAndReleasePokemons()
    {
        // First, verify trainer exists
        var trainerExists = await _service.TrainerExistsAsync(1);
        trainerExists.Should().BeTrue();

        // Act
        var result = await _service.DeleteTrainerAsync(1);

        // Assert
        result.Should().BeTrue();

        // Verify trainer is deleted
        var deletedTrainer = await _service.GetTrainerByIdAsync(1);
        deletedTrainer.Should().BeNull();

        // Verify Pokemon are released (owner set to null)
        var releasedPokemon = await _fixture.Context.Pokemons
            .Where(p => p.Name == "Pikachu")
            .FirstOrDefaultAsync();
        releasedPokemon.Should().NotBeNull();
        releasedPokemon!.OwnerId.Should().BeNull();
    }

    [Fact]
    public async Task DeleteTrainerAsync_WithNonExistingId_ShouldReturnFalse()
    {
        // Act
        var result = await _service.DeleteTrainerAsync(999);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task TrainerExistsAsync_WithExistingId_ShouldReturnTrue()
    {
        // Act
        var result = await _service.TrainerExistsAsync(2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task TrainerExistsAsync_WithNonExistingId_ShouldReturnFalse()
    {
        // Act
        var result = await _service.TrainerExistsAsync(999);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetPaginatedTrainersAsync_WithBasicPagination_ShouldReturnPagedResults()
    {
        // Arrange
        var searchCriteria = new PaginatedTrainerSearchDto
        {
            PageNumber = 1,
            PageSize = 1
        };

        // Act
        var result = await _service.GetPaginatedTrainersAsync(searchCriteria);

        // Assert
        result.Should().NotBeNull();
        result.Data.Count.Should().Be(1);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(1);
        result.TotalItems.Should().BeGreaterOrEqualTo(2);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task GetPaginatedTrainersAsync_WithNameFilter_ShouldReturnFilteredResults()
    {
        // Arrange
        var searchCriteria = new PaginatedTrainerSearchDto
        {
            Name = "Ash",
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _service.GetPaginatedTrainersAsync(searchCriteria);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().AllSatisfy(t => t.Name.Should().Contain("Ash"));
    }

    [Fact]
    public async Task GetPaginatedTrainersAsync_WithAgeFilter_ShouldReturnFilteredResults()
    {
        // Arrange
        var searchCriteria = new PaginatedTrainerSearchDto
        {
            MinAge = 12,
            MaxAge = 15,
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _service.GetPaginatedTrainersAsync(searchCriteria);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().AllSatisfy(t => t.Age.Should().BeInRange(12, 15));
    }

    [Fact]
    public async Task GetPaginatedTrainersAsync_WithSorting_ShouldReturnSortedResults()
    {
        // Arrange
        var searchCriteria = new PaginatedTrainerSearchDto
        {
            SortBy = "age",
            SortOrder = "desc",
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _service.GetPaginatedTrainersAsync(searchCriteria);

        // Assert
        result.Should().NotBeNull();
        var ages = result.Data.Select(t => t.Age ?? 0).ToList();
        ages.Should().BeInDescendingOrder();
    }

    [Fact]
    public void MapToTrainerWithPokemonDto_WhenPokemonTypeOrMoveIsNull_MapsWithoutThrowing()
    {
        var trainer = new Trainer
        {
            Id = 5,
            Name = null!,
            Pokemons = null!
        };
        var summary = InvokePrivate<TrainerResponseDto>(typeof(TrainerService), "MapToTrainerResponseDto", trainer);
        summary.Name.Should().BeEmpty();
        summary.PokemonCount.Should().Be(0);

        trainer.Pokemons = new List<Pokemon>
        {
            new()
            {
                Id = 100,
                Name = "Typeless",
                Level = 4,
                Health = 20,
                Type = null!,
                PokemonMoves = new List<PokemonMove>
                {
                    new() { Move = null! },
                    new()
                    {
                        Move = new Move
                        {
                            Id = 80,
                            Name = "Mystery",
                            Power = 15,
                            Type = null!
                        }
                    }
                }
            }
        };

        var detailed = InvokePrivate<TrainerWithPokemonDto>(typeof(TrainerService), "MapToTrainerWithPokemonDto", trainer);

        detailed.PokemonCount.Should().Be(1);
        var pokemon = detailed.Pokemons.Should().ContainSingle().Subject;
        pokemon.Type.Should().BeEmpty();
        pokemon.Moves.Should().ContainSingle();
        pokemon.Moves[0].Name.Should().Be("Mystery");
        pokemon.Moves[0].Type.Should().BeEmpty();
    }

    private static T InvokePrivate<T>(Type type, string methodName, params object?[] args)
    {
        var method = type.GetMethod(
            methodName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        var result = method!.Invoke(null, args);
        return (T)result!;
    }
}
