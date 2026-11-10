namespace MigraineDiary.Api.Entities;

public sealed class AttackTrigger
{
    public Guid MigraineAttackId { get; set; }

    public Guid TriggerId { get; set; }

    public string? Description { get; set; }

    public MigraineAttack MigraineAttack { get; set; } = null!;

    public Trigger Trigger { get; set; } = null!;
}