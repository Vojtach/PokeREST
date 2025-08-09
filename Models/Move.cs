using System;
using System.Collections.Generic;

namespace PokeREST.Models;

public partial class Move
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int Power { get; set; }

    public int TypeId { get; set; }

    public virtual ICollection<PokemonMove> PokemonMoves { get; set; } = new List<PokemonMove>();

    public virtual Element Type { get; set; } = null!;
}
