namespace MigraineDiary.Api.Contracts.VitalMeasurements;

public sealed class VitalMeasurementResponse
{
    public Guid Id { get; init; }

    public int SystolicPressure { get; init; }

    public int DiastolicPressure { get; init; }

    public int Pulse { get; init; }

    public DateTimeOffset MeasuredAt { get; init; }

    public string? Notes { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}