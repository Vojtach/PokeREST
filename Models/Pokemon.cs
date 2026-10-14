using System;
using System.Collections.Generic;

namespace PokeREST.Models;

/// <summary>
/// Individual Pokemon, either wild (no owner) or owned by a trainer. Maps to the <c>Pokemon</c> table.
/// </summary>
/// <remarks>Returned to API clients as <see cref="PokeREST.DTOs.PokemonResponseDto"/>.</remarks>
public partial class Pokemon
{
    /// <summary>
    /// Primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Pokemon name, at most 100 characters.
    /// </summary>
    /// <example>"Pikachu"</example>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Current level, used by the level range filters (1-100).
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// Foreign key to the Pokemon's <see cref="Element"/>.
    /// </summary>
    public int TypeId { get; set; }

    /// <summary>
    /// Foreign key to the owning <see cref="Trainer"/>; <c>null</c> for wild Pokemon.
    /// </summary>
    /// <remarks>Set when a trainer catches the Pokemon and reset to <c>null</c> when the owning trainer is deleted.</remarks>
    public int? OwnerId { get; set; }

    /// <summary>
    /// Health points (HP).
    /// </summary>
    public int Health { get; set; }

    /// <summary>
    /// When the Pokemon was caught, stored as UTC in a <c>timestamp without time zone</c> column.
    /// </summary>
    /// <remarks>Defaults to <c>CURRENT_TIMESTAMP</c> on insert and is overwritten when a trainer catches the Pokemon.</remarks>
    public DateTime? CaughtAt { get; set; }

    /// <summary>
    /// Owning trainer, or <c>null</c> for wild Pokemon.
    /// </summary>
    public virtual Trainer? Owner { get; set; }

    /// <summary>
    /// Join entries linking this Pokemon to the moves it knows.
    /// </summary>
    public virtual ICollection<PokemonMove> PokemonMoves { get; set; } = new List<PokemonMove>();

    /// <summary>
    /// Elemental type of the Pokemon.
    /// </summary>
    public virtual Element Type { get; set; } = null!;
}
