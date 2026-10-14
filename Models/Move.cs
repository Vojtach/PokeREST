using System;
using System.Collections.Generic;

namespace PokeREST.Models;

/// <summary>
/// Move that Pokemon can learn. Maps to the <c>Moves</c> table.
/// </summary>
/// <remarks>Returned to API clients as <see cref="PokeREST.DTOs.MoveResponseDto"/>.</remarks>
public partial class Move
{
    /// <summary>
    /// Primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Move name, at most 100 characters.
    /// </summary>
    /// <example>"Thunder Shock"</example>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Base power of the move.
    /// </summary>
    public int Power { get; set; }

    /// <summary>
    /// Foreign key to the move's <see cref="Element"/>.
    /// </summary>
    public int TypeId { get; set; }

    /// <summary>
    /// Join entries linking this move to the Pokemon that know it.
    /// </summary>
    public virtual ICollection<PokemonMove> PokemonMoves { get; set; } = new List<PokemonMove>();

    /// <summary>
    /// Elemental type of the move.
    /// </summary>
    public virtual Element Type { get; set; } = null!;
}
