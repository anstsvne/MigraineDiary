using System.ComponentModel.DataAnnotations;
using MigraineDiary.Api.Contracts.Common;
using MigraineDiary.Api.Enums;

namespace MigraineDiary.Api.Contracts.MigraineAttacks;

public sealed class MigraineAttackFilterRequest
    : PaginationRequest
{
    // Legacy timestamp filters.
    public DateTimeOffset? From { get; init; }

    public DateTimeOffset? To { get; init; }

    // Calendar filters (inclusive date range).
    public DateOnly? FromDate { get; init; }

    public DateOnly? ToDate { get; init; }

    [Range(1, 10)]
    public int? MinIntensity { get; init; }

    [Range(1, 10)]
    public int? MaxIntensity { get; init; }

    [Range(0d, double.MaxValue)]
    public double? MinDurationMinutes { get; init; }

    [Range(0d, double.MaxValue)]
    public double? MaxDurationMinutes { get; init; }

    public string SortBy { get; init; } = "date";

    public string SortDirection { get; init; } = "desc";

    public PainLocation? PainLocation { get; init; }

    public Guid? SymptomId { get; init; }

    public Guid? TriggerId { get; init; }

    public Guid? MedicationId { get; init; }
}
