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

    public DbSet<User> Users =>
        Set<User>();

    public DbSet<MigraineAttack> MigraineAttacks =>
        Set<MigraineAttack>();

    public DbSet<Symptom> Symptoms =>
        Set<Symptom>();

    public DbSet<AttackSymptom> AttackSymptoms =>
        Set<AttackSymptom>();

    public DbSet<Trigger> Triggers =>
        Set<Trigger>();

    public DbSet<AttackTrigger> AttackTriggers =>
        Set<AttackTrigger>();

    public DbSet<Medication> Medications =>
        Set<Medication>();

    public DbSet<AttackMedication> AttackMedications =>
        Set<AttackMedication>();

    public DbSet<VitalMeasurement> VitalMeasurements =>
        Set<VitalMeasurement>();

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

        modelBuilder.Entity<Symptom>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .HasMaxLength(100)
                .IsRequired();

            entity.HasIndex(x => x.Name)
                .IsUnique();

            entity.HasData(
                new Symptom
                {
                    Id = Guid.Parse(
                        "11111111-1111-1111-1111-111111111111"),
                    Name = "Nausea"
                },
                new Symptom
                {
                    Id = Guid.Parse(
                        "22222222-2222-2222-2222-222222222222"),
                    Name = "LightSensitivity"
                },
                new Symptom
                {
                    Id = Guid.Parse(
                        "33333333-3333-3333-3333-333333333333"),
                    Name = "SoundSensitivity"
                },
                new Symptom
                {
                    Id = Guid.Parse(
                        "44444444-4444-4444-4444-444444444444"),
                    Name = "Dizziness"
                },
                new Symptom
                {
                    Id = Guid.Parse(
                        "55555555-5555-5555-5555-555555555555"),
                    Name = "VisualDisturbance"
                },
                new Symptom
                {
                    Id = Guid.Parse(
                        "66666666-6666-6666-6666-666666666666"),
                    Name = "Weakness"
                },
                new Symptom
                {
                    Id = Guid.Parse(
                        "77777777-7777-7777-7777-777777777777"),
                    Name = "Other"
                });
        });

        modelBuilder.Entity<AttackSymptom>(entity =>
        {
            entity.HasKey(x => new
            {
                x.MigraineAttackId,
                x.SymptomId
            });

            entity.Property(x => x.Description)
                .HasMaxLength(500);

            entity.HasOne(x => x.MigraineAttack)
                .WithMany(x => x.Symptoms)
                .HasForeignKey(x => x.MigraineAttackId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Symptom)
                .WithMany(x => x.AttackSymptoms)
                .HasForeignKey(x => x.SymptomId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Trigger>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .HasMaxLength(100)
                .IsRequired();

            entity.HasIndex(x => x.Name)
                .IsUnique();

            entity.HasData(
                new Trigger
                {
                    Id = Guid.Parse(
                        "81111111-1111-1111-1111-111111111111"),
                    Name = "Stress"
                },
                new Trigger
                {
                    Id = Guid.Parse(
                        "82222222-2222-2222-2222-222222222222"),
                    Name = "LackOfSleep"
                },
                new Trigger
                {
                    Id = Guid.Parse(
                        "83333333-3333-3333-3333-333333333333"),
                    Name = "WeatherChange"
                },
                new Trigger
                {
                    Id = Guid.Parse(
                        "84444444-4444-4444-4444-444444444444"),
                    Name = "Hunger"
                },
                new Trigger
                {
                    Id = Guid.Parse(
                        "85555555-5555-5555-5555-555555555555"),
                    Name = "CertainFoods"
                },
                new Trigger
                {
                    Id = Guid.Parse(
                        "86666666-6666-6666-6666-666666666666"),
                    Name = "PhysicalExertion"
                },
                new Trigger
                {
                    Id = Guid.Parse(
                        "87777777-7777-7777-7777-777777777777"),
                    Name = "BrightLight"
                },
                new Trigger
                {
                    Id = Guid.Parse(
                        "88888888-8888-8888-8888-888888888888"),
                    Name = "LoudNoise"
                },
                new Trigger
                {
                    Id = Guid.Parse(
                        "89999999-9999-9999-9999-999999999999"),
                    Name = "Other"
                });
        });

        modelBuilder.Entity<AttackTrigger>(entity =>
        {
            entity.HasKey(x => new
            {
                x.MigraineAttackId,
                x.TriggerId
            });

            entity.Property(x => x.Description)
                .HasMaxLength(500);

            entity.HasOne(x => x.MigraineAttack)
                .WithMany(x => x.Triggers)
                .HasForeignKey(x => x.MigraineAttackId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Trigger)
                .WithMany(x => x.AttackTriggers)
                .HasForeignKey(x => x.TriggerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Medication>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .HasMaxLength(200)
                .IsRequired();

            entity.HasOne(x => x.User)
                .WithMany(x => x.Medications)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => new
            {
                x.UserId,
                x.Name
            })
            .IsUnique();
        });

        modelBuilder.Entity<AttackMedication>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Dosage)
                .HasMaxLength(100);

            entity.Property(x => x.TakenAt);

            entity.HasOne(x => x.MigraineAttack)
                .WithMany(x => x.Medications)
                .HasForeignKey(x => x.MigraineAttackId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Medication)
                .WithMany(x => x.AttackMedications)
                .HasForeignKey(x => x.MedicationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.MigraineAttackId);

            entity.HasIndex(x => x.MedicationId);
        });

        modelBuilder.Entity<VitalMeasurement>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.SystolicPressure)
                .IsRequired();

            entity.Property(x => x.DiastolicPressure)
                .IsRequired();

            entity.Property(x => x.Pulse)
                .IsRequired();

            entity.Property(x => x.MeasuredAt)
                .IsRequired();

            entity.Property(x => x.Notes)
                .HasMaxLength(1000);

            entity.Property(x => x.CreatedAt)
                .IsRequired();

            entity.HasOne(x => x.User)
                .WithMany(x => x.VitalMeasurements)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => new
            {
                x.UserId,
                x.MeasuredAt
            });

            entity.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_VitalMeasurements_SystolicPressure",
                    "\"SystolicPressure\" >= 1 AND \"SystolicPressure\" <= 300");

                table.HasCheckConstraint(
                    "CK_VitalMeasurements_DiastolicPressure",
                    "\"DiastolicPressure\" >= 1 AND \"DiastolicPressure\" <= 200");

                table.HasCheckConstraint(
                    "CK_VitalMeasurements_Pulse",
                    "\"Pulse\" >= 1 AND \"Pulse\" <= 300");
            });
        });
    }
}