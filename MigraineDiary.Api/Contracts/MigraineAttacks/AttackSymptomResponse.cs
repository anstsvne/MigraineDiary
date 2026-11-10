namespace MigraineDiary.Api.Contracts.MigraineAttacks;

public sealed class AttackSymptomResponse
{
    public Guid SymptomId { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }
}