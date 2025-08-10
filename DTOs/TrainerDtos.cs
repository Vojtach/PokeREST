using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PokeREST.DTOs;

public class CreateTrainerDto
{
    [Required]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 100 characters")]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(50)]
    [JsonPropertyName("region")]
    public string? Region { get; set; }

    [Range(1, 99, ErrorMessage = "Age must be between 1 and 99")]
    [JsonPropertyName("age")]
    public int? Age { get; set; }
}

public class UpdateTrainerDto
{
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 100 characters")]
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [StringLength(50)]
    [JsonPropertyName("region")]
    public string? Region { get; set; }

    [Range(1, 99, ErrorMessage = "Age must be between 1 and 99")]
    [JsonPropertyName("age")]
    public int? Age { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Wins must be a non-negative number")]
    [JsonPropertyName("wins")]
    public int? Wins { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Losses must be a non-negative number")]
    [JsonPropertyName("losses")]
    public int? Losses { get; set; }
}

public class TrainerResponseDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("region")]
    public string? Region { get; set; }

    [JsonPropertyName("age")]
    public int? Age { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime? CreatedAt { get; set; }

    [JsonPropertyName("wins")]
    public int? Wins { get; set; }

    [JsonPropertyName("losses")]
    public int? Losses { get; set; }

    [JsonPropertyName("pokemonCount")]
    public int PokemonCount { get; set; }
}

public class TrainerWithPokemonDto : TrainerResponseDto
{
    [JsonPropertyName("pokemons")]
    public List<PokemonResponseDto> Pokemons { get; set; } = new();
}

