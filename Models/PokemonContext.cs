using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace PokeREST.Models;

/// <summary>
/// EF Core context for the PostgreSQL database, the only data-access layer in the app (there are no repositories;
/// services query this context directly).
/// </summary>
/// <remarks>
/// Scaffolded database-first: table, constraint and column names mirror the existing schema. Put customizations in
/// a separate partial class via <c>OnModelCreatingPartial</c> so they survive re-scaffolding.
/// <see cref="PokemonMoveDetails"/> and <see cref="PokemonWithTypes"/> map keyless database views and are not
/// used by the current API.
/// </remarks>
public partial class PokemonContext : DbContext
{
    public PokemonContext()
    {
    }

    public PokemonContext(DbContextOptions<PokemonContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Element> Elements { get; set; }

    public virtual DbSet<Move> Moves { get; set; }

    public virtual DbSet<Pokemon> Pokemons { get; set; }

    public virtual DbSet<PokemonMove> PokemonMoves { get; set; }

    public virtual DbSet<PokemonMoveDetail> PokemonMoveDetails { get; set; }

    public virtual DbSet<PokemonWithType> PokemonWithTypes { get; set; }

    public virtual DbSet<Trainer> Trainers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Element>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Elements_pkey");

            entity.HasIndex(e => e.Name, "Elements_Name_key").IsUnique();

            entity.Property(e => e.Name).HasMaxLength(50);
        });

        modelBuilder.Entity<Move>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Moves_pkey");

            entity.Property(e => e.Name).HasMaxLength(100);

            entity.HasOne(d => d.Type).WithMany(p => p.Moves)
                .HasForeignKey(d => d.TypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Moves_TypeId_fkey");
        });

        modelBuilder.Entity<Pokemon>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Pokemon_pkey");

            entity.ToTable("Pokemon");

            entity.Property(e => e.CaughtAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.Name).HasMaxLength(100);

            entity.HasOne(d => d.Owner).WithMany(p => p.Pokemons)
                .HasForeignKey(d => d.OwnerId)
                .HasConstraintName("Pokemon_OwnerId_fkey");

            entity.HasOne(d => d.Type).WithMany(p => p.Pokemons)
                .HasForeignKey(d => d.TypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Pokemon_TypeId_fkey");
        });

        modelBuilder.Entity<PokemonMove>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PokemonMoves_pkey");

            entity.HasIndex(e => new { e.PokemonId, e.MoveId }, "unique_pokemon_move").IsUnique();

            entity.HasOne(d => d.Move).WithMany(p => p.PokemonMoves)
                .HasForeignKey(d => d.MoveId)
                .HasConstraintName("PokemonMoves_MoveId_fkey");

            entity.HasOne(d => d.Pokemon).WithMany(p => p.PokemonMoves)
                .HasForeignKey(d => d.PokemonId)
                .HasConstraintName("PokemonMoves_PokemonId_fkey");
        });

        modelBuilder.Entity<PokemonMoveDetail>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("PokemonMoveDetails");

            entity.Property(e => e.MoveName).HasMaxLength(100);
            entity.Property(e => e.MoveType).HasMaxLength(50);
            entity.Property(e => e.PokemonName).HasMaxLength(100);
        });

        modelBuilder.Entity<PokemonWithType>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("PokemonWithTypes");

            entity.Property(e => e.CaughtAt).HasColumnType("timestamp without time zone");
            entity.Property(e => e.PokemonName).HasMaxLength(100);
            entity.Property(e => e.TypeName).HasMaxLength(50);
        });

        modelBuilder.Entity<Trainer>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Trainers_pkey");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.Losses).HasDefaultValue(0);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Region).HasMaxLength(50);
            entity.Property(e => e.Wins).HasDefaultValue(0);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
