namespace MigraineDiary.Api.Contracts.MigraineAttacks;

public sealed class AttackTriggerResponse
{
    public Guid TriggerId { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }
}