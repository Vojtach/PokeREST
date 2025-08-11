using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PokeREST.DTOs;
using PokeREST.Models;
using PokeREST.Services;
using PokeREST.Tests.TestFixtures;
using Xunit;

namespace PokeREST.Tests.Services;

public class PokemonServiceTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;
    private readonly PokemonService _service;

    public PokemonServiceTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _service = new PokemonService(_fixture.Context);
    }

    [Fact]
    public async Task AddExistingPokemonToTrainerAsync_WithValidIds_ShouldAssignPokemonToTrainer()
    {
        // Arrange - Use the wild Bulbasaur (ID 4) and trainer 1
        var trainerId = 1;
        var pokemonId = 4;

        // Act
        var result = await _service.AddExistingPokemonToTrainerAsync(trainerId, pokemonId);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Bulbasaur");
        result.Owner.Should().Be("Ash Ketchum");
        result.CaughtAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));

        // Verify in database
        var updatedPokemon = await _fixture.Context.Pokemons
            .FirstOrDefaultAsync(p => p.Id == pokemonId);
        updatedPokemon.Should().NotBeNull();
        updatedPokemon!.OwnerId.Should().Be(trainerId);
        updatedPokemon.CaughtAt.Should().NotBeNull();
    }

    [Fact]
    public async Task AddExistingPokemonToTrainerAsync_WithInvalidTrainerId_ShouldReturnNull()
    {
        // Arrange
        var invalidTrainerId = 999;
        var pokemonId = 3;

        // Act
        var result = await _service.AddExistingPokemonToTrainerAsync(invalidTrainerId, pokemonId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task AddExistingPokemonToTrainerAsync_WithInvalidPokemonId_ShouldReturnNull()
    {
        // Arrange
        var trainerId = 1;
        var invalidPokemonId = 999;

        // Act
        var result = await _service.AddExistingPokemonToTrainerAsync(trainerId, invalidPokemonId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllPokemonAsync_ShouldReturnAllPokemon()
    {
        // Act
        var result = await _service.GetAllPokemonAsync();

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().BeGreaterOrEqualTo(3); // From seeded data
        result.Should().Contain(p => p.Name == "Pikachu");
        result.Should().Contain(p => p.Name == "Squirtle");
        result.Should().Contain(p => p.Name == "Charmander");
    }

    [Fact]
    public async Task GetAllPokemonAsync_ShouldIncludePokemonDetails()
    {
        // Act
        var result = await _service.GetAllPokemonAsync();

        // Assert
        var pikachu = result.FirstOrDefault(p => p.Name == "Pikachu");
        pikachu.Should().NotBeNull();
        pikachu!.Type.Should().Be("Electric");
        pikachu.Owner.Should().Be("Ash Ketchum");
        pikachu.Level.Should().Be(25);
        pikachu.Health.Should().Be(100);
        pikachu.Moves.Should().NotBeEmpty();
        pikachu.Moves.Should().Contain(m => m.Name == "Thunderbolt");
    }

    [Fact]
    public async Task SearchPokemonAsync_WithNameSearch_ShouldReturnMatchingPokemon()
    {
        // Arrange
        var searchCriteria = new PokemonSearchDto
        {
            Name = "Pika"
        };

        // Act
        var result = await _service.SearchPokemonAsync(searchCriteria);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain(p => p.Name == "Pikachu");
        result.Should().NotContain(p => p.Name == "Squirtle");
    }

    [Fact]
    public async Task SearchPokemonAsync_WithTypeSearch_ShouldReturnMatchingPokemon()
    {
        // Arrange
        var searchCriteria = new PokemonSearchDto
        {
            Type = "Water"
        };

        // Act
        var result = await _service.SearchPokemonAsync(searchCriteria);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain(p => p.Name == "Squirtle");
        result.Should().NotContain(p => p.Name == "Pikachu");
    }

    [Fact]
    public async Task SearchPokemonAsync_WithLevelRange_ShouldReturnMatchingPokemon()
    {
        // Arrange
        var searchCriteria = new PokemonSearchDto
        {
            MinLevel = 20,
            MaxLevel = 30
        };

        // Act
        var result = await _service.SearchPokemonAsync(searchCriteria);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain(p => p.Name == "Pikachu" && p.Level == 25);
        result.Should().NotContain(p => p.Name == "Squirtle" && p.Level == 15);
    }

    [Fact]
    public async Task SearchPokemonAsync_WithOwnerSearch_ShouldReturnMatchingPokemon()
    {
        // Arrange
        var searchCriteria = new PokemonSearchDto
        {
            Owner = "Ash"
        };

        // Act
        var result = await _service.SearchPokemonAsync(searchCriteria);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain(p => p.Owner == "Ash Ketchum");
        result.Should().NotContain(p => p.Owner == "Misty");
    }

    [Fact]
    public async Task SearchPokemonAsync_WithHasOwnerTrue_ShouldReturnOnlyOwnedPokemon()
    {
        // Arrange
        var searchCriteria = new PokemonSearchDto
        {
            HasOwner = true
        };

        // Act
        var result = await _service.SearchPokemonAsync(searchCriteria);

        // Assert
        result.Should().NotBeNull();
        result.Should().AllSatisfy(p => p.Owner.Should().NotBeNull());
    }

    [Fact]
    public async Task SearchPokemonAsync_WithHasOwnerFalse_ShouldReturnOnlyWildPokemon()
    {
        // Arrange
        var searchCriteria = new PokemonSearchDto
        {
            HasOwner = false
        };

        // Act
        var result = await _service.SearchPokemonAsync(searchCriteria);

        // Assert
        result.Should().NotBeNull();
        result.Should().AllSatisfy(p => p.Owner.Should().BeNull());
        result.Should().Contain(p => p.Name == "Charmander");
    }

    [Fact]
    public async Task SearchPokemonAsync_WithMultipleCriteria_ShouldReturnMatchingPokemon()
    {
        // Arrange
        var searchCriteria = new PokemonSearchDto
        {
            Type = "Electric",
            MinLevel = 20,
            HasOwner = true
        };

        // Act
        var result = await _service.SearchPokemonAsync(searchCriteria);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain(p => p.Name == "Pikachu");
        result.Should().AllSatisfy(p =>
        {
            p.Type.Should().Be("Electric");
            p.Level.Should().BeGreaterOrEqualTo(20);
            p.Owner.Should().NotBeNull();
        });
    }

    [Fact]
    public async Task GetPaginatedPokemonAsync_WithBasicPagination_ShouldReturnPagedResults()
    {
        // Arrange
        var searchCriteria = new PaginatedPokemonSearchDto
        {
            PageNumber = 1,
            PageSize = 2
        };

        // Act
        var result = await _service.GetPaginatedPokemonAsync(searchCriteria);

        // Assert
        result.Should().NotBeNull();
        result.Data.Count.Should().Be(2);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(2);
        result.TotalItems.Should().BeGreaterOrEqualTo(3);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task GetPaginatedPokemonAsync_WithNameFilter_ShouldReturnFilteredResults()
    {
        // Arrange
        var searchCriteria = new PaginatedPokemonSearchDto
        {
            Name = "Pika",
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _service.GetPaginatedPokemonAsync(searchCriteria);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().Contain(p => p.Name.Contains("Pikachu"));
    }

    [Fact]
    public async Task GetPaginatedPokemonAsync_WithSortingByLevel_ShouldReturnSortedResults()
    {
        // Arrange
        var searchCriteria = new PaginatedPokemonSearchDto
        {
            SortBy = "level",
            SortOrder = "desc",
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _service.GetPaginatedPokemonAsync(searchCriteria);

        // Assert
        result.Should().NotBeNull();
        var levels = result.Data.Select(p => p.Level).ToList();
        levels.Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task GetPaginatedPokemonAsync_WithSortingByName_ShouldReturnSortedResults()
    {
        // Arrange
        var searchCriteria = new PaginatedPokemonSearchDto
        {
            SortBy = "name",
            SortOrder = "asc",
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _service.GetPaginatedPokemonAsync(searchCriteria);

        // Assert
        result.Should().NotBeNull();
        var names = result.Data.Select(p => p.Name).ToList();
        names.Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetPaginatedPokemonAsync_WithTypeFilter_ShouldReturnFilteredResults()
    {
        // Arrange
        var searchCriteria = new PaginatedPokemonSearchDto
        {
            Type = "Fire",
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _service.GetPaginatedPokemonAsync(searchCriteria);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().AllSatisfy(p => p.Type.Should().Contain("Fire"));
    }

    [Fact]
    public async Task GetPaginatedPokemonAsync_WithComplexFiltersAndSorting_ShouldWork()
    {
        // Arrange
        var searchCriteria = new PaginatedPokemonSearchDto
        {
            MinLevel = 10,
            MaxLevel = 30,
            HasOwner = true,
            SortBy = "level",
            SortOrder = "asc",
            PageNumber = 1,
            PageSize = 5
        };

        // Act
        var result = await _service.GetPaginatedPokemonAsync(searchCriteria);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().AllSatisfy(p =>
        {
            p.Level.Should().BeInRange(10, 30);
            p.Owner.Should().NotBeNull();
        });
        
        var levels = result.Data.Select(p => p.Level).ToList();
        levels.Should().BeInAscendingOrder();
    }
}
