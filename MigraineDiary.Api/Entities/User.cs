namespace MigraineDiary.Api.Entities;

public sealed class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string Name { get; set; }

    public required string Email { get; set; }

    public required string PasswordHash { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
        = DateTimeOffset.UtcNow;

    public ICollection<MigraineAttack> MigraineAttacks { get; set; }
        = new List<MigraineAttack>();

    public ICollection<Medication> Medications { get; set; }
        = new List<Medication>();

    public ICollection<VitalMeasurement> VitalMeasurements { get; set; }
        = new List<VitalMeasurement>();
}