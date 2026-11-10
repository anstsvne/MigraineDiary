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
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.MigraineAttacks
            .AsNoTracking()
            .Where(x => x.UserId == userId)
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

        await _dbContext.SaveChangesAsync(
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
}