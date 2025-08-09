using System;
using System.Collections.Generic;

namespace PokeREST.Models;

public partial class PokemonMoveDetail
{
    public string? PokemonName { get; set; }

    public string? MoveName { get; set; }

    public int? Power { get; set; }

    public string? MoveType { get; set; }
}
