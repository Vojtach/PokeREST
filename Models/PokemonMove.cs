using System;
using System.Collections.Generic;

namespace PokeREST.Models;

/// <summary>
/// Many-to-many join between <see cref="Pokemon"/> and the <see cref="Move"/>s they know. Maps to the <c>PokemonMoves</c> table.
/// </summary>
/// <remarks>The pair (<see cref="PokemonId"/>, <see cref="MoveId"/>) is unique, so a Pokemon cannot learn the same move twice.</remarks>
public partial class PokemonMove
{
    /// <summary>
    /// Primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to the <see cref="Pokemon"/> that knows the move.
    /// </summary>
    public int PokemonId { get; set; }

    /// <summary>
    /// Foreign key to the learned <see cref="Move"/>.
    /// </summary>
    public int MoveId { get; set; }

    /// <summary>
    /// The learned move.
    /// </summary>
    public virtual Move Move { get; set; } = null!;

    /// <summary>
    /// The Pokemon that knows the move.
    /// </summary>
    public virtual Pokemon Pokemon { get; set; } = null!;
}
