using System;
using System.Collections.Generic;

namespace PokeREST.Models;

public partial class PokemonWithType
{
    public int? Id { get; set; }

    public string? PokemonName { get; set; }

    public int? Level { get; set; }

    public int? Health { get; set; }

    public string? TypeName { get; set; }

    public DateTime? CaughtAt { get; set; }
}
