using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PokeREST.DTOs
{
    public class CreatePokemonDto
    {
        [Required]
        [StringLength(100, MinimumLength = 1)]
        [RegularExpression(@"^[a-zA-Z0-9\s\-\.]+$", ErrorMessage = "Pokemon name contains invalid characters")]
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [Range(1, 100, ErrorMessage = "Level must be between 1 and 100")]
        [JsonPropertyName("level")]
        public int Level { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Valid type ID is required")]
        [JsonPropertyName("typeId")]
        public int TypeId { get; set; }

        [Range(1, 9999, ErrorMessage = "Health must be between 1 and 9999")]
        [JsonPropertyName("health")]
        public int Health { get; set; }
    }

    public class PokemonDto
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
    }
}
