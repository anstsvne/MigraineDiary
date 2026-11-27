namespace MigraineDiary.Api.Contracts.MigraineAttacks;

public sealed class MigraineAttackStatisticsResponse
{
    public int AttackCount { get; init; }

    public int MigraineDaysCount { get; init; }

    public double AverageIntensity { get; init; }

    public int? MinIntensity { get; init; }

    public int? MaxIntensity { get; init; }

    public double? AverageDurationMinutes { get; init; }

    public double? MaxDurationMinutes { get; init; }

    // Always includes all ten intensity levels, including zero counts.
    public IReadOnlyList<IntensityDistributionItemResponse> IntensityDistribution { get; init; }
        = Array.Empty<IntensityDistributionItemResponse>();

    // Sparse series of attack start dates, not days with ongoing migraine.
    public IReadOnlyList<MigraineAttackDailyTrendItemResponse> DailyTrend { get; init; }
        = Array.Empty<MigraineAttackDailyTrendItemResponse>();

    public int KnownDurationAttackCount { get; init; }

    public int UnknownDurationAttackCount { get; init; }

    public IReadOnlyList<StatisticsItemResponse> Symptoms { get; init; }
        = Array.Empty<StatisticsItemResponse>();

    public IReadOnlyList<StatisticsItemResponse> Triggers { get; init; }
        = Array.Empty<StatisticsItemResponse>();

    public IReadOnlyList<StatisticsItemResponse> Medications { get; init; }
        = Array.Empty<StatisticsItemResponse>();

    public IReadOnlyList<MedicationEffectivenessStatisticsItemResponse>
        MedicationEffectiveness
    { get; init; }
        = Array.Empty<MedicationEffectivenessStatisticsItemResponse>();
}