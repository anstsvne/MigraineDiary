using MigraineDiary.Api.Contracts.MigraineAttacks;
using MigraineDiary.Api.Data;
using MigraineDiary.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace MigraineDiary.Api.Services;

public sealed class MigraineAttackService
{
    private readonly AppDbContext _dbContext;

    public MigraineAttackService(
        AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MigraineAttackResponse> CreateAsync(
        Guid userId,
        CreateMigraineAttackRequest request,
        CancellationToken cancellationToken = default)
    {
        var startAt = request.StartAt!.Value.ToUniversalTime();
        var endAt = request.EndAt!.Value.ToUniversalTime();

        if (endAt < startAt)
        {
            throw new ArgumentException(
                "Время окончания приступа не может быть раньше времени начала.");
        }

        var symptoms =
            await ValidateAndNormalizeSymptomsAsync(
                request.Symptoms,
                cancellationToken);

        var triggers =
            await ValidateAndNormalizeTriggersAsync(
                request.Triggers,
                cancellationToken);

        var medications =
            await ValidateAndNormalizeMedicationsAsync(
                userId,
                request.Medications,
                cancellationToken);

        var attack = new MigraineAttack
        {
            UserId = userId,
            StartAt = startAt,
            EndAt = endAt,
            Intensity = request.Intensity,
            PainLocation = request.PainLocation!.Value,

            PainLocationDescription =
                string.IsNullOrWhiteSpace(
                    request.PainLocationDescription)
                    ? null
                    : request.PainLocationDescription.Trim(),

            Notes =
                string.IsNullOrWhiteSpace(request.Notes)
                    ? null
                    : request.Notes.Trim()
        };

        foreach (var symptom in symptoms)
        {
            attack.Symptoms.Add(
                new AttackSymptom
                {
                    SymptomId = symptom.Key,
                    Description = symptom.Value
                });
        }

        foreach (var trigger in triggers)
        {
            attack.Triggers.Add(
                new AttackTrigger
                {
                    TriggerId = trigger.Key,
                    Description = trigger.Value
                });
        }

        foreach (var medication in medications)
        {
            attack.Medications.Add(
                new AttackMedication
                {
                    MedicationId = medication.MedicationId,
                    Dosage = medication.Dosage,
                    TakenAt = medication.TakenAt
                });
        }

        _dbContext.MigraineAttacks.Add(attack);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return (await GetByIdAsync(
            userId,
            attack.Id,
            cancellationToken))!;
    }

    public async Task<IReadOnlyList<MigraineAttackResponse>> GetAllAsync(
        Guid userId,
        MigraineAttackFilterRequest filter,
        CancellationToken cancellationToken = default)
    {
        ValidateFilters(filter);

        var query = _dbContext.MigraineAttacks
            .AsNoTracking()
            .Where(x => x.UserId == userId);

        if (filter.From.HasValue)
        {
            var from = filter.From.Value.ToUniversalTime();

            query = query.Where(x =>
                x.StartAt >= from);
        }

        if (filter.To.HasValue)
        {
            var to = filter.To.Value.ToUniversalTime();

            query = query.Where(x =>
                x.StartAt <= to);
        }

        if (filter.MinIntensity.HasValue)
        {
            query = query.Where(x =>
                x.Intensity >= filter.MinIntensity.Value);
        }

        if (filter.MaxIntensity.HasValue)
        {
            query = query.Where(x =>
                x.Intensity <= filter.MaxIntensity.Value);
        }

        if (filter.PainLocation.HasValue)
        {
            query = query.Where(x =>
                x.PainLocation == filter.PainLocation.Value);
        }

        if (filter.SymptomId.HasValue)
        {
            var symptomId = filter.SymptomId.Value;

            query = query.Where(x =>
                x.Symptoms.Any(s =>
                    s.SymptomId == symptomId));
        }

        if (filter.TriggerId.HasValue)
        {
            var triggerId = filter.TriggerId.Value;

            query = query.Where(x =>
                x.Triggers.Any(t =>
                    t.TriggerId == triggerId));
        }

        if (filter.MedicationId.HasValue)
        {
            var medicationId = filter.MedicationId.Value;

            query = query.Where(x =>
                x.Medications.Any(m =>
                    m.MedicationId == medicationId));
        }

        return await query
            .OrderByDescending(x => x.StartAt)
            .Select(x => new MigraineAttackResponse
            {
                Id = x.Id,
                StartAt = x.StartAt,
                EndAt = x.EndAt,

                DurationMinutes =
                    (x.EndAt - x.StartAt).TotalMinutes,

                Intensity = x.Intensity,
                PainLocation = x.PainLocation,

                PainLocationDescription =
                    x.PainLocationDescription,

                Notes = x.Notes,
                CreatedAt = x.CreatedAt,

                Symptoms = x.Symptoms
                    .OrderBy(s => s.Symptom.Name)
                    .Select(s => new AttackSymptomResponse
                    {
                        SymptomId = s.SymptomId,
                        Name = s.Symptom.Name,
                        Description = s.Description
                    })
                    .ToList(),

                Triggers = x.Triggers
                    .OrderBy(t => t.Trigger.Name)
                    .Select(t => new AttackTriggerResponse
                    {
                        TriggerId = t.TriggerId,
                        Name = t.Trigger.Name,
                        Description = t.Description
                    })
                    .ToList(),

                Medications = x.Medications
                    .OrderBy(m => m.TakenAt)
                    .ThenBy(m => m.Medication.Name)
                    .Select(m => new AttackMedicationResponse
                    {
                        Id = m.Id,
                        MedicationId = m.MedicationId,
                        Name = m.Medication.Name,
                        Dosage = m.Dosage,
                        TakenAt = m.TakenAt
                    })
                    .ToList()
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<MigraineAttackResponse?> GetByIdAsync(
        Guid userId,
        Guid attackId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.MigraineAttacks
            .AsNoTracking()
            .Where(x =>
                x.Id == attackId &&
                x.UserId == userId)
            .Select(x => new MigraineAttackResponse
            {
                Id = x.Id,
                StartAt = x.StartAt,
                EndAt = x.EndAt,

                DurationMinutes =
                    (x.EndAt - x.StartAt).TotalMinutes,

                Intensity = x.Intensity,
                PainLocation = x.PainLocation,

                PainLocationDescription =
                    x.PainLocationDescription,

                Notes = x.Notes,
                CreatedAt = x.CreatedAt,

                Symptoms = x.Symptoms
                    .OrderBy(s => s.Symptom.Name)
                    .Select(s => new AttackSymptomResponse
                    {
                        SymptomId = s.SymptomId,
                        Name = s.Symptom.Name,
                        Description = s.Description
                    })
                    .ToList(),

                Triggers = x.Triggers
                    .OrderBy(t => t.Trigger.Name)
                    .Select(t => new AttackTriggerResponse
                    {
                        TriggerId = t.TriggerId,
                        Name = t.Trigger.Name,
                        Description = t.Description
                    })
                    .ToList(),

                Medications = x.Medications
                    .OrderBy(m => m.TakenAt)
                    .ThenBy(m => m.Medication.Name)
                    .Select(m => new AttackMedicationResponse
                    {
                        Id = m.Id,
                        MedicationId = m.MedicationId,
                        Name = m.Medication.Name,
                        Dosage = m.Dosage,
                        TakenAt = m.TakenAt
                    })
                    .ToList()
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<MigraineAttackResponse?> UpdateAsync(
        Guid userId,
        Guid attackId,
        UpdateMigraineAttackRequest request,
        CancellationToken cancellationToken = default)
    {
        var attack = await _dbContext.MigraineAttacks
            .Include(x => x.Symptoms)
            .Include(x => x.Triggers)
            .SingleOrDefaultAsync(
                x =>
                    x.Id == attackId &&
                    x.UserId == userId,
                cancellationToken);

        if (attack is null)
        {
            return null;
        }

        var startAt = request.StartAt!.Value.ToUniversalTime();
        var endAt = request.EndAt!.Value.ToUniversalTime();

        if (endAt < startAt)
        {
            throw new ArgumentException(
                "Время окончания приступа не может быть раньше времени начала.");
        }

        var requestedSymptoms =
            await ValidateAndNormalizeSymptomsAsync(
                request.Symptoms,
                cancellationToken);

        var requestedTriggers =
            await ValidateAndNormalizeTriggersAsync(
                request.Triggers,
                cancellationToken);

        var requestedMedications =
            await ValidateAndNormalizeMedicationsAsync(
                userId,
                request.Medications,
                cancellationToken);

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        attack.StartAt = startAt;
        attack.EndAt = endAt;
        attack.Intensity = request.Intensity;
        attack.PainLocation = request.PainLocation!.Value;

        attack.PainLocationDescription =
            string.IsNullOrWhiteSpace(
                request.PainLocationDescription)
                ? null
                : request.PainLocationDescription.Trim();

        attack.Notes =
            string.IsNullOrWhiteSpace(request.Notes)
                ? null
                : request.Notes.Trim();

        var symptomsToRemove = attack.Symptoms
            .Where(x =>
                !requestedSymptoms.ContainsKey(
                    x.SymptomId))
            .ToList();

        _dbContext.AttackSymptoms.RemoveRange(
            symptomsToRemove);

        foreach (var requestedSymptom in requestedSymptoms)
        {
            var existingSymptom = attack.Symptoms
                .SingleOrDefault(x =>
                    x.SymptomId == requestedSymptom.Key);

            if (existingSymptom is null)
            {
                attack.Symptoms.Add(
                    new AttackSymptom
                    {
                        SymptomId = requestedSymptom.Key,
                        Description = requestedSymptom.Value
                    });
            }
            else
            {
                existingSymptom.Description =
                    requestedSymptom.Value;
            }
        }

        var triggersToRemove = attack.Triggers
            .Where(x =>
                !requestedTriggers.ContainsKey(
                    x.TriggerId))
            .ToList();

        _dbContext.AttackTriggers.RemoveRange(
            triggersToRemove);

        foreach (var requestedTrigger in requestedTriggers)
        {
            var existingTrigger = attack.Triggers
                .SingleOrDefault(x =>
                    x.TriggerId == requestedTrigger.Key);

            if (existingTrigger is null)
            {
                attack.Triggers.Add(
                    new AttackTrigger
                    {
                        TriggerId = requestedTrigger.Key,
                        Description = requestedTrigger.Value
                    });
            }
            else
            {
                existingTrigger.Description =
                    requestedTrigger.Value;
            }
        }

        await _dbContext.AttackMedications
            .Where(x =>
                x.MigraineAttackId == attackId)
            .ExecuteDeleteAsync(
                cancellationToken);

        foreach (var medication in requestedMedications)
        {
            _dbContext.AttackMedications.Add(
                new AttackMedication
                {
                    Id = Guid.NewGuid(),
                    MigraineAttackId = attackId,
                    MedicationId = medication.MedicationId,
                    Dosage = medication.Dosage,
                    TakenAt = medication.TakenAt
                });
        }

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);

        return await GetByIdAsync(
            userId,
            attackId,
            cancellationToken);
    }

    public async Task<bool> DeleteAsync(
        Guid userId,
        Guid attackId,
        CancellationToken cancellationToken = default)
    {
        var attack = await _dbContext.MigraineAttacks
            .SingleOrDefaultAsync(
                x =>
                    x.Id == attackId &&
                    x.UserId == userId,
                cancellationToken);

        if (attack is null)
        {
            return false;
        }

        _dbContext.MigraineAttacks.Remove(attack);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return true;
    }

    private static void ValidateFilters(
        MigraineAttackFilterRequest filter)
    {
        if (filter.From.HasValue &&
            filter.To.HasValue &&
            filter.To.Value < filter.From.Value)
        {
            throw new ArgumentException(
                "Конец периода не может быть раньше начала периода.");
        }

        if (filter.MinIntensity.HasValue &&
            filter.MaxIntensity.HasValue &&
            filter.MaxIntensity.Value <
            filter.MinIntensity.Value)
        {
            throw new ArgumentException(
                "Максимальная интенсивность не может быть меньше минимальной.");
        }

        if (filter.SymptomId.HasValue &&
            filter.SymptomId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Идентификатор симптома не может быть пустым.");
        }

        if (filter.TriggerId.HasValue &&
            filter.TriggerId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Идентификатор триггера не может быть пустым.");
        }

        if (filter.MedicationId.HasValue &&
            filter.MedicationId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Идентификатор препарата не может быть пустым.");
        }
    }

    private async Task<Dictionary<Guid, string?>>
        ValidateAndNormalizeSymptomsAsync(
            IReadOnlyCollection<AttackSymptomRequest> symptoms,
            CancellationToken cancellationToken)
    {
        if (symptoms.Count == 0)
        {
            return new Dictionary<Guid, string?>();
        }

        if (symptoms.Any(x => x.SymptomId == Guid.Empty))
        {
            throw new ArgumentException(
                "Идентификатор симптома не может быть пустым.");
        }

        var symptomIds = symptoms
            .Select(x => x.SymptomId)
            .ToArray();

        if (symptomIds.Distinct().Count() != symptomIds.Length)
        {
            throw new ArgumentException(
                "Один и тот же симптом нельзя указать несколько раз.");
        }

        var existingIds = await _dbContext.Symptoms
            .Where(x => symptomIds.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (existingIds.Count != symptomIds.Length)
        {
            throw new ArgumentException(
                "Один или несколько указанных симптомов не существуют.");
        }

        return symptoms.ToDictionary(
            x => x.SymptomId,
            x => string.IsNullOrWhiteSpace(x.Description)
                ? null
                : x.Description.Trim());
    }

    private async Task<Dictionary<Guid, string?>>
        ValidateAndNormalizeTriggersAsync(
            IReadOnlyCollection<AttackTriggerRequest> triggers,
            CancellationToken cancellationToken)
    {
        if (triggers.Count == 0)
        {
            return new Dictionary<Guid, string?>();
        }

        if (triggers.Any(x => x.TriggerId == Guid.Empty))
        {
            throw new ArgumentException(
                "Идентификатор триггера не может быть пустым.");
        }

        var triggerIds = triggers
            .Select(x => x.TriggerId)
            .ToArray();

        if (triggerIds.Distinct().Count() != triggerIds.Length)
        {
            throw new ArgumentException(
                "Один и тот же триггер нельзя указать несколько раз.");
        }

        var existingIds = await _dbContext.Triggers
            .Where(x => triggerIds.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (existingIds.Count != triggerIds.Length)
        {
            throw new ArgumentException(
                "Один или несколько указанных триггеров не существуют.");
        }

        return triggers.ToDictionary(
            x => x.TriggerId,
            x => string.IsNullOrWhiteSpace(x.Description)
                ? null
                : x.Description.Trim());
    }

    private async Task<IReadOnlyList<NormalizedAttackMedication>>
        ValidateAndNormalizeMedicationsAsync(
            Guid userId,
            IReadOnlyCollection<AttackMedicationRequest> medications,
            CancellationToken cancellationToken)
    {
        if (medications.Count == 0)
        {
            return Array.Empty<NormalizedAttackMedication>();
        }

        if (medications.Any(x =>
            x.MedicationId == Guid.Empty))
        {
            throw new ArgumentException(
                "Идентификатор препарата не может быть пустым.");
        }

        var medicationIds = medications
            .Select(x => x.MedicationId)
            .Distinct()
            .ToArray();

        var existingIds = await _dbContext.Medications
            .Where(x =>
                x.UserId == userId &&
                medicationIds.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (existingIds.Count != medicationIds.Length)
        {
            throw new ArgumentException(
                "Один или несколько указанных препаратов не существуют.");
        }

        return medications
            .Select(x =>
                new NormalizedAttackMedication(
                    x.MedicationId,
                    string.IsNullOrWhiteSpace(x.Dosage)
                        ? null
                        : x.Dosage.Trim(),
                    x.TakenAt?.ToUniversalTime()))
            .ToList();
    }

    private sealed record NormalizedAttackMedication(
        Guid MedicationId,
        string? Dosage,
        DateTimeOffset? TakenAt);
}