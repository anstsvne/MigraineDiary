namespace MigraineDiary.Api.Contracts.VitalMeasurements;

public sealed class VitalMeasurementStatisticsResponse
{
    public int MeasurementCount { get; init; }

    public double? AverageSystolicPressure { get; init; }
    public int? MinSystolicPressure { get; init; }
    public int? MaxSystolicPressure { get; init; }

    public double? AverageDiastolicPressure { get; init; }
    public int? MinDiastolicPressure { get; init; }
    public int? MaxDiastolicPressure { get; init; }

    public double? AveragePulse { get; init; }
    public int? MinPulse { get; init; }
    public int? MaxPulse { get; init; }
}
