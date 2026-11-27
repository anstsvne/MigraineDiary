using MigraineDiary.Api.Contracts.Common;
using MigraineDiary.Api.Contracts.MigraineAttacks;
using MigraineDiary.Api.Contracts.VitalMeasurements;
using MigraineDiary.Api.Data;
using MigraineDiary.Api.Entities;
using MigraineDiary.Api.Enums;
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

        if (medications.Any(x => x.Id.HasValue))
        {
            throw new ArgumentException("New medication intakes must not specify an existing intake ID.");
        }

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
                    TakenAt = medication.TakenAt,
                    Effectiveness = medication.Effectiveness
                        ?? MedicationEffectiveness.NotAssessed
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

        if (filter.MinDurationMinutes.HasValue || filter.MaxDurationMinutes.HasValue)
        {
            query = query.Where(x => x.StartAt.HasValue && x.EndAt.HasValue);

            if (filter.MinDurationMinutes is double minDuration)
            {
                query = query.Where(x =>
                    (x.EndAt!.Value - x.StartAt!.Value).TotalMinutes >= minDuration);
            }

            if (filter.MaxDurationMinutes is double maxDuration)
            {
                query = query.Where(x =>
                    (x.EndAt!.Value - x.StartAt!.Value).TotalMinutes <= maxDuration);
            }
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
            attacks = await ApplySorting(query, filter)
                .Include(x => x.Symptoms)
                    .ThenInclude(x => x.Symptom)
                .Include(x => x.Triggers)
                    .ThenInclude(x => x.Trigger)
                .Include(x => x.Medications)
                    .ThenInclude(x => x.Medication)
                .AsSplitQuery()

                .Skip((int)skip)
                .Take(filter.PageSize)
                .ToListAsync(cancellationToken);
        }

        var items = new List<MigraineAttackResponse>();

        foreach (var attack in attacks)
        {
            items.Add(MapToResponse(attack));
        }

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

    private static IOrderedQueryable<MigraineAttack> ApplySorting(
        IQueryable<MigraineAttack> query,
        MigraineAttackFilterRequest filter)
    {
        var descending = string.Equals(filter.SortDirection.Trim(), "desc", StringComparison.OrdinalIgnoreCase);
        var sortBy = filter.SortBy.Trim().ToLowerInvariant();

        if (sortBy == "date")
        {
            return descending
                ? query.OrderByDescending(x => x.StartDate)
                    .ThenByDescending(x => x.StartAt.HasValue)
                    .ThenByDescending(x => x.StartAt)
                    .ThenByDescending(x => x.CreatedAt)
                    .ThenBy(x => x.Id)
                : query.OrderBy(x => x.StartDate)
                    .ThenByDescending(x => x.StartAt.HasValue)
                    .ThenBy(x => x.StartAt)
                    .ThenByDescending(x => x.CreatedAt)
                    .ThenBy(x => x.Id);
        }

        IOrderedQueryable<MigraineAttack> ordered;
        if (sortBy == "intensity")
        {
            ordered = descending
                ? query.OrderByDescending(x => x.Intensity)
                : query.OrderBy(x => x.Intensity);
        }
        else
        {
            // The nullable timestamp difference is an interval in PostgreSQL.
            // Unknown durations always follow known durations.
            var knownFirst = query.OrderByDescending(x =>
                x.StartAt.HasValue && x.EndAt.HasValue);

            ordered = descending
                ? knownFirst.ThenByDescending(x => x.EndAt - x.StartAt)
                : knownFirst.ThenBy(x => x.EndAt - x.StartAt);
        }

        return ordered
            .ThenByDescending(x => x.StartDate)
            .ThenByDescending(x => x.StartAt.HasValue)
            .ThenByDescending(x => x.StartAt)
            .ThenByDescending(x => x.CreatedAt)
            .ThenBy(x => x.Id);
    }

    private const int MaxCalendarDays = 366;

    public async Task<MigraineAttackCalendarResponse> GetCalendarAsync(
        Guid userId,
        MigraineAttackCalendarRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.From is not DateOnly from || request.To is not DateOnly to)
        {
            throw new ArgumentException("Необходимо указать начало и конец периода.");
        }

        if (to < from)
        {
            throw new ArgumentException("Конец периода не может быть раньше начала периода.");
        }

        if (to.DayNumber - from.DayNumber + 1 > MaxCalendarDays)
        {
            throw new ArgumentException("Период календаря не может превышать 366 дней.");
        }

        var attacks = await _dbContext.MigraineAttacks
            .AsNoTracking()
            .Where(x => x.UserId == userId &&
                        x.StartDate <= to && x.EndDate >= from)
            .Select(x => new { x.Id, x.StartDate, x.EndDate, x.Intensity })
            .ToListAsync(cancellationToken);

        var days = new SortedDictionary<DateOnly, (int MaxIntensity, List<Guid> AttackIds)>();

        foreach (var attack in attacks)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var firstDay = Math.Max(attack.StartDate.DayNumber, from.DayNumber);
            var lastDay = Math.Min(attack.EndDate.DayNumber, to.DayNumber);

            for (var dayNumber = firstDay; dayNumber <= lastDay; dayNumber++)
            {
                var date = DateOnly.FromDayNumber(dayNumber);

                if (!days.TryGetValue(date, out var day))
                {
                    day = (attack.Intensity, new List<Guid>());
                }

                day.MaxIntensity = Math.Max(day.MaxIntensity, attack.Intensity);
                day.AttackIds.Add(attack.Id);
                days[date] = day;
            }
        }

        return new MigraineAttackCalendarResponse
        {
            From = from,
            To = to,
            Days = days.Select(x => new MigraineAttackCalendarDayResponse
            {
                Date = x.Key,
                AttackCount = x.Value.AttackIds.Count,
                MaxIntensity = x.Value.MaxIntensity,
                AttackIds = x.Value.AttackIds.OrderBy(id => id).ToArray()
            }).ToArray()
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
                x.StartDate,
                x.EndDate,
                x.StartAt,
                x.EndAt
            })
            .ToListAsync(cancellationToken);

        var intensityCounts = attacks
            .GroupBy(x => x.Intensity)
            .ToDictionary(group => group.Key, group => group.Count());

        var intensityDistribution = Enumerable.Range(1, 10)
            .Select(intensity => new IntensityDistributionItemResponse
            {
                Intensity = intensity,
                Count = intensityCounts.GetValueOrDefault(intensity)
            })
            .ToArray();

        if (attacks.Count == 0)
        {
            return new MigraineAttackStatisticsResponse
            {
                AttackCount = 0,
                IntensityDistribution = intensityDistribution,
                MigraineDaysCount = 0,
                AverageIntensity = 0,
                MinIntensity = null,
                MaxIntensity = null,
                AverageDurationMinutes = null,
                MaxDurationMinutes = null,

                KnownDurationAttackCount = 0,
                UnknownDurationAttackCount = 0,

                MedicationEffectiveness =
                    Array.Empty<MedicationEffectivenessStatisticsItemResponse>()
            };
        }

        // Продолжительность известна только тогда,
        // когда указаны оба точных времени.
        var durations = attacks
            .Where(x =>
                x.StartAt.HasValue &&
                x.EndAt.HasValue)
            .Select(x =>
                (x.EndAt!.Value - x.StartAt!.Value).TotalMinutes)
            .ToList();

        // Подсчёт уникальных календарных дней мигрени.
        var migraineDaysCount = CountUniqueMigraineDays(
            attacks.Select(x => (
                StartDate: request.FromDate.HasValue &&
                           x.StartDate < request.FromDate.Value
                    ? request.FromDate.Value
                    : x.StartDate,

                EndDate: request.ToDate.HasValue &&
                         x.EndDate > request.ToDate.Value
                    ? request.ToDate.Value
                    : x.EndDate
            )));

        // Daily trend counts attack starts once. Calendar overlap can include
        // attacks that began before the requested range; do not plot those
        // starts outside the calendar range or assign them to another date.
        var dailyTrend = attacks
            .Where(x => (!request.FromDate.HasValue || x.StartDate >= request.FromDate.Value) &&
                        (!request.ToDate.HasValue || x.StartDate <= request.ToDate.Value))
            .GroupBy(x => x.StartDate)
            .OrderBy(group => group.Key)
            .Select(group =>
            {
                var dailyDurations = group
                    .Where(x => x.StartAt.HasValue && x.EndAt.HasValue)
                    .Select(x => (x.EndAt!.Value - x.StartAt!.Value).TotalMinutes)
                    .ToArray();

                return new MigraineAttackDailyTrendItemResponse
                {
                    Date = group.Key,
                    AttackCount = group.Count(),
                    AverageIntensity = Math.Round(group.Average(x => x.Intensity), 2),
                    MaxIntensity = group.Max(x => x.Intensity),
                    KnownDurationAttackCount = dailyDurations.Length,
                    AverageDurationMinutes = dailyDurations.Length == 0
                        ? null
                        : Math.Round(dailyDurations.Average(), 2),
                    MaxDurationMinutes = dailyDurations.Length == 0
                        ? null
                        : Math.Round(dailyDurations.Max(), 2)
                };
            })
            .ToArray();

        // Группировка симптомов.
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

        // Группировка триггеров.
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

        // Группировка лекарств.
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
        // Группировка эффективности применения препаратов.
        // Учитываются только записи из выбранного периода.

        var medicationEffectiveness = await query
            .SelectMany(x => x.Medications)
            .GroupBy(x => new
            {
                x.MedicationId,
                x.Medication.Name,
                x.Effectiveness
            })
            .Select(group => new MedicationEffectivenessStatisticsItemResponse
            {
                MedicationId = group.Key.MedicationId,

                MedicationName = group.Key.Name,

                Effectiveness = group.Key.Effectiveness,

                // Количество записей о применении препарата
                // с соответствующим результатом.
                Count = group.Count()
            })
            .OrderBy(x => x.MedicationName)
            .ThenBy(x => x.Effectiveness)
            .ToListAsync(cancellationToken);

        return new MigraineAttackStatisticsResponse
        {
            AttackCount = attacks.Count,

            MigraineDaysCount = migraineDaysCount,
            IntensityDistribution = intensityDistribution,
            DailyTrend = dailyTrend,

            AverageIntensity = Math.Round(
                attacks.Average(x => x.Intensity),
                2),

            MinIntensity = attacks.Min(x => x.Intensity),

            MaxIntensity = attacks.Max(x => x.Intensity),

            AverageDurationMinutes = durations.Count == 0
                ? null
                : Math.Round(durations.Average(), 2),

            MaxDurationMinutes = durations.Count == 0
                ? null
                : Math.Round(durations.Max(), 2),

            KnownDurationAttackCount = durations.Count,

            UnknownDurationAttackCount = attacks.Count - durations.Count,

            Symptoms = symptoms,
            Triggers = triggers,
            Medications = medications,

            MedicationEffectiveness = medicationEffectiveness
        };
    }


    private static int CountUniqueMigraineDays(
        IEnumerable<(DateOnly StartDate, DateOnly EndDate)> intervals)
    {
        var orderedIntervals = intervals
            .OrderBy(x => x.StartDate)
            .ThenBy(x => x.EndDate)
            .ToList();

        if (orderedIntervals.Count == 0)
        {
            return 0;
        }

        var currentStart = orderedIntervals[0].StartDate;
        var currentEnd = orderedIntervals[0].EndDate;

        var totalDays = 0;

        foreach (var interval in orderedIntervals.Skip(1))
        {
            // Если интервалы пересекаются,
            // объединяем их.
            if (interval.StartDate <= currentEnd)
            {
                if (interval.EndDate > currentEnd)
                {
                    currentEnd = interval.EndDate;
                }
            }
            else
            {
                totalDays +=
                    currentEnd.DayNumber -
                    currentStart.DayNumber + 1;

                currentStart = interval.StartDate;
                currentEnd = interval.EndDate;
            }
        }

        totalDays +=
            currentEnd.DayNumber -
            currentStart.DayNumber + 1;

        return totalDays;
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
            .Include(x => x.VitalMeasurements.Where(m => m.UserId == userId))
            .AsSplitQuery()
            .SingleOrDefaultAsync(
                x => x.Id == attackId && x.UserId == userId,
                cancellationToken);

        return attack is null
            ? null
            : MapToResponse(
                attack,
                includeVitalMeasurements: true);
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
        await UpdateAttackMedicationsAsync(
            attackId, requestedMedications, cancellationToken);

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
        MigraineAttack attack,
        bool includeVitalMeasurements = false)
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

            VitalMeasurements = includeVitalMeasurements
                ? attack.VitalMeasurements
                    .OrderByDescending(x => x.MeasuredAt)
                    .ThenBy(x => x.Id)
                    .Select(x => new VitalMeasurementResponse
                    {
                        Id = x.Id,
                        MigraineAttackId = x.MigraineAttackId,
                        BodyPosition = x.BodyPosition,
                        SystolicPressure = x.SystolicPressure,
                        DiastolicPressure = x.DiastolicPressure,
                        Pulse = x.Pulse,
                        MeasuredAt = x.MeasuredAt,
                        Notes = x.Notes,
                        CreatedAt = x.CreatedAt
                    })
                    .ToArray()
                : null,

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
                    TakenAt = x.TakenAt,
                    Effectiveness = x.Effectiveness
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

        if ((filter.MinDurationMinutes is double min && (!double.IsFinite(min) || min < 0)) ||
            (filter.MaxDurationMinutes is double max && (!double.IsFinite(max) || max < 0)))
        {
            throw new ArgumentException("Duration limits must be finite non-negative numbers.");
        }

        if (filter.MinDurationMinutes.HasValue && filter.MaxDurationMinutes.HasValue &&
            filter.MinDurationMinutes.Value > filter.MaxDurationMinutes.Value)
        {
            throw new ArgumentException("Maximum duration must not be less than minimum duration.");
        }

        if (filter.SortBy?.Trim().ToLowerInvariant() is not ("date" or "intensity" or "duration"))
        {
            throw new ArgumentException("sortBy must be date, intensity or duration.");
        }

        if (filter.SortDirection?.Trim().ToLowerInvariant() is not ("asc" or "desc"))
        {
            throw new ArgumentException("sortDirection must be asc or desc.");
        }

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

        if (medications.Any(x => x.Effectiveness.HasValue &&
            !Enum.IsDefined(x.Effectiveness.Value)))
        {
            throw new ArgumentException("Invalid medication effectiveness.");
        }

        if (medications.Any(x => x.Id == Guid.Empty))
        {
            throw new ArgumentException("Medication intake ID must not be empty.");
        }

        var intakeIds = medications.Where(x => x.Id.HasValue)
            .Select(x => x.Id!.Value).ToArray();

        if (intakeIds.Distinct().Count() != intakeIds.Length)
        {
            throw new ArgumentException("Duplicate medication intake ID.");
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
                x.Id,
                x.MedicationId,
                NormalizeText(x.Dosage),
                x.TakenAt?.ToUniversalTime(),
                x.Effectiveness))
            .ToList();
    }

    private async Task UpdateAttackMedicationsAsync(
        Guid attackId,
        IReadOnlyList<NormalizedAttackMedication> requested,
        CancellationToken cancellationToken)
    {
        var existing = await _dbContext.AttackMedications
            .Where(x => x.MigraineAttackId == attackId)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

        var byId = existing.ToDictionary(x => x.Id);
        var reserved = requested.Where(x => x.Id.HasValue)
            .Select(x => x.Id!.Value).ToHashSet();
        var retained = new HashSet<Guid>();

        // Explicit IDs must belong to the current attack and medication.
        foreach (var item in requested.Where(x => x.Id.HasValue))
        {
            if (!byId.TryGetValue(item.Id!.Value, out var intake) ||
                intake.MedicationId != item.MedicationId)
            {
                throw new ArgumentException("Medication intake not found or unavailable.");
            }
        }

        foreach (var item in requested)
        {
            AttackMedication? intake;

            if (item.Id.HasValue)
            {
                intake = byId[item.Id.Value];
            }
            else
            {
                // Legacy clients do not send intake IDs. Match unchanged
                // intakes by medication, normalized dosage and time.
                var matches = existing.Where(x =>
                    !reserved.Contains(x.Id) && !retained.Contains(x.Id) &&
                    x.MedicationId == item.MedicationId &&
                    x.Dosage == item.Dosage && x.TakenAt == item.TakenAt)
                    .ToList();

                if (!item.Effectiveness.HasValue &&
                    matches.Select(x => x.Effectiveness).Distinct().Count() > 1)
                {
                    throw new ArgumentException("Ambiguous medication intake; specify its ID.");
                }

                intake = matches.FirstOrDefault();
            }

            if (intake is null)
            {
                _dbContext.AttackMedications.Add(new AttackMedication
                {
                    MigraineAttackId = attackId,
                    MedicationId = item.MedicationId,
                    Dosage = item.Dosage,
                    TakenAt = item.TakenAt,
                    Effectiveness = item.Effectiveness
                        ?? MedicationEffectiveness.NotAssessed
                });
                continue;
            }

            retained.Add(intake.Id);
            intake.Dosage = item.Dosage;
            intake.TakenAt = item.TakenAt;
            if (item.Effectiveness.HasValue)
            {
                intake.Effectiveness = item.Effectiveness.Value;
            }
        }

        _dbContext.AttackMedications.RemoveRange(
            existing.Where(x => !retained.Contains(x.Id)));
    }

    private sealed record NormalizedAttackPeriod(
        DateOnly StartDate,
        DateOnly EndDate,
        DateTimeOffset? StartAt,
        DateTimeOffset? EndAt);

    private sealed record NormalizedAttackMedication(
        Guid? Id,
        Guid MedicationId,
        string? Dosage,
        DateTimeOffset? TakenAt,
        MedicationEffectiveness? Effectiveness);
}
