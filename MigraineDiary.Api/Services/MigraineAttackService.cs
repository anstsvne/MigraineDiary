using MigraineDiary.Api.Contracts.Common;
using MigraineDiary.Api.Contracts.MigraineAttacks;
using MigraineDiary.Api.Data;
using MigraineDiary.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace MigraineDiary.Api.Services;

public sealed class MigraineAttackService
{
    private readonly AppDbContext _dbContext;

    public MigraineAttackService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MigraineAttackResponse> CreateAsync(
        Guid userId,
        CreateMigraineAttackRequest request,
        CancellationToken cancellationToken = default)
    {
        var period = NormalizeAttackPeriod(
            request.StartDate,
            request.EndDate,
            request.StartAt,
            request.EndAt);

        var symptoms = await ValidateAndNormalizeSymptomsAsync(
            request.Symptoms,
            cancellationToken);

        var triggers = await ValidateAndNormalizeTriggersAsync(
            request.Triggers,
            cancellationToken);

        var medications = await ValidateAndNormalizeMedicationsAsync(
            userId,
            request.Medications,
            cancellationToken);

        var attack = new MigraineAttack
        {
            UserId = userId,

            StartDate = period.StartDate,
            EndDate = period.EndDate,

            StartAt = period.StartAt,
            EndAt = period.EndAt,

            Intensity = request.Intensity,
            PainLocation = request.PainLocation!.Value,

            PainLocationDescription = NormalizeText(
                request.PainLocationDescription),

            Notes = NormalizeText(request.Notes)
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

        await _dbContext.SaveChangesAsync(cancellationToken);

        return (await GetByIdAsync(
            userId,
            attack.Id,
            cancellationToken))!;
    }

    public async Task<PagedResponse<MigraineAttackResponse>> GetAllAsync(
        Guid userId,
        MigraineAttackFilterRequest filter,
        CancellationToken cancellationToken = default)
    {
        ValidateFilters(filter);

        var query = _dbContext.MigraineAttacks
            .AsNoTracking()
            .Where(x => x.UserId == userId);

        query = ApplyPeriodFilter(
            query,
            filter.From,
            filter.To,
            filter.FromDate,
            filter.ToDate);

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

        var totalCount = await query.CountAsync(cancellationToken);

        var skip = ((long)filter.Page - 1) * filter.PageSize;

        List<MigraineAttack> attacks;

        if (skip >= totalCount || skip > int.MaxValue)
        {
            attacks = new List<MigraineAttack>();
        }
        else
        {
            attacks = await query
                .Include(x => x.Symptoms)
                    .ThenInclude(x => x.Symptom)
                .Include(x => x.Triggers)
                    .ThenInclude(x => x.Trigger)
                .Include(x => x.Medications)
                    .ThenInclude(x => x.Medication)
                .AsSplitQuery()
                .OrderByDescending(x => x.StartDate)
                .ThenByDescending(x => x.StartAt.HasValue)
                .ThenByDescending(x => x.StartAt)
                .ThenByDescending(x => x.CreatedAt)
                .ThenBy(x => x.Id)
                .Skip((int)skip)
                .Take(filter.PageSize)
                .ToListAsync(cancellationToken);
        }

        var items = attacks
            .Select(MapToResponse)
            .ToList();

        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(
                totalCount / (double)filter.PageSize);

        return new PagedResponse<MigraineAttackResponse>
        {
            Items = items,
            Page = filter.Page,
            PageSize = filter.PageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }

    public async Task<MigraineAttackStatisticsResponse> GetStatisticsAsync(
        Guid userId,
        MigraineAttackStatisticsRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateStatisticsPeriod(request);

        var query = _dbContext.MigraineAttacks
            .AsNoTracking()
            .Where(x => x.UserId == userId);

        query = ApplyPeriodFilter(
            query,
            request.From,
            request.To,
            request.FromDate,
            request.ToDate);

        var attacks = await query
            .Select(x => new
            {
                x.Intensity,
                x.StartAt,
                x.EndAt
            })
            .ToListAsync(cancellationToken);

        if (attacks.Count == 0)
        {
            return new MigraineAttackStatisticsResponse
            {
                AttackCount = 0,
                AverageIntensity = 0,
                AverageDurationMinutes = null
            };
        }

        // Средняя продолжительность рассчитывается
        // только для приступов с обоими точными временами.
        var durations = attacks
            .Where(x =>
                x.StartAt.HasValue &&
                x.EndAt.HasValue)
            .Select(x =>
                (x.EndAt!.Value - x.StartAt!.Value).TotalMinutes)
            .ToList();

        var symptoms = await query
            .SelectMany(x => x.Symptoms)
            .GroupBy(x => new
            {
                x.SymptomId,
                x.Symptom.Name
            })
            .Select(group => new StatisticsItemResponse
            {
                Id = group.Key.SymptomId,
                Name = group.Key.Name,
                Count = group.Count()
            })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var triggers = await query
            .SelectMany(x => x.Triggers)
            .GroupBy(x => new
            {
                x.TriggerId,
                x.Trigger.Name
            })
            .Select(group => new StatisticsItemResponse
            {
                Id = group.Key.TriggerId,
                Name = group.Key.Name,
                Count = group.Count()
            })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var medications = await query
            .SelectMany(x => x.Medications)
            .GroupBy(x => new
            {
                x.MedicationId,
                x.Medication.Name
            })
            .Select(group => new StatisticsItemResponse
            {
                Id = group.Key.MedicationId,
                Name = group.Key.Name,

                Count = group
                    .Select(x => x.MigraineAttackId)
                    .Distinct()
                    .Count()
            })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return new MigraineAttackStatisticsResponse
        {
            AttackCount = attacks.Count,

            AverageIntensity = Math.Round(
                attacks.Average(x => x.Intensity),
                2),

            AverageDurationMinutes = durations.Count == 0
                ? null
                : Math.Round(durations.Average(), 2),

            Symptoms = symptoms,
            Triggers = triggers,
            Medications = medications
        };
    }

    public async Task<MigraineAttackResponse?> GetByIdAsync(
        Guid userId,
        Guid attackId,
        CancellationToken cancellationToken = default)
    {
        var attack = await _dbContext.MigraineAttacks
            .AsNoTracking()
            .Include(x => x.Symptoms)
                .ThenInclude(x => x.Symptom)
            .Include(x => x.Triggers)
                .ThenInclude(x => x.Trigger)
            .Include(x => x.Medications)
                .ThenInclude(x => x.Medication)
            .AsSplitQuery()
            .SingleOrDefaultAsync(
                x => x.Id == attackId && x.UserId == userId,
                cancellationToken);

        return attack is null
            ? null
            : MapToResponse(attack);
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
            .AsSplitQuery()
            .SingleOrDefaultAsync(
                x => x.Id == attackId && x.UserId == userId,
                cancellationToken);

        if (attack is null)
        {
            return null;
        }

        var period = NormalizeAttackPeriod(
            request.StartDate,
            request.EndDate,
            request.StartAt,
            request.EndAt);

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

        attack.StartDate = period.StartDate;
        attack.EndDate = period.EndDate;

        attack.StartAt = period.StartAt;
        attack.EndAt = period.EndAt;

        attack.Intensity = request.Intensity;
        attack.PainLocation = request.PainLocation!.Value;

        attack.PainLocationDescription =
            NormalizeText(request.PainLocationDescription);

        attack.Notes = NormalizeText(request.Notes);

        // Обновление симптомов.
        var symptomsToRemove = attack.Symptoms
            .Where(x =>
                !requestedSymptoms.ContainsKey(x.SymptomId))
            .ToList();

        _dbContext.AttackSymptoms.RemoveRange(symptomsToRemove);

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
                existingSymptom.Description = requestedSymptom.Value;
            }
        }

        // Обновление триггеров.
        var triggersToRemove = attack.Triggers
            .Where(x =>
                !requestedTriggers.ContainsKey(x.TriggerId))
            .ToList();

        _dbContext.AttackTriggers.RemoveRange(triggersToRemove);

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
                existingTrigger.Description = requestedTrigger.Value;
            }
        }

        // Обновление препаратов.
        await _dbContext.AttackMedications
            .Where(x => x.MigraineAttackId == attackId)
            .ExecuteDeleteAsync(cancellationToken);

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

        await _dbContext.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

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
                x => x.Id == attackId && x.UserId == userId,
                cancellationToken);

        if (attack is null)
        {
            return false;
        }

        _dbContext.MigraineAttacks.Remove(attack);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    private static MigraineAttackResponse MapToResponse(
        MigraineAttack attack)
    {
        return new MigraineAttackResponse
        {
            Id = attack.Id,

            StartDate = attack.StartDate,
            EndDate = attack.EndDate,

            StartAt = attack.StartAt,
            EndAt = attack.EndAt,

            DurationMinutes =
                attack.StartAt.HasValue &&
                attack.EndAt.HasValue
                    ? (attack.EndAt.Value - attack.StartAt.Value)
                        .TotalMinutes
                    : null,

            DurationDays =
                attack.EndDate.DayNumber -
                attack.StartDate.DayNumber + 1,

            Intensity = attack.Intensity,
            PainLocation = attack.PainLocation,

            PainLocationDescription =
                attack.PainLocationDescription,

            Notes = attack.Notes,
            CreatedAt = attack.CreatedAt,

            Symptoms = attack.Symptoms
                .OrderBy(x => x.Symptom.Name)
                .Select(x => new AttackSymptomResponse
                {
                    SymptomId = x.SymptomId,
                    Name = x.Symptom.Name,
                    Description = x.Description
                })
                .ToList(),

            Triggers = attack.Triggers
                .OrderBy(x => x.Trigger.Name)
                .Select(x => new AttackTriggerResponse
                {
                    TriggerId = x.TriggerId,
                    Name = x.Trigger.Name,
                    Description = x.Description
                })
                .ToList(),

            Medications = attack.Medications
                .OrderBy(x => x.TakenAt)
                .ThenBy(x => x.Medication.Name)
                .Select(x => new AttackMedicationResponse
                {
                    Id = x.Id,
                    MedicationId = x.MedicationId,
                    Name = x.Medication.Name,
                    Dosage = x.Dosage,
                    TakenAt = x.TakenAt
                })
                .ToList()
        };
    }

    private static NormalizedAttackPeriod NormalizeAttackPeriod(
        DateOnly? startDate,
        DateOnly? endDate,
        DateTimeOffset? startAt,
        DateTimeOffset? endAt)
    {
        // New requests must specify both dates. Legacy requests may
        // omit both dates if both exact timestamps are supplied.
        if (startDate.HasValue != endDate.HasValue)
        {
            throw new ArgumentException(
                "Дата начала и дата окончания приступа должны быть указаны вместе.");
        }

        var normalizedStartAt = startAt?.ToUniversalTime();
        var normalizedEndAt = endAt?.ToUniversalTime();

        // Совместимость со старыми клиентами:
        // если календарные даты не переданы,
        // извлекаем их из UTC-времени.
        var resolvedStartDate = startDate ??
            (normalizedStartAt.HasValue
                ? DateOnly.FromDateTime(
                    normalizedStartAt.Value.UtcDateTime)
                : null);

        var resolvedEndDate = endDate ??
            (normalizedEndAt.HasValue
                ? DateOnly.FromDateTime(
                    normalizedEndAt.Value.UtcDateTime)
                : null);

        if (!resolvedStartDate.HasValue ||
            !resolvedEndDate.HasValue)
        {
            throw new ArgumentException(
                "Дата начала и дата окончания приступа обязательны.");
        }

        if (resolvedEndDate.Value < resolvedStartDate.Value)
        {
            throw new ArgumentException(
                "Дата окончания приступа не может быть раньше даты начала.");
        }

        if (normalizedStartAt.HasValue &&
            normalizedEndAt.HasValue &&
            normalizedEndAt.Value < normalizedStartAt.Value)
        {
            throw new ArgumentException(
                "Время окончания приступа не может быть раньше времени начала.");
        }

        return new NormalizedAttackPeriod(
            resolvedStartDate.Value,
            resolvedEndDate.Value,
            normalizedStartAt,
            normalizedEndAt);
    }

    private static string? NormalizeText(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static IQueryable<MigraineAttack> ApplyPeriodFilter(
        IQueryable<MigraineAttack> query,
        DateTimeOffset? from,
        DateTimeOffset? to,
        DateOnly? fromDate,
        DateOnly? toDate)
    {
        if (fromDate.HasValue || toDate.HasValue)
        {
            // Calendar filters use inclusive interval overlap.
            if (fromDate.HasValue)
            {
                var lowerDate = fromDate.Value;
                query = query.Where(x => x.EndDate >= lowerDate);
            }

            if (toDate.HasValue)
            {
                var upperDate = toDate.Value;
                query = query.Where(x => x.StartDate <= upperDate);
            }

            return query;
        }

        // Legacy timestamp filters retain their original start-time
        // semantics. If start time is unknown, use the UTC calendar date.
        if (from.HasValue)
        {
            var lower = from.Value.ToUniversalTime();
            var lowerDate = DateOnly.FromDateTime(lower.UtcDateTime);

            query = query.Where(x =>
                x.StartAt.HasValue
                    ? x.StartAt.Value >= lower
                    : x.StartDate >= lowerDate);
        }

        if (to.HasValue)
        {
            var upper = to.Value.ToUniversalTime();
            var upperDate = DateOnly.FromDateTime(upper.UtcDateTime);

            query = query.Where(x =>
                x.StartAt.HasValue
                    ? x.StartAt.Value <= upper
                    : x.StartDate <= upperDate);
        }

        return query;
    }

    private static void ValidatePeriod(
        DateTimeOffset? from,
        DateTimeOffset? to,
        DateOnly? fromDate,
        DateOnly? toDate)
    {
        if ((from.HasValue || to.HasValue) &&
            (fromDate.HasValue || toDate.HasValue))
        {
            throw new ArgumentException(
                "Нельзя одновременно использовать фильтры from/to и fromDate/toDate.");
        }

        if ((from.HasValue && to.HasValue && to.Value < from.Value) ||
            (fromDate.HasValue && toDate.HasValue && toDate.Value < fromDate.Value))
        {
            throw new ArgumentException(
                "Конец периода не может быть раньше начала периода.");
        }
    }

    private static void ValidateStatisticsPeriod(
        MigraineAttackStatisticsRequest request)
    {
        ValidatePeriod(
            request.From,
            request.To,
            request.FromDate,
            request.ToDate);
    }

    private static void ValidateFilters(
        MigraineAttackFilterRequest filter)
    {
        ValidatePeriod(
            filter.From,
            filter.To,
            filter.FromDate,
            filter.ToDate);

        if (filter.MinIntensity.HasValue &&
            filter.MaxIntensity.HasValue &&
            filter.MaxIntensity.Value < filter.MinIntensity.Value)
        {
            throw new ArgumentException(
                "Максимальная интенсивность не может быть меньше минимальной.");
        }

        if (filter.SymptomId == Guid.Empty)
        {
            throw new ArgumentException(
                "Идентификатор симптома не может быть пустым.");
        }

        if (filter.TriggerId == Guid.Empty)
        {
            throw new ArgumentException(
                "Идентификатор триггера не может быть пустым.");
        }

        if (filter.MedicationId == Guid.Empty)
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
            x => NormalizeText(x.Description));
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
            x => NormalizeText(x.Description));
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

        if (medications.Any(x => x.MedicationId == Guid.Empty))
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
            .Select(x => new NormalizedAttackMedication(
                x.MedicationId,
                NormalizeText(x.Dosage),
                x.TakenAt?.ToUniversalTime()))
            .ToList();
    }

    private sealed record NormalizedAttackPeriod(
        DateOnly StartDate,
        DateOnly EndDate,
        DateTimeOffset? StartAt,
        DateTimeOffset? EndAt);

    private sealed record NormalizedAttackMedication(
        Guid MedicationId,
        string? Dosage,
        DateTimeOffset? TakenAt);
}
