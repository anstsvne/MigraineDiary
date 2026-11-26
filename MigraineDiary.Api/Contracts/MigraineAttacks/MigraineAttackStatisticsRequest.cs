namespace MigraineDiary.Api.Contracts.MigraineAttacks;

public sealed class MigraineAttackStatisticsRequest
{
    // Legacy timestamp filters.
    public DateTimeOffset? From { get; init; }

    public DateTimeOffset? To { get; init; }

    // Calendar filters (inclusive date range).
    public DateOnly? FromDate { get; init; }

    public DateOnly? ToDate { get; init; }
}
