namespace MigraineDiary.Api.Contracts.MigraineAttacks;

public sealed class MigraineAttackDailyTrendItemResponse
{
    public DateOnly Date { get; init; }

    public int AttackCount { get; init; }

    public double AverageIntensity { get; init; }

    public int MaxIntensity { get; init; }

    public int KnownDurationAttackCount { get; init; }

    public double? AverageDurationMinutes { get; init; }

    public double? MaxDurationMinutes { get; init; }
}
