using System;
using System.Collections.Generic;

namespace PokeREST.Models;

public partial class PokemonMove
{
    public int Id { get; set; }

    public int PokemonId { get; set; }

    public int MoveId { get; set; }

    public virtual Move Move { get; set; } = null!;

    public virtual Pokemon Pokemon { get; set; } = null!;
}
