namespace MigraineDiary.Api.Entities;

public sealed class Trigger
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string Name { get; set; }

    public ICollection<AttackTrigger> AttackTriggers { get; set; }
        = new List<AttackTrigger>();
}