using LittleLeagueSoccer.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace LittleLeagueSoccer.Api.Data.EfCore;

public class SoccerDbContext(DbContextOptions<SoccerDbContext> options) : DbContext(options)
{
    public DbSet<Championship> Championships => Set<Championship>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Player> Players => Set<Player>();
    public DbSet<Game> Games => Set<Game>();
    public DbSet<PlayerStats> PlayerStats => Set<PlayerStats>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Championship>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(100);
        });

        b.Entity<Team>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(100);
            e.HasOne(x => x.Championship).WithMany(c => c.Teams)
                .HasForeignKey(x => x.ChampionshipId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Player>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(100);
            e.HasOne(x => x.Team).WithMany(t => t.Players)
                .HasForeignKey(x => x.TeamId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Game>(e =>
        {
            e.HasOne(x => x.TeamA).WithMany()
                .HasForeignKey(x => x.TeamAId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.TeamB).WithMany()
                .HasForeignKey(x => x.TeamBId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<PlayerStats>(e =>
        {
            e.ToTable("PlayerStats");
            e.Property(x => x.Position).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(x => new { x.GameId, x.PlayerId }).IsUnique();
            e.HasOne(x => x.Game).WithMany(g => g.PlayerStats)
                .HasForeignKey(x => x.GameId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Player).WithMany(p => p.Stats)
                .HasForeignKey(x => x.PlayerId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
