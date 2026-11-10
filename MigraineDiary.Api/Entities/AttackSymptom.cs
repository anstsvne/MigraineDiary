namespace MigraineDiary.Api.Entities;

public sealed class AttackSymptom
{
    public Guid MigraineAttackId { get; set; }

    public Guid SymptomId { get; set; }

    public string? Description { get; set; }

    public MigraineAttack MigraineAttack { get; set; } = null!;

    public Symptom Symptom { get; set; } = null!;
}