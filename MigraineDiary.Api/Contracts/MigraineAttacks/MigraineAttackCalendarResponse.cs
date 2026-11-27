using System.ComponentModel.DataAnnotations;

namespace MigraineDiary.Api.Contracts.MigraineAttacks;

public sealed class MigraineAttackCalendarRequest
{
    [Required]
    public DateOnly? From { get; init; }

    [Required]
    public DateOnly? To { get; init; }
}

public sealed class MigraineAttackCalendarResponse
{
    public DateOnly From { get; init; }

    public DateOnly To { get; init; }

    public IReadOnlyList<MigraineAttackCalendarDayResponse> Days { get; init; }
        = Array.Empty<MigraineAttackCalendarDayResponse>();
}

public sealed class MigraineAttackCalendarDayResponse
{
    public DateOnly Date { get; init; }

    public int AttackCount { get; init; }

    public int MaxIntensity { get; init; }

    public IReadOnlyList<Guid> AttackIds { get; init; } = Array.Empty<Guid>();
}
