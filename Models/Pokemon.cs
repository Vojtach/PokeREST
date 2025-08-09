using System;
using System.Collections.Generic;

namespace PokeREST.Models;

public partial class Pokemon
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int Level { get; set; }

    public int TypeId { get; set; }

    public int? OwnerId { get; set; }

    public int Health { get; set; }

    public DateTime? CaughtAt { get; set; }

    public virtual Trainer? Owner { get; set; }

    public virtual ICollection<PokemonMove> PokemonMoves { get; set; } = new List<PokemonMove>();

    public virtual Element Type { get; set; } = null!;
}
