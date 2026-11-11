namespace MigraineDiary.Api.Entities;

public sealed class AttackMedication
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid MigraineAttackId { get; set; }

    public Guid MedicationId { get; set; }

    public string? Dosage { get; set; }

    public DateTimeOffset? TakenAt { get; set; }

    public MigraineAttack MigraineAttack { get; set; } = null!;

    public Medication Medication { get; set; } = null!;
}
