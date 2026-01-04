using CupSystemApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CupSystemApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Tournament> Tournaments => Set<Tournament>();
    public DbSet<Bracket> Brackets => Set<Bracket>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<TournamentParticipant> TournamentParticipants => Set<TournamentParticipant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Tournament>()
            .HasOne(t => t.Bracket)
            .WithOne(b => b.Tournament)
            .HasForeignKey<Bracket>(b => b.TournamentId);

        modelBuilder.Entity<TournamentParticipant>()
            .HasKey(tp => new { tp.TournamentId, tp.UserId });

        modelBuilder.Entity<TournamentParticipant>()
            .HasOne(tp => tp.Tournament)
            .WithMany(t => t.Participants)
            .HasForeignKey(tp => tp.TournamentId);

        modelBuilder.Entity<TournamentParticipant>()
            .HasOne(tp => tp.User)
            .WithMany(u => u.TournamentParticipants)
            .HasForeignKey(tp => tp.UserId);

        modelBuilder.Entity<Match>()
            .HasOne(m => m.Winner)
            .WithMany()
            .HasForeignKey(m => m.WinnerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Match>()
            .HasOne(m => m.Player1)
            .WithMany()
            .HasForeignKey(m => m.Player1Id)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Match>()
            .HasOne(m => m.Player2)
            .WithMany()
            .HasForeignKey(m => m.Player2Id)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
