using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MigraineDiary.Api.Enums;

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

    private Guid? _migraineAttackId;
    private BodyPosition? _bodyPosition;

    public Guid? MigraineAttackId
    {
        get => _migraineAttackId;
        init
        {
            _migraineAttackId = value;
            HasMigraineAttackId = true;
        }
    }

    [EnumDataType(typeof(BodyPosition))]
    public BodyPosition? BodyPosition
    {
        get => _bodyPosition;
        init
        {
            _bodyPosition = value;
            HasBodyPosition = true;
        }
    }

    [JsonIgnore]
    public bool HasMigraineAttackId { get; private set; }

    [JsonIgnore]
    public bool HasBodyPosition { get; private set; }
}
