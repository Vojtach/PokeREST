using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using PokeREST.Controllers;
using PokeREST.DTOs;
using PokeREST.Services;
using Xunit;

namespace PokeREST.Tests.Controllers;

public class TrainersControllerTests
{
    private readonly Mock<ITrainerService> _mockTrainerService;
    private readonly TrainersController _controller;

    public TrainersControllerTests()
    {
        _mockTrainerService = new Mock<ITrainerService>();
        _controller = new TrainersController(_mockTrainerService.Object);
    }

    [Fact]
    public async Task CreateTrainer_WithValidData_ShouldReturnCreatedResult()
    {
        // Arrange
        var createDto = new CreateTrainerDto
        {
            Name = "Test Trainer",
            Region = "Kanto",
            Age = 20
        };

        var responseDto = new TrainerResponseDto
        {
            Id = 1,
            Name = "Test Trainer",
            Region = "Kanto",
            Age = 20,
            PokemonCount = 0,
            Wins = 0,
            Losses = 0,
            CreatedAt = DateTime.UtcNow
        };

        _mockTrainerService.Setup(s => s.CreateTrainerAsync(createDto))
            .ReturnsAsync(responseDto);

        // Act
        var result = await _controller.CreateTrainer(createDto);

        // Assert
        result.Should().NotBeNull();
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
        
        var response = createdResult.Value.Should().BeOfType<ApiResponse<TrainerResponseDto>>().Subject;
        response.Success.Should().BeTrue();
        response.StatusCode.Should().Be(201);
        response.Data.Should().BeEquivalentTo(responseDto);
        response.Message.Should().Be("Trainer created successfully");
    }

    [Fact]
    public async Task CreateTrainer_WithInvalidModelState_ShouldReturnBadRequest()
    {
        // Arrange
        var createDto = new CreateTrainerDto();
        _controller.ModelState.AddModelError("Name", "Name is required");

        // Act
        var result = await _controller.CreateTrainer(createDto);

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
    public async Task CreateTrainer_WhenServiceFails_ShouldReturnBadRequest()
    {
        // Arrange
        var createDto = new CreateTrainerDto { Name = "Test" };
        _mockTrainerService.Setup(s => s.CreateTrainerAsync(createDto))
            .ReturnsAsync((TrainerResponseDto?)null);

        // Act
        var result = await _controller.CreateTrainer(createDto);

        // Assert
        result.Should().NotBeNull();
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);

        var response = badRequestResult.Value.Should().BeOfType<ApiResponse<object>>().Subject;
        response.Success.Should().BeFalse();
        response.Message.Should().Be("Failed to create trainer");
    }

    [Fact]
    public async Task CreateTrainer_WhenExceptionOccurs_ShouldReturnInternalServerError()
    {
        // Arrange
        var createDto = new CreateTrainerDto { Name = "Test" };
        _mockTrainerService.Setup(s => s.CreateTrainerAsync(createDto))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.CreateTrainer(createDto);

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
    public async Task GetAllTrainers_ShouldReturnOkWithTrainersList()
    {
        // Arrange
        var trainers = new List<TrainerResponseDto>
        {
            new() { Id = 1, Name = "Ash", Region = "Kanto", Age = 10, PokemonCount = 2 },
            new() { Id = 2, Name = "Misty", Region = "Kanto", Age = 12, PokemonCount = 1 }
        };

        _mockTrainerService.Setup(s => s.GetAllTrainersAsync())
            .ReturnsAsync(trainers);

        // Act
        var result = await _controller.GetAllTrainers();

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var response = okResult.Value.Should().BeOfType<ApiResponse<List<TrainerResponseDto>>>().Subject;
        response.Success.Should().BeTrue();
        response.Data.Should().HaveCount(2);
        response.Data.Should().BeEquivalentTo(trainers);
        response.Message.Should().Be("Trainers retrieved successfully");
    }

    [Fact]
    public async Task GetAllTrainers_WhenExceptionOccurs_ShouldReturnInternalServerError()
    {
        // Arrange
        _mockTrainerService.Setup(s => s.GetAllTrainersAsync())
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetAllTrainers();

        // Assert
        result.Should().NotBeNull();
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task GetTrainerById_WithExistingId_ShouldReturnOkWithTrainer()
    {
        // Arrange
        const int trainerId = 1;
        var trainer = new TrainerWithPokemonDto
        {
            Id = trainerId,
            Name = "Ash",
            Region = "Kanto",
            Age = 10,
            PokemonCount = 1,
            Pokemons = new List<PokemonResponseDto>
            {
                new() { Id = 1, Name = "Pikachu", Level = 25, Type = "Electric" }
            }
        };

        _mockTrainerService.Setup(s => s.GetTrainerByIdAsync(trainerId))
            .ReturnsAsync(trainer);

        // Act
        var result = await _controller.GetTrainerById(trainerId);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var response = okResult.Value.Should().BeOfType<ApiResponse<TrainerWithPokemonDto>>().Subject;
        response.Success.Should().BeTrue();
        response.Data.Should().BeEquivalentTo(trainer);
        response.Message.Should().Be("Trainer found successfully");
    }

    [Fact]
    public async Task GetTrainerById_WithNonExistingId_ShouldReturnNotFound()
    {
        // Arrange
        const int trainerId = 999;
        _mockTrainerService.Setup(s => s.GetTrainerByIdAsync(trainerId))
            .ReturnsAsync((TrainerWithPokemonDto?)null);

        // Act
        var result = await _controller.GetTrainerById(trainerId);

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
    public async Task UpdateTrainer_WithValidData_ShouldReturnOkWithUpdatedTrainer()
    {
        // Arrange
        const int trainerId = 1;
        var updateDto = new UpdateTrainerDto
        {
            Name = "Updated Name",
            Wins = 10
        };

        var updatedTrainer = new TrainerResponseDto
        {
            Id = trainerId,
            Name = "Updated Name",
            Region = "Kanto",
            Age = 10,
            Wins = 10,
            Losses = 5,
            PokemonCount = 2
        };

        _mockTrainerService.Setup(s => s.UpdateTrainerAsync(trainerId, updateDto))
            .ReturnsAsync(updatedTrainer);

        // Act
        var result = await _controller.UpdateTrainer(trainerId, updateDto);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var response = okResult.Value.Should().BeOfType<ApiResponse<TrainerResponseDto>>().Subject;
        response.Success.Should().BeTrue();
        response.Data.Should().BeEquivalentTo(updatedTrainer);
        response.Message.Should().Be("Trainer updated successfully");
    }

    [Fact]
    public async Task UpdateTrainer_WithInvalidModelState_ShouldReturnBadRequest()
    {
        // Arrange
        const int trainerId = 1;
        var updateDto = new UpdateTrainerDto();
        _controller.ModelState.AddModelError("Age", "Invalid age");

        // Act
        var result = await _controller.UpdateTrainer(trainerId, updateDto);

        // Assert
        result.Should().NotBeNull();
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task UpdateTrainer_WithNonExistingId_ShouldReturnNotFound()
    {
        // Arrange
        const int trainerId = 999;
        var updateDto = new UpdateTrainerDto { Name = "Test" };
        
        _mockTrainerService.Setup(s => s.UpdateTrainerAsync(trainerId, updateDto))
            .ReturnsAsync((TrainerResponseDto?)null);

        // Act
        var result = await _controller.UpdateTrainer(trainerId, updateDto);

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
    public async Task DeleteTrainer_WithExistingId_ShouldReturnOkWithSuccessMessage()
    {
        // Arrange
        const int trainerId = 1;
        _mockTrainerService.Setup(s => s.DeleteTrainerAsync(trainerId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteTrainer(trainerId);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var response = okResult.Value.Should().BeOfType<ApiResponse<object>>().Subject;
        response.Success.Should().BeTrue();
        response.Data.Should().BeNull();
        response.Message.Should().Be("Trainer deleted successfully");
    }

    [Fact]
    public async Task DeleteTrainer_WithNonExistingId_ShouldReturnNotFound()
    {
        // Arrange
        const int trainerId = 999;
        _mockTrainerService.Setup(s => s.DeleteTrainerAsync(trainerId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteTrainer(trainerId);

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
    public async Task GetPaginatedTrainers_WithValidCriteria_ShouldReturnOkWithPaginatedResults()
    {
        // Arrange
        var trainers = new List<TrainerResponseDto>
        {
            new() { Id = 1, Name = "Ash", Age = 10 }
        };

        var paginatedResponse = new PaginatedResponse<TrainerResponseDto>
        {
            Data = trainers,
            TotalItems = 1,
            PageNumber = 1,
            PageSize = 10,
            TotalPages = 1,
            HasPreviousPage = false,
            HasNextPage = false
        };

        _mockTrainerService.Setup(s => s.GetPaginatedTrainersAsync(It.IsAny<PaginatedTrainerSearchDto>()))
            .ReturnsAsync(paginatedResponse);

        // Act
        var result = await _controller.GetPaginatedTrainers();

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var response = okResult.Value.Should().BeOfType<ApiResponse<PaginatedResponse<TrainerResponseDto>>>().Subject;
        response.Success.Should().BeTrue();
        response.Data.Should().BeEquivalentTo(paginatedResponse);
        response.Message.Should().Contain("Found 1 trainers");
    }

    [Fact]
    public async Task GetPaginatedTrainers_WithNoResults_ShouldReturnOkWithEmptyResults()
    {
        // Arrange
        var paginatedResponse = new PaginatedResponse<TrainerResponseDto>
        {
            Data = new List<TrainerResponseDto>(),
            TotalItems = 0,
            PageNumber = 1,
            PageSize = 10,
            TotalPages = 0,
            HasPreviousPage = false,
            HasNextPage = false
        };

        _mockTrainerService.Setup(s => s.GetPaginatedTrainersAsync(It.IsAny<PaginatedTrainerSearchDto>()))
            .ReturnsAsync(paginatedResponse);

        // Act
        var result = await _controller.GetPaginatedTrainers();

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var response = okResult.Value.Should().BeOfType<ApiResponse<PaginatedResponse<TrainerResponseDto>>>().Subject;
        response.Success.Should().BeTrue();
        response.Data.Should().BeEquivalentTo(paginatedResponse);
        response.Message.Should().Be("No trainers found matching search criteria");
    }

    [Fact]
    public async Task GetPaginatedTrainers_WithInvalidCriteria_ShouldReturnBadRequest()
    {
        // Arrange
        var searchCriteria = new PaginatedTrainerSearchDto
        {
            MinAge = 20,
            MaxAge = 10 // Invalid: min > max
        };

        // We need to simulate the IsValid() method returning false
        // Since we can't mock static methods easily, we'll test the controller's validation logic
        
        // Act
        var result = await _controller.GetPaginatedTrainers(
            minAge: searchCriteria.MinAge,
            maxAge: searchCriteria.MaxAge);

        // Assert
        result.Should().NotBeNull();
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);

        var response = badRequestResult.Value.Should().BeOfType<ApiResponse<object>>().Subject;
        response.Success.Should().BeFalse();
        response.StatusCode.Should().Be(400);
        response.Message.Should().Contain("Invalid search criteria");
    }
}
