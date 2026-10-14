using System;
using System.Collections.Generic;

namespace PokeREST.Models;

/// <summary>
/// Pokemon trainer who can own Pokemon. Maps to the <c>Trainers</c> table.
/// </summary>
/// <remarks>
/// Created from <see cref="PokeREST.DTOs.CreateTrainerDto"/>, updated from <see cref="PokeREST.DTOs.UpdateTrainerDto"/>
/// and returned as <see cref="PokeREST.DTOs.TrainerResponseDto"/> or <see cref="PokeREST.DTOs.TrainerWithPokemonDto"/>.
/// </remarks>
public partial class Trainer
{
    /// <summary>
    /// Primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Display name, 1-100 characters.
    /// </summary>
    /// <example>"Ash Ketchum"</example>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Home region, at most 50 characters; <c>null</c> if unknown.
    /// </summary>
    /// <example>"Kanto"</example>
    public string? Region { get; set; }

    /// <summary>
    /// Age in years (1-99 when set through the API); <c>null</c> if unknown.
    /// </summary>
    public int? Age { get; set; }

    /// <summary>
    /// When the trainer was created, stored as UTC in a <c>timestamp without time zone</c> column.
    /// </summary>
    /// <remarks>Defaults to <c>CURRENT_TIMESTAMP</c> on insert.</remarks>
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// Total number of battles won. Defaults to 0.
    /// </summary>
    public int? Wins { get; set; }

    /// <summary>
    /// Total number of battles lost. Defaults to 0.
    /// </summary>
    public int? Losses { get; set; }

    /// <summary>
    /// Pokemon owned by this trainer. They are released (owner set to <c>null</c>) when the trainer is deleted.
    /// </summary>
    public virtual ICollection<Pokemon> Pokemons { get; set; } = new List<Pokemon>();
}
