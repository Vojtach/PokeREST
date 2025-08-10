using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PokeREST.DTOs;

public class AddExistingPokemonDto
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "PokemonId must be a positive number")]
    [JsonPropertyName("pokemonId")]
    public int PokemonId { get; set; }
}

public class PokemonResponseDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("level")]
    public int Level { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("owner")]
    public string? Owner { get; set; }

    [JsonPropertyName("health")]
    public int Health { get; set; }

    [JsonPropertyName("caughtAt")]
    public DateTime? CaughtAt { get; set; }

    [JsonPropertyName("moves")]
    public List<MoveResponseDto> Moves { get; set; } = new();
}

public class MoveResponseDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("power")]
    public int Power { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
}
