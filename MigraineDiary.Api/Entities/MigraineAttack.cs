using MigraineDiary.Api.Enums;

namespace MigraineDiary.Api.Entities;

public sealed class MigraineAttack
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public DateTimeOffset StartAt { get; set; }

    public DateTimeOffset EndAt { get; set; }

    public int Intensity { get; set; }

    public PainLocation PainLocation { get; set; }

    public string? PainLocationDescription { get; set; }

    public string? Notes { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
        = DateTimeOffset.UtcNow;

    public User User { get; set; } = null!;
}