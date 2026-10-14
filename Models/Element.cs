using System;
using System.Collections.Generic;

namespace PokeREST.Models;

/// <summary>
/// Elemental type (e.g. Fire, Water, Electric) shared by Pokemon and moves. Maps to the <c>Elements</c> table.
/// </summary>
/// <remarks>Exposed through the API only by name, as the <c>type</c> field of Pokemon and move response DTOs.</remarks>
public partial class Element
{
    /// <summary>
    /// Primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Unique type name, at most 50 characters.
    /// </summary>
    /// <example>"Electric"</example>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Moves of this type.
    /// </summary>
    public virtual ICollection<Move> Moves { get; set; } = new List<Move>();

    /// <summary>
    /// Pokemon of this type.
    /// </summary>
    public virtual ICollection<Pokemon> Pokemons { get; set; } = new List<Pokemon>();
}
