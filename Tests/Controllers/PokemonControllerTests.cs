using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using PokeREST.Controllers;
using PokeREST.DTOs;
using PokeREST.Services;
using Xunit;

namespace PokeREST.Tests.Controllers;

public class PokemonControllerTests
{
    private readonly Mock<IPokemonService> _mockPokemonService;
    private readonly Mock<ITrainerService> _mockTrainerService;
    private readonly PokemonController _controller;

    public PokemonControllerTests()
    {
        _mockPokemonService = new Mock<IPokemonService>();
        _mockTrainerService = new Mock<ITrainerService>();
        _controller = new PokemonController(_mockPokemonService.Object, _mockTrainerService.Object);
    }

    [Fact]
    public async Task AddPokemonToTrainer_WithValidData_ShouldReturnOkResult()
    {
        // Arrange
        const int trainerId = 1;
        var addPokemonDto = new AddExistingPokemonDto { PokemonId = 1 };
        var pokemonResponse = new PokemonResponseDto
        {
            Id = 1,
            Name = "Pikachu",
            Level = 25,
            Type = "Electric",
            Owner = "Ash Ketchum",
            Health = 100,
            CaughtAt = DateTime.UtcNow
        };

        _mockTrainerService.Setup(s => s.TrainerExistsAsync(trainerId))
            .ReturnsAsync(true);

        _mockPokemonService.Setup(s => s.AddExistingPokemonToTrainerAsync(trainerId, addPokemonDto.PokemonId))
            .ReturnsAsync(pokemonResponse);

        // Act
        var result = await _controller.AddPokemonToTrainer(trainerId, addPokemonDto);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var response = okResult.Value.Should().BeOfType<ApiResponse<PokemonResponseDto>>().Subject;
        response.Success.Should().BeTrue();
        response.StatusCode.Should().Be(200);
        response.Data.Should().BeEquivalentTo(pokemonResponse);
        response.Message.Should().Be("Pokemon added to trainer successfully");
    }

    [Fact]
    public async Task AddPokemonToTrainer_WithInvalidModelState_ShouldReturnBadRequest()
    {
        // Arrange
        const int trainerId = 1;
        var addPokemonDto = new AddExistingPokemonDto();
        _controller.ModelState.AddModelError("PokemonId", "PokemonId is required");

        // Act
        var result = await _controller.AddPokemonToTrainer(trainerId, addPokemonDto);

        // Assert
        result.Should().NotBeNull();
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);

        var response = badRequestResult.Value.Should().BeOfType<ApiResponse<object>>().Subject;
        response.Success.Should().BeFalse();
        response.StatusCode.Should().Be(400);
        response.Message.Should().Be("Invalid input data");
    }

    [Fact]
    public async Task AddPokemonToTrainer_WithNonExistentTrainer_ShouldReturnNotFound()
    {
        // Arrange
        const int trainerId = 999;
        var addPokemonDto = new AddExistingPokemonDto { PokemonId = 1 };

        _mockTrainerService.Setup(s => s.TrainerExistsAsync(trainerId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.AddPokemonToTrainer(trainerId, addPokemonDto);

        // Assert
        result.Should().NotBeNull();
        var notFoundResult = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.StatusCode.Should().Be(404);

        var response = notFoundResult.Value.Should().BeOfType<ApiResponse<object>>().Subject;
        response.Success.Should().BeFalse();
        response.StatusCode.Should().Be(404);
        response.Message.Should().Be("Trainer not found");
    }

    [Fact]
    public async Task AddPokemonToTrainer_WithInvalidPokemonId_ShouldReturnBadRequest()
    {
        // Arrange
        const int trainerId = 1;
        var addPokemonDto = new AddExistingPokemonDto { PokemonId = 999 };

        _mockTrainerService.Setup(s => s.TrainerExistsAsync(trainerId))
            .ReturnsAsync(true);

        _mockPokemonService.Setup(s => s.AddExistingPokemonToTrainerAsync(trainerId, addPokemonDto.PokemonId))
            .ReturnsAsync((PokemonResponseDto?)null);

        // Act
        var result = await _controller.AddPokemonToTrainer(trainerId, addPokemonDto);

        // Assert
        result.Should().NotBeNull();
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);

        var response = badRequestResult.Value.Should().BeOfType<ApiResponse<object>>().Subject;
        response.Success.Should().BeFalse();
        response.StatusCode.Should().Be(400);
        response.Message.Should().Be("Failed to add Pokemon to trainer. Please check if the Pokemon ID is valid.");
    }

    [Fact]
    public async Task AddPokemonToTrainer_WhenExceptionOccurs_ShouldReturnInternalServerError()
    {
        // Arrange
        const int trainerId = 1;
        var addPokemonDto = new AddExistingPokemonDto { PokemonId = 1 };

        _mockTrainerService.Setup(s => s.TrainerExistsAsync(trainerId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.AddPokemonToTrainer(trainerId, addPokemonDto);

        // Assert
        result.Should().NotBeNull();
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);

        var response = statusResult.Value.Should().BeOfType<ApiResponse<object>>().Subject;
        response.Success.Should().BeFalse();
        response.StatusCode.Should().Be(500);
        response.Message.Should().Be("Internal server error");
    }

    [Fact]
    public async Task GetAllPokemon_WithoutSearchCriteria_ShouldReturnAllPokemon()
    {
        // Arrange
        var pokemon = new List<PokemonResponseDto>
        {
            new() { Id = 1, Name = "Pikachu", Level = 25, Type = "Electric", Owner = "Ash" },
            new() { Id = 2, Name = "Squirtle", Level = 15, Type = "Water", Owner = "Misty" }
        };

        _mockPokemonService.Setup(s => s.GetAllPokemonAsync())
            .ReturnsAsync(pokemon);

        // Act
        var result = await _controller.GetAllPokemon();

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var response = okResult.Value.Should().BeOfType<ApiResponse<List<PokemonResponseDto>>>().Subject;
        response.Success.Should().BeTrue();
        response.Data.Should().HaveCount(2);
        response.Data.Should().BeEquivalentTo(pokemon);
        response.Message.Should().Be("Pokemon retrieved successfully");
    }

    [Fact]
    public async Task GetAllPokemon_WithSearchCriteria_ShouldReturnFilteredPokemon()
    {
        // Arrange
        var searchResults = new List<PokemonResponseDto>
        {
            new() { Id = 1, Name = "Pikachu", Level = 25, Type = "Electric", Owner = "Ash" }
        };

        _mockPokemonService.Setup(s => s.SearchPokemonAsync(It.IsAny<PokemonSearchDto>()))
            .ReturnsAsync(searchResults);

        // Act
        var result = await _controller.GetAllPokemon(name: "Pika");

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var response = okResult.Value.Should().BeOfType<ApiResponse<List<PokemonResponseDto>>>().Subject;
        response.Success.Should().BeTrue();
        response.Data.Should().HaveCount(1);
        response.Data.Should().BeEquivalentTo(searchResults);
        response.Message.Should().Be("Found 1 Pokemon matching search criteria");
    }

    [Fact]
    public async Task GetAllPokemon_WithSearchCriteriaNoResults_ShouldReturnEmptyList()
    {
        // Arrange
        var searchResults = new List<PokemonResponseDto>();

        _mockPokemonService.Setup(s => s.SearchPokemonAsync(It.IsAny<PokemonSearchDto>()))
            .ReturnsAsync(searchResults);

        // Act
        var result = await _controller.GetAllPokemon(name: "NonExistent");

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var response = okResult.Value.Should().BeOfType<ApiResponse<List<PokemonResponseDto>>>().Subject;
        response.Success.Should().BeTrue();
        response.Data.Should().BeEmpty();
        response.Message.Should().Be("No Pokemon found matching search criteria");
    }

    [Fact]
    public async Task GetAllPokemon_WithInvalidSearchCriteria_ShouldReturnBadRequest()
    {
        // Act - Using invalid min/max level range
        var result = await _controller.GetAllPokemon(minLevel: 50, maxLevel: 20);

        // Assert
        result.Should().NotBeNull();
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);

        var response = badRequestResult.Value.Should().BeOfType<ApiResponse<object>>().Subject;
        response.Success.Should().BeFalse();
        response.StatusCode.Should().Be(400);
        response.Message.Should().Be("Invalid search criteria: MinLevel must be less than or equal to MaxLevel");
    }

    [Fact]
    public async Task GetAllPokemon_WhenExceptionOccurs_ShouldReturnInternalServerError()
    {
        // Arrange
        _mockPokemonService.Setup(s => s.GetAllPokemonAsync())
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetAllPokemon();

        // Assert
        result.Should().NotBeNull();
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);

        var response = statusResult.Value.Should().BeOfType<ApiResponse<object>>().Subject;
        response.Success.Should().BeFalse();
        response.StatusCode.Should().Be(500);
        response.Message.Should().Be("Internal server error");
    }

    [Fact]
    public async Task GetPaginatedPokemon_WithValidCriteria_ShouldReturnPagedResults()
    {
        // Arrange
        var pokemon = new List<PokemonResponseDto>
        {
            new() { Id = 1, Name = "Pikachu", Level = 25, Type = "Electric" }
        };

        var paginatedResponse = new PaginatedResponse<PokemonResponseDto>
        {
            Data = pokemon,
            TotalItems = 1,
            PageNumber = 1,
            PageSize = 10,
            TotalPages = 1,
            HasPreviousPage = false,
            HasNextPage = false
        };

        _mockPokemonService.Setup(s => s.GetPaginatedPokemonAsync(It.IsAny<PaginatedPokemonSearchDto>()))
            .ReturnsAsync(paginatedResponse);

        // Act
        var result = await _controller.GetPaginatedPokemon();

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var response = okResult.Value.Should().BeOfType<ApiResponse<PaginatedResponse<PokemonResponseDto>>>().Subject;
        response.Success.Should().BeTrue();
        response.Data.Should().BeEquivalentTo(paginatedResponse);
        response.Message.Should().Be("Found 1 Pokemon (Page 1 of 1)");
    }

    [Fact]
    public async Task GetPaginatedPokemon_WithFilters_ShouldReturnFilteredResults()
    {
        // Arrange
        var pokemon = new List<PokemonResponseDto>
        {
            new() { Id = 1, Name = "Pikachu", Level = 25, Type = "Electric", Owner = "Ash" }
        };

        var paginatedResponse = new PaginatedResponse<PokemonResponseDto>
        {
            Data = pokemon,
            TotalItems = 1,
            PageNumber = 1,
            PageSize = 10,
            TotalPages = 1,
            HasPreviousPage = false,
            HasNextPage = false
        };

        _mockPokemonService.Setup(s => s.GetPaginatedPokemonAsync(It.IsAny<PaginatedPokemonSearchDto>()))
            .ReturnsAsync(paginatedResponse);

        // Act
        var result = await _controller.GetPaginatedPokemon(
            name: "Pikachu",
            type: "Electric",
            minLevel: 20,
            maxLevel: 30,
            hasOwner: true);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var response = okResult.Value.Should().BeOfType<ApiResponse<PaginatedResponse<PokemonResponseDto>>>().Subject;
        response.Success.Should().BeTrue();
        response.Data.Should().BeEquivalentTo(paginatedResponse);

        // Verify that the service was called with the correct search criteria
        _mockPokemonService.Verify(s => s.GetPaginatedPokemonAsync(It.Is<PaginatedPokemonSearchDto>(dto =>
            dto.Name == "Pikachu" &&
            dto.Type == "Electric" &&
            dto.MinLevel == 20 &&
            dto.MaxLevel == 30 &&
            dto.HasOwner == true)), Times.Once);
    }

    [Fact]
    public async Task GetPaginatedPokemon_WithSortingAndPagination_ShouldReturnSortedResults()
    {
        // Arrange
        var pokemon = new List<PokemonResponseDto>
        {
            new() { Id = 1, Name = "Charmander", Level = 12, Type = "Fire" },
            new() { Id = 2, Name = "Squirtle", Level = 15, Type = "Water" }
        };

        var paginatedResponse = new PaginatedResponse<PokemonResponseDto>
        {
            Data = pokemon,
            TotalItems = 5,
            PageNumber = 2,
            PageSize = 2,
            TotalPages = 3,
            HasPreviousPage = true,
            HasNextPage = true
        };

        _mockPokemonService.Setup(s => s.GetPaginatedPokemonAsync(It.IsAny<PaginatedPokemonSearchDto>()))
            .ReturnsAsync(paginatedResponse);

        // Act
        var result = await _controller.GetPaginatedPokemon(
            pageNumber: 2,
            pageSize: 2,
            sortBy: "level",
            sortOrder: "asc");

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var response = okResult.Value.Should().BeOfType<ApiResponse<PaginatedResponse<PokemonResponseDto>>>().Subject;
        response.Success.Should().BeTrue();
        response.Data.Should().BeEquivalentTo(paginatedResponse);
        response.Message.Should().Be("Found 5 Pokemon (Page 2 of 3)");

        // Verify that the service was called with the correct pagination criteria
        _mockPokemonService.Verify(s => s.GetPaginatedPokemonAsync(It.Is<PaginatedPokemonSearchDto>(dto =>
            dto.PageNumber == 2 &&
            dto.PageSize == 2 &&
            dto.SortBy == "level" &&
            dto.SortOrder == "asc")), Times.Once);
    }

    [Fact]
    public async Task GetPaginatedPokemon_WithNoResults_ShouldReturnEmptyResults()
    {
        // Arrange
        var paginatedResponse = new PaginatedResponse<PokemonResponseDto>
        {
            Data = new List<PokemonResponseDto>(),
            TotalItems = 0,
            PageNumber = 1,
            PageSize = 10,
            TotalPages = 0,
            HasPreviousPage = false,
            HasNextPage = false
        };

        _mockPokemonService.Setup(s => s.GetPaginatedPokemonAsync(It.IsAny<PaginatedPokemonSearchDto>()))
            .ReturnsAsync(paginatedResponse);

        // Act
        var result = await _controller.GetPaginatedPokemon();

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var response = okResult.Value.Should().BeOfType<ApiResponse<PaginatedResponse<PokemonResponseDto>>>().Subject;
        response.Success.Should().BeTrue();
        response.Data.Should().BeEquivalentTo(paginatedResponse);
        response.Message.Should().Be("No Pokemon found matching search criteria");
    }

    [Fact]
    public async Task GetPaginatedPokemon_WithInvalidSearchCriteria_ShouldReturnBadRequest()
    {
        // Act - Using invalid search criteria (min level > max level)
        var result = await _controller.GetPaginatedPokemon(
            minLevel: 50,
            maxLevel: 20,
            sortBy: "invalid_field");

        // Assert
        result.Should().NotBeNull();
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);

        var response = badRequestResult.Value.Should().BeOfType<ApiResponse<object>>().Subject;
        response.Success.Should().BeFalse();
        response.StatusCode.Should().Be(400);
        response.Message.Should().Contain("Invalid search criteria");
    }

    [Fact]
    public async Task GetPaginatedPokemon_WhenExceptionOccurs_ShouldReturnInternalServerError()
    {
        // Arrange
        _mockPokemonService.Setup(s => s.GetPaginatedPokemonAsync(It.IsAny<PaginatedPokemonSearchDto>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetPaginatedPokemon();

        // Assert
        result.Should().NotBeNull();
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);

        var response = statusResult.Value.Should().BeOfType<ApiResponse<object>>().Subject;
        response.Success.Should().BeFalse();
        response.StatusCode.Should().Be(500);
        response.Message.Should().Be("Internal server error");
    }

    [Fact]
    public async Task AddPokemonToTrainer_WithNullBody_ShouldReturnBadRequest()
    {
        var result = await _controller.AddPokemonToTrainer(1, null!);

        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);
        var response = badRequestResult.Value.Should().BeOfType<ApiResponse<object>>().Subject;
        response.Success.Should().BeFalse();
        response.StatusCode.Should().Be(400);
        response.Message.Should().Be("Invalid input data");
        _mockTrainerService.Verify(s => s.TrainerExistsAsync(It.IsAny<int>()), Times.Never);
        _mockPokemonService.Verify(
            s => s.AddExistingPokemonToTrainerAsync(It.IsAny<int>(), It.IsAny<int>()),
            Times.Never);
    }
}
