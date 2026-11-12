namespace MigraineDiary.Api.Contracts.MigraineAttacks;

public sealed class StatisticsItemResponse
{
    public Guid Id { get; init; }

    public required string Name { get; init; }

    public int Count { get; init; }
}