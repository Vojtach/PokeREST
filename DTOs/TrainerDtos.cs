using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PokeREST.DTOs
{
    public class CreateTrainerDto
    {
        [Required]
        [StringLength(100, MinimumLength = 1)]
        [RegularExpression(@"^[a-zA-Z0-9\s\-\.]+$", ErrorMessage = "Name contains invalid characters")]
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(50)]
        [RegularExpression(@"^[a-zA-Z0-9\s\-]+$", ErrorMessage = "Region contains invalid characters")]
        [JsonPropertyName("region")]
        public string? Region { get; set; }

        [Range(1, 150, ErrorMessage = "Age must be between 1 and 150")]
        [JsonPropertyName("age")]
        public int? Age { get; set; }
    }

    public class UpdateTrainerDto
    {
        [StringLength(100, MinimumLength = 1)]
        [RegularExpression(@"^[a-zA-Z0-9\s\-\.]+$", ErrorMessage = "Name contains invalid characters")]
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [StringLength(50)]
        [RegularExpression(@"^[a-zA-Z0-9\s\-]+$", ErrorMessage = "Region contains invalid characters")]
        [JsonPropertyName("region")]
        public string? Region { get; set; }

        [Range(1, 150, ErrorMessage = "Age must be between 1 and 150")]
        [JsonPropertyName("age")]
        public int? Age { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Wins cannot be negative")]
        [JsonPropertyName("wins")]
        public int? Wins { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Losses cannot be negative")]
        [JsonPropertyName("losses")]
        public int? Losses { get; set; }
    }

    public class TrainerDto
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

        [JsonPropertyName("pokemons")]
        public List<PokemonDto>? Pokemons { get; set; }
    }
}
