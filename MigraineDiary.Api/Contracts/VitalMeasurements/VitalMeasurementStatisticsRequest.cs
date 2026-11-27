namespace MigraineDiary.Api.Contracts.VitalMeasurements;

public sealed class VitalMeasurementStatisticsRequest
{
    public DateTimeOffset? From { get; init; }

    public DateTimeOffset? To { get; init; }

    // Inclusive UTC calendar dates. Do not mix with timestamp filters.
    public DateOnly? FromDate { get; init; }

    public DateOnly? ToDate { get; init; }
}
