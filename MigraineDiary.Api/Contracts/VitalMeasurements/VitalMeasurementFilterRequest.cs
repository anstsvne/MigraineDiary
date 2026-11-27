using System.ComponentModel.DataAnnotations;
using MigraineDiary.Api.Contracts.Common;
using MigraineDiary.Api.Enums;

namespace MigraineDiary.Api.Contracts.VitalMeasurements;

public sealed class VitalMeasurementFilterRequest : PaginationRequest
{
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
    public DateOnly? FromDate { get; init; }
    public DateOnly? ToDate { get; init; }

    [Range(1, 300)]
    public int? MinSystolicPressure { get; init; }

    [Range(1, 300)]
    public int? MaxSystolicPressure { get; init; }

    [Range(1, 200)]
    public int? MinDiastolicPressure { get; init; }

    [Range(1, 200)]
    public int? MaxDiastolicPressure { get; init; }

    [Range(1, 300)]
    public int? MinPulse { get; init; }

    [Range(1, 300)]
    public int? MaxPulse { get; init; }

    public Guid? MigraineAttackId { get; init; }
    public bool? HasMigraineAttack { get; init; }

    [EnumDataType(typeof(BodyPosition))]
    public BodyPosition? BodyPosition { get; init; }

    public string SortBy { get; init; } = "date";
    public string SortDirection { get; init; } = "desc";
}
