namespace MigraineDiary.Api.Contracts.MigraineAttacks;

public sealed class MigraineAttackStatisticsRequest
{
    public DateTimeOffset? From { get; init; }

    public DateTimeOffset? To { get; init; }
}