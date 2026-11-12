namespace MigraineDiary.Api.Contracts.MigraineAttacks;

public sealed class MigraineAttackStatisticsResponse
{
    public int AttackCount { get; init; }

    public double AverageIntensity { get; init; }

    public double AverageDurationMinutes { get; init; }

    public IReadOnlyList<StatisticsItemResponse> Symptoms { get; init; }
        = Array.Empty<StatisticsItemResponse>();

    public IReadOnlyList<StatisticsItemResponse> Triggers { get; init; }
        = Array.Empty<StatisticsItemResponse>();

    public IReadOnlyList<StatisticsItemResponse> Medications { get; init; }
        = Array.Empty<StatisticsItemResponse>();
}
