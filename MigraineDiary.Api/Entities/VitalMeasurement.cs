using MigraineDiary.Api.Enums;

namespace MigraineDiary.Api.Entities;

public sealed class VitalMeasurement
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public Guid? MigraineAttackId { get; set; }

    public BodyPosition? BodyPosition { get; set; }

    public int SystolicPressure { get; set; }

    public int DiastolicPressure { get; set; }

    public int Pulse { get; set; }

    public DateTimeOffset MeasuredAt { get; set; }

    public string? Notes { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
        = DateTimeOffset.UtcNow;

    public User User { get; set; } = null!;

    public MigraineAttack? MigraineAttack { get; set; }
}
