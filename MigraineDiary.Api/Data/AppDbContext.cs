using MigraineDiary.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace MigraineDiary.Api.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(
        DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<MigraineAttack> MigraineAttacks =>
        Set<MigraineAttack>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.Email)
                .HasMaxLength(320)
                .IsRequired();

            entity.Property(x => x.PasswordHash)
                .IsRequired();

            entity.HasIndex(x => x.Email)
                .IsUnique();
        });

        modelBuilder.Entity<MigraineAttack>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.StartAt)
                .IsRequired();

            entity.Property(x => x.EndAt)
                .IsRequired();

            entity.Property(x => x.Intensity)
                .IsRequired();

            entity.Property(x => x.PainLocation)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(x => x.PainLocationDescription)
                .HasMaxLength(500);

            entity.Property(x => x.Notes)
                .HasMaxLength(4000);

            entity.HasOne(x => x.User)
                .WithMany(x => x.MigraineAttacks)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_MigraineAttacks_Intensity",
                    "\"Intensity\" >= 1 AND \"Intensity\" <= 10");

                table.HasCheckConstraint(
                    "CK_MigraineAttacks_EndAt",
                    "\"EndAt\" >= \"StartAt\"");
            });
        });
    }
}