using System;
using System.Collections.Generic;

namespace PokeREST.Models;

/// <summary>
/// Read-only, keyless projection of the <c>PokemonWithTypes</c> database view: Pokemon joined with their type name.
/// </summary>
/// <remarks>All columns are nullable because they come from a view.</remarks>
public partial class PokemonWithType
{
    /// <summary>
    /// ID of the Pokemon.
    /// </summary>
    public int? Id { get; set; }

    /// <summary>
    /// Name of the Pokemon, at most 100 characters.
    /// </summary>
    public string? PokemonName { get; set; }

    /// <summary>
    /// Current level of the Pokemon.
    /// </summary>
    public int? Level { get; set; }

    /// <summary>
    /// Health points (HP) of the Pokemon.
    /// </summary>
    public int? Health { get; set; }

    /// <summary>
    /// Elemental type name of the Pokemon, at most 50 characters.
    /// </summary>
    public string? TypeName { get; set; }

    /// <summary>
    /// When the Pokemon was caught (UTC, <c>timestamp without time zone</c>).
    /// </summary>
    public DateTime? CaughtAt { get; set; }
}
