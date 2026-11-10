namespace MigraineDiary.Api.Contracts.Triggers;

public sealed class TriggerResponse
{
    public Guid Id { get; init; }

    public required string Name { get; init; }
}