using Analyzer.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Analyzer.Infrastructure.Data;

public class AnalyzerDbContext(DbContextOptions<AnalyzerDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Invite> Invites => Set<Invite>();
    public DbSet<ITSystem> ITSystems => Set<ITSystem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(builder =>
        {
            builder.HasKey(u => u.Id);
            builder.HasIndex(u => u.Username).IsUnique();
            builder.HasIndex(u => u.Email).IsUnique();

            builder.Property(u => u.Role).HasConversion<int>();
        });

        modelBuilder.Entity<Team>(builder =>
        {
            builder.HasKey(t => t.Id);

            builder.Ignore(t => t.MemberIds);
            builder.Ignore("_memberIds");
        });

        modelBuilder.Entity<Invite>(builder =>
        {
            builder.HasKey(i => i.Id);
            builder.HasIndex(i => i.Code).IsUnique();

            builder.Property(i => i.Status).HasConversion<int>();
            builder.Property(i => i.Role).HasConversion<int>();
        });

        modelBuilder.Entity<ITSystem>(builder =>
        {
            builder.HasKey(s => s.Id);
            builder.HasIndex(s => new { s.TeamId, s.Name }).IsUnique();
        });
    }
}