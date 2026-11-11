using System.ComponentModel.DataAnnotations;

namespace MigraineDiary.Api.Contracts.VitalMeasurements;

public sealed class UpdateVitalMeasurementRequest
{
    [Range(1, 300)]
    public int SystolicPressure { get; init; }

    [Range(1, 200)]
    public int DiastolicPressure { get; init; }

    [Range(1, 300)]
    public int Pulse { get; init; }

    [Required]
    public DateTimeOffset? MeasuredAt { get; init; }

    [MaxLength(1000)]
    public string? Notes { get; init; }
}