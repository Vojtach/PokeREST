using System;
using System.Collections.Generic;

namespace PokeREST.Models;

/// <summary>
/// Read-only, keyless projection of the <c>PokemonMoveDetails</c> database view: one row per Pokemon/move pair.
/// </summary>
/// <remarks>All columns are nullable because they come from a view.</remarks>
public partial class PokemonMoveDetail
{
    /// <summary>
    /// Name of the Pokemon, at most 100 characters.
    /// </summary>
    public string? PokemonName { get; set; }

    /// <summary>
    /// Name of the move, at most 100 characters.
    /// </summary>
    public string? MoveName { get; set; }

    /// <summary>
    /// Base power of the move.
    /// </summary>
    public int? Power { get; set; }

    /// <summary>
    /// Elemental type name of the move, at most 50 characters.
    /// </summary>
    public string? MoveType { get; set; }
}
