using FluentAssertions;
using PokeREST.DTOs;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace PokeREST.Tests.DTOs;

public class DtoValidationTests
{
    private static List<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(model, null, null);
        Validator.TryValidateObject(model, validationContext, validationResults, true);
        return validationResults;
    }

    #region CreateTrainerDto Tests

    [Fact]
    public void CreateTrainerDto_WithValidData_ShouldPassValidation()
    {
        // Arrange
        var dto = new CreateTrainerDto
        {
            Name = "Ash Ketchum",
            Region = "Kanto",
            Age = 10
        };

        // Act
        var validationResults = ValidateModel(dto);

        // Assert
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void CreateTrainerDto_WithEmptyName_ShouldFailValidation()
    {
        // Arrange
        var dto = new CreateTrainerDto
        {
            Name = "",
            Region = "Kanto",
            Age = 10
        };

        // Act
        var validationResults = ValidateModel(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        // Check for either Required message or StringLength message, depending on validation order
        validationResults.Should().Contain(vr => 
            vr.ErrorMessage!.Contains("Name must be between 1 and 100 characters") ||
            vr.ErrorMessage!.Contains("Name") && vr.MemberNames.Contains("Name"));
    }

    [Fact]
    public void CreateTrainerDto_WithNullName_ShouldFailValidation()
    {
        // Arrange
        var dto = new CreateTrainerDto
        {
            Name = null!,
            Region = "Kanto",
            Age = 10
        };

        // Act
        var validationResults = ValidateModel(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(vr => vr.MemberNames.Contains("Name"));
    }

    [Fact]
    public void CreateTrainerDto_WithTooLongName_ShouldFailValidation()
    {
        // Arrange
        var dto = new CreateTrainerDto
        {
            Name = new string('A', 101), // 101 characters
            Region = "Kanto",
            Age = 10
        };

        // Act
        var validationResults = ValidateModel(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(vr => vr.ErrorMessage!.Contains("Name must be between 1 and 100 characters"));
    }

    [Fact]
    public void CreateTrainerDto_WithInvalidAge_ShouldFailValidation()
    {
        // Arrange
        var dto = new CreateTrainerDto
        {
            Name = "Test Trainer",
            Region = "Kanto",
            Age = 0 // Invalid age
        };

        // Act
        var validationResults = ValidateModel(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(vr => vr.ErrorMessage!.Contains("Age must be between 1 and 99"));
    }

    [Fact]
    public void CreateTrainerDto_WithTooHighAge_ShouldFailValidation()
    {
        // Arrange
        var dto = new CreateTrainerDto
        {
            Name = "Test Trainer",
            Region = "Kanto",
            Age = 100 // Invalid age
        };

        // Act
        var validationResults = ValidateModel(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(vr => vr.ErrorMessage!.Contains("Age must be between 1 and 99"));
    }

    [Fact]
    public void CreateTrainerDto_WithTooLongRegion_ShouldFailValidation()
    {
        // Arrange
        var dto = new CreateTrainerDto
        {
            Name = "Test Trainer",
            Region = new string('A', 51), // 51 characters
            Age = 20
        };

        // Act
        var validationResults = ValidateModel(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(vr => vr.MemberNames.Contains("Region"));
    }

    #endregion

    #region UpdateTrainerDto Tests

    [Fact]
    public void UpdateTrainerDto_WithValidData_ShouldPassValidation()
    {
        // Arrange
        var dto = new UpdateTrainerDto
        {
            Name = "Updated Name",
            Region = "Johto",
            Age = 15,
            Wins = 10,
            Losses = 5
        };

        // Act
        var validationResults = ValidateModel(dto);

        // Assert
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void UpdateTrainerDto_WithNegativeWins_ShouldFailValidation()
    {
        // Arrange
        var dto = new UpdateTrainerDto
        {
            Name = "Test",
            Wins = -1
        };

        // Act
        var validationResults = ValidateModel(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(vr => vr.ErrorMessage!.Contains("Wins must be a non-negative number"));
    }

    [Fact]
    public void UpdateTrainerDto_WithNegativeLosses_ShouldFailValidation()
    {
        // Arrange
        var dto = new UpdateTrainerDto
        {
            Name = "Test",
            Losses = -1
        };

        // Act
        var validationResults = ValidateModel(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(vr => vr.ErrorMessage!.Contains("Losses must be a non-negative number"));
    }

    #endregion

    #region AddExistingPokemonDto Tests

    [Fact]
    public void AddExistingPokemonDto_WithValidData_ShouldPassValidation()
    {
        // Arrange
        var dto = new AddExistingPokemonDto
        {
            PokemonId = 1
        };

        // Act
        var validationResults = ValidateModel(dto);

        // Assert
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void AddExistingPokemonDto_WithZeroPokemonId_ShouldFailValidation()
    {
        // Arrange
        var dto = new AddExistingPokemonDto
        {
            PokemonId = 0
        };

        // Act
        var validationResults = ValidateModel(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(vr => vr.ErrorMessage!.Contains("PokemonId must be a positive number"));
    }

    [Fact]
    public void AddExistingPokemonDto_WithNegativePokemonId_ShouldFailValidation()
    {
        // Arrange
        var dto = new AddExistingPokemonDto
        {
            PokemonId = -1
        };

        // Act
        var validationResults = ValidateModel(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(vr => vr.ErrorMessage!.Contains("PokemonId must be a positive number"));
    }

    #endregion

    #region PokemonSearchDto Tests

    [Fact]
    public void PokemonSearchDto_WithValidLevelRange_ShouldBeValid()
    {
        // Arrange
        var dto = new PokemonSearchDto
        {
            MinLevel = 10,
            MaxLevel = 50
        };

        // Act
        var isValid = dto.IsValid();

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void PokemonSearchDto_WithInvalidLevelRange_ShouldBeInvalid()
    {
        // Arrange
        var dto = new PokemonSearchDto
        {
            MinLevel = 50,
            MaxLevel = 10 // Min > Max
        };

        // Act
        var isValid = dto.IsValid();

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void PokemonSearchDto_WithNullLevels_ShouldBeValid()
    {
        // Arrange
        var dto = new PokemonSearchDto
        {
            MinLevel = null,
            MaxLevel = null
        };

        // Act
        var isValid = dto.IsValid();

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void PokemonSearchDto_WithOnlyMinLevel_ShouldBeValid()
    {
        // Arrange
        var dto = new PokemonSearchDto
        {
            MinLevel = 10,
            MaxLevel = null
        };

        // Act
        var isValid = dto.IsValid();

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void PokemonSearchDto_WithInvalidMinLevel_ShouldFailValidation()
    {
        // Arrange
        var dto = new PokemonSearchDto
        {
            MinLevel = 0
        };

        // Act
        var validationResults = ValidateModel(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(vr => vr.ErrorMessage!.Contains("MinLevel must be between 1 and 100"));
    }

    [Fact]
    public void PokemonSearchDto_WithTooHighMaxLevel_ShouldFailValidation()
    {
        // Arrange
        var dto = new PokemonSearchDto
        {
            MaxLevel = 101
        };

        // Act
        var validationResults = ValidateModel(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(vr => vr.ErrorMessage!.Contains("MaxLevel must be between 1 and 100"));
    }

    #endregion

    #region PaginatedPokemonSearchDto Tests

    [Fact]
    public void PaginatedPokemonSearchDto_WithValidData_ShouldBeValid()
    {
        // Arrange
        var dto = new PaginatedPokemonSearchDto
        {
            MinLevel = 10,
            MaxLevel = 50,
            PageNumber = 1,
            PageSize = 10,
            SortBy = "name",
            SortOrder = "asc"
        };

        // Act
        var isValid = dto.IsValid();

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void PaginatedPokemonSearchDto_WithInvalidSortBy_ShouldBeInvalid()
    {
        // Arrange
        var dto = new PaginatedPokemonSearchDto
        {
            SortBy = "invalid_field"
        };

        // Act
        var isValid = dto.IsValid();

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void PaginatedPokemonSearchDto_WithInvalidSortOrder_ShouldBeInvalid()
    {
        // Arrange
        var dto = new PaginatedPokemonSearchDto
        {
            SortOrder = "invalid_order"
        };

        // Act
        var isValid = dto.IsValid();

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void PaginatedPokemonSearchDto_WithInvalidPageNumber_ShouldFailValidation()
    {
        // Arrange
        var dto = new PaginatedPokemonSearchDto
        {
            PageNumber = 0
        };

        // Act
        var validationResults = ValidateModel(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(vr => vr.ErrorMessage!.Contains("Page number must be greater than 0"));
    }

    [Fact]
    public void PaginatedPokemonSearchDto_WithInvalidPageSize_ShouldFailValidation()
    {
        // Arrange
        var dto = new PaginatedPokemonSearchDto
        {
            PageSize = 0
        };

        // Act
        var validationResults = ValidateModel(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(vr => vr.ErrorMessage!.Contains("Page size must be between 1 and 100"));
    }

    [Fact]
    public void PaginatedPokemonSearchDto_WithTooLargePageSize_ShouldFailValidation()
    {
        // Arrange
        var dto = new PaginatedPokemonSearchDto
        {
            PageSize = 101
        };

        // Act
        var validationResults = ValidateModel(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(vr => vr.ErrorMessage!.Contains("Page size must be between 1 and 100"));
    }

    #endregion

    #region PaginatedTrainerSearchDto Tests

    [Fact]
    public void PaginatedTrainerSearchDto_WithValidData_ShouldBeValid()
    {
        // Arrange
        var dto = new PaginatedTrainerSearchDto
        {
            MinAge = 10,
            MaxAge = 50,
            MinWins = 0,
            MaxWins = 100,
            PageNumber = 1,
            PageSize = 10,
            SortBy = "name",
            SortOrder = "asc"
        };

        // Act
        var isValid = dto.IsValid();

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void PaginatedTrainerSearchDto_WithInvalidAgeRange_ShouldBeInvalid()
    {
        // Arrange
        var dto = new PaginatedTrainerSearchDto
        {
            MinAge = 50,
            MaxAge = 10 // Min > Max
        };

        // Act
        var isValid = dto.IsValid();

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void PaginatedTrainerSearchDto_WithInvalidWinsRange_ShouldBeInvalid()
    {
        // Arrange
        var dto = new PaginatedTrainerSearchDto
        {
            MinWins = 50,
            MaxWins = 10 // Min > Max
        };

        // Act
        var isValid = dto.IsValid();

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void PaginatedTrainerSearchDto_WithInvalidSortBy_ShouldBeInvalid()
    {
        // Arrange
        var dto = new PaginatedTrainerSearchDto
        {
            SortBy = "invalid_field"
        };

        // Act
        var isValid = dto.IsValid();

        // Assert
        isValid.Should().BeFalse();
    }

    #endregion

    #region ApiResponse Tests

    [Fact]
    public void ApiResponse_SuccessResponse_ShouldHaveCorrectProperties()
    {
        // Arrange
        var data = "test data";
        const string message = "Success";
        const int statusCode = 200;

        // Act
        var response = ApiResponse<string>.SuccessResponse(data, message, statusCode);

        // Assert
        response.Success.Should().BeTrue();
        response.StatusCode.Should().Be(statusCode);
        response.Message.Should().Be(message);
        response.Data.Should().Be(data);
    }

    [Fact]
    public void ApiResponse_ErrorResponse_ShouldHaveCorrectProperties()
    {
        // Arrange
        const string message = "Error occurred";
        const int statusCode = 400;

        // Act
        var response = ApiResponse<object>.ErrorResponse(message, statusCode);

        // Assert
        response.Success.Should().BeFalse();
        response.StatusCode.Should().Be(statusCode);
        response.Message.Should().Be(message);
        response.Data.Should().BeNull();
    }

    [Fact]
    public void ApiResponse_SuccessResponseWithDefaults_ShouldHaveDefaultValues()
    {
        // Arrange
        var data = "test data";

        // Act
        var response = ApiResponse<string>.SuccessResponse(data);

        // Assert
        response.Success.Should().BeTrue();
        response.StatusCode.Should().Be(200);
        response.Message.Should().Be("Operation completed successfully");
        response.Data.Should().Be(data);
    }

    [Fact]
    public void ApiResponse_ErrorResponseWithDefaults_ShouldHaveDefaultValues()
    {
        // Arrange
        const string message = "Error occurred";

        // Act
        var response = ApiResponse<object>.ErrorResponse(message);

        // Assert
        response.Success.Should().BeFalse();
        response.StatusCode.Should().Be(400);
        response.Message.Should().Be(message);
        response.Data.Should().BeNull();
    }

    #endregion
}
