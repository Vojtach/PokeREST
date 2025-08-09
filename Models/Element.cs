using System;
using System.Collections.Generic;

namespace PokeREST.Models;

public partial class Element
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Move> Moves { get; set; } = new List<Move>();

    public virtual ICollection<Pokemon> Pokemons { get; set; } = new List<Pokemon>();
}
