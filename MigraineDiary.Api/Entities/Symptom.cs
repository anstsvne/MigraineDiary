namespace MigraineDiary.Api.Entities;

public sealed class Symptom
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string Name { get; set; }

    public ICollection<AttackSymptom> AttackSymptoms { get; set; }
        = new List<AttackSymptom>();
}