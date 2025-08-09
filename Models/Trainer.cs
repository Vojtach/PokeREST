using System;
using System.Collections.Generic;

namespace PokeREST.Models;

public partial class Trainer
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Region { get; set; }

    public int? Age { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int? Wins { get; set; }

    public int? Losses { get; set; }

    public virtual ICollection<Pokemon> Pokemons { get; set; } = new List<Pokemon>();
}
