namespace MigraineDiary.Api.Entities;

public sealed class Medication
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public required string Name { get; set; }

    public User User { get; set; } = null!;

    public ICollection<AttackMedication> AttackMedications { get; set; }
        = new List<AttackMedication>();
}