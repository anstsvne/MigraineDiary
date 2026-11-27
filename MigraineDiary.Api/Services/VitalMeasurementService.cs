using MigraineDiary.Api.Contracts.Common;
using MigraineDiary.Api.Contracts.VitalMeasurements;
using MigraineDiary.Api.Data;
using MigraineDiary.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace MigraineDiary.Api.Services;

public sealed class VitalMeasurementService
{
    private readonly AppDbContext _dbContext;

    public VitalMeasurementService(
        AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResponse<VitalMeasurementResponse>> GetAllAsync(
        Guid userId,
        VitalMeasurementFilterRequest filter,
        CancellationToken cancellationToken = default)
    {
        ValidateFilters(filter);

        var query = _dbContext.VitalMeasurements
            .AsNoTracking()
            .Where(x => x.UserId == userId);

        query = ApplyPeriodFilter(query, filter.From, filter.To, filter.FromDate, filter.ToDate);

        if (filter.MinSystolicPressure is int minSystolic)
            query = query.Where(x => x.SystolicPressure >= minSystolic);
        if (filter.MaxSystolicPressure is int maxSystolic)
            query = query.Where(x => x.SystolicPressure <= maxSystolic);
        if (filter.MinDiastolicPressure is int minDiastolic)
            query = query.Where(x => x.DiastolicPressure >= minDiastolic);
        if (filter.MaxDiastolicPressure is int maxDiastolic)
            query = query.Where(x => x.DiastolicPressure <= maxDiastolic);
        if (filter.MinPulse is int minPulse)
            query = query.Where(x => x.Pulse >= minPulse);
        if (filter.MaxPulse is int maxPulse)
            query = query.Where(x => x.Pulse <= maxPulse);
        if (filter.MigraineAttackId is Guid attackId)
            query = query.Where(x => x.MigraineAttackId == attackId);
        if (filter.HasMigraineAttack is bool linked)
            query = query.Where(x => x.MigraineAttackId.HasValue == linked);
        if (filter.BodyPosition.HasValue)
        {
            var position = filter.BodyPosition.Value;
            query = query.Where(x => x.BodyPosition == position);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var skip = ((long)filter.Page - 1) * filter.PageSize;
        IReadOnlyList<VitalMeasurementResponse> items = Array.Empty<VitalMeasurementResponse>();

        if (skip < totalCount && skip <= int.MaxValue)
        {
            items = await ApplySorting(query, filter)
                .Skip((int)skip)
                .Take(filter.PageSize)
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
                .ToListAsync(cancellationToken);
        }

        return new PagedResponse<VitalMeasurementResponse>
        {
            Items = items,
            Page = filter.Page,
            PageSize = filter.PageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)filter.PageSize)
        };
    }

    private static void ValidateFilters(VitalMeasurementFilterRequest filter)
    {
        if (filter.MinSystolicPressure > filter.MaxSystolicPressure ||
            filter.MinDiastolicPressure > filter.MaxDiastolicPressure ||
            filter.MinPulse > filter.MaxPulse)
            throw new ArgumentException("Minimum value cannot exceed maximum value.");

        if (filter.MigraineAttackId == Guid.Empty)
            throw new ArgumentException("Migraine attack ID cannot be empty.");

        if (filter.MigraineAttackId.HasValue && filter.HasMigraineAttack == false)
            throw new ArgumentException("Migraine attack ID cannot be combined with hasMigraineAttack=false.");

        if (filter.BodyPosition.HasValue && !Enum.IsDefined(filter.BodyPosition.Value))
            throw new ArgumentException("Invalid body position.");

        if (filter.SortBy?.ToLowerInvariant() is not ("date" or "systolicpressure" or "diastolicpressure" or "pulse"))
            throw new ArgumentException("Invalid sortBy value.");

        if (filter.SortDirection?.ToLowerInvariant() is not ("asc" or "desc"))
            throw new ArgumentException("Invalid sortDirection value.");
    }

    private static IOrderedQueryable<VitalMeasurement> ApplySorting(
        IQueryable<VitalMeasurement> query,
        VitalMeasurementFilterRequest filter)
    {
        var ascending = filter.SortDirection.Equals("asc", StringComparison.OrdinalIgnoreCase);
        var sortBy = filter.SortBy.ToLowerInvariant();

        if (sortBy == "date")
        {
            return (ascending ? query.OrderBy(x => x.MeasuredAt) : query.OrderByDescending(x => x.MeasuredAt))
                .ThenBy(x => x.Id);
        }

        var ordered = sortBy switch
        {
            "systolicpressure" => ascending ? query.OrderBy(x => x.SystolicPressure) : query.OrderByDescending(x => x.SystolicPressure),
            "diastolicpressure" => ascending ? query.OrderBy(x => x.DiastolicPressure) : query.OrderByDescending(x => x.DiastolicPressure),
            "pulse" => ascending ? query.OrderBy(x => x.Pulse) : query.OrderByDescending(x => x.Pulse),
            _ => throw new ArgumentException("Invalid sortBy value.")
        };

        return ordered.ThenByDescending(x => x.MeasuredAt).ThenBy(x => x.Id);
    }

    private static IQueryable<VitalMeasurement> ApplyPeriodFilter(
        IQueryable<VitalMeasurement> query,
        DateTimeOffset? lowerTime,
        DateTimeOffset? upperTime,
        DateOnly? lowerDate,
        DateOnly? upperDate)
    {
        if ((lowerTime.HasValue || upperTime.HasValue) &&
            (lowerDate.HasValue || upperDate.HasValue))
        {
            throw new ArgumentException(
                "Timestamp and calendar date filters cannot be combined.");
        }

        if ((lowerTime.HasValue && upperTime.HasValue &&
             lowerTime.Value > upperTime.Value) ||
            (lowerDate.HasValue && upperDate.HasValue &&
             lowerDate.Value > upperDate.Value))
        {
            throw new ArgumentException(
                "The period end cannot be earlier than the period start.");
        }



        if (lowerTime.HasValue)
        {
            var from = lowerTime.Value.ToUniversalTime();
            query = query.Where(x => x.MeasuredAt >= from);
        }

        if (upperTime.HasValue)
        {
            var to = upperTime.Value.ToUniversalTime();
            query = query.Where(x => x.MeasuredAt <= to);
        }

        if (lowerDate.HasValue)
        {
            var from = new DateTimeOffset(
                lowerDate.Value.ToDateTime(TimeOnly.MinValue),
                TimeSpan.Zero);
            query = query.Where(x => x.MeasuredAt >= from);
        }

        // Exclusive midnight of the following UTC date includes the entire
        // requested final day, including fractional seconds.
        if (upperDate.HasValue && upperDate.Value != DateOnly.MaxValue)
        {
            var toExclusive = new DateTimeOffset(
                upperDate.Value.AddDays(1).ToDateTime(TimeOnly.MinValue),
                TimeSpan.Zero);
            query = query.Where(x => x.MeasuredAt < toExclusive);
        }

        return query;
    }

    public async Task<VitalMeasurementStatisticsResponse> GetStatisticsAsync(
        Guid userId,
        VitalMeasurementStatisticsRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.VitalMeasurements
            .AsNoTracking()
            .Where(x => x.UserId == userId);

        query = ApplyPeriodFilter(query, request.From, request.To, request.FromDate, request.ToDate);

        var result = await query
            .GroupBy(x => 1)
            .Select(group => new
            {
                MeasurementCount = group.Count(),
                AverageSystolicPressure = group.Average(x => x.SystolicPressure),
                MinSystolicPressure = group.Min(x => x.SystolicPressure),
                MaxSystolicPressure = group.Max(x => x.SystolicPressure),
                AverageDiastolicPressure = group.Average(x => x.DiastolicPressure),
                MinDiastolicPressure = group.Min(x => x.DiastolicPressure),
                MaxDiastolicPressure = group.Max(x => x.DiastolicPressure),
                AveragePulse = group.Average(x => x.Pulse),
                MinPulse = group.Min(x => x.Pulse),
                MaxPulse = group.Max(x => x.Pulse)
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (result is null)
        {
            return new VitalMeasurementStatisticsResponse();
        }

        return new VitalMeasurementStatisticsResponse
        {
            MeasurementCount = result.MeasurementCount,
            AverageSystolicPressure = Math.Round(result.AverageSystolicPressure, 2),
            MinSystolicPressure = result.MinSystolicPressure,
            MaxSystolicPressure = result.MaxSystolicPressure,
            AverageDiastolicPressure = Math.Round(result.AverageDiastolicPressure, 2),
            MinDiastolicPressure = result.MinDiastolicPressure,
            MaxDiastolicPressure = result.MaxDiastolicPressure,
            AveragePulse = Math.Round(result.AveragePulse, 2),
            MinPulse = result.MinPulse,
            MaxPulse = result.MaxPulse
        };
    }

    public async Task<VitalMeasurementResponse?> GetByIdAsync(
        Guid userId,
        Guid measurementId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.VitalMeasurements
            .AsNoTracking()
            .Where(x =>
                x.Id == measurementId &&
                x.UserId == userId)
            .Select(x =>
                new VitalMeasurementResponse
                {
                    Id = x.Id,
                    MigraineAttackId = x.MigraineAttackId,
                    BodyPosition = x.BodyPosition,
                    SystolicPressure =
                        x.SystolicPressure,
                    DiastolicPressure =
                        x.DiastolicPressure,
                    Pulse = x.Pulse,
                    MeasuredAt = x.MeasuredAt,
                    Notes = x.Notes,
                    CreatedAt = x.CreatedAt
                })
            .SingleOrDefaultAsync(
                cancellationToken);
    }

    public async Task<VitalMeasurementResponse> CreateAsync(
        Guid userId,
        CreateVitalMeasurementRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.MeasuredAt is null)
        {
            throw new ArgumentException(
                "Measurement date and time are required.");
        }

        await ValidateAttackOwnershipAsync(
            userId,
            request.MigraineAttackId,
            cancellationToken);

        var measurement =
            new VitalMeasurement
            {
                UserId = userId,

                MigraineAttackId = request.MigraineAttackId,
                BodyPosition = request.BodyPosition,

                SystolicPressure =
                    request.SystolicPressure,

                DiastolicPressure =
                    request.DiastolicPressure,

                Pulse =
                    request.Pulse,

                MeasuredAt =
                    request.MeasuredAt.Value
                        .ToUniversalTime(),

                Notes =
                    NormalizeNotes(
                        request.Notes)
            };

        _dbContext.VitalMeasurements.Add(
            measurement);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return MapToResponse(
            measurement);
    }

    public async Task<VitalMeasurementResponse?> UpdateAsync(
        Guid userId,
        Guid measurementId,
        UpdateVitalMeasurementRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.MeasuredAt is null)
        {
            throw new ArgumentException(
                "Measurement date and time are required.");
        }

        var measurement =
            await _dbContext.VitalMeasurements
                .SingleOrDefaultAsync(
                    x =>
                        x.Id == measurementId &&
                        x.UserId == userId,
                    cancellationToken);

        if (measurement is null)
        {
            return null;
        }

        if (request.HasMigraineAttackId)
        {
            await ValidateAttackOwnershipAsync(
                userId,
                request.MigraineAttackId,
                cancellationToken);

            measurement.MigraineAttackId =
                request.MigraineAttackId;
        }

        if (request.HasBodyPosition)
        {
            measurement.BodyPosition =
                request.BodyPosition;
        }

        measurement.SystolicPressure =
            request.SystolicPressure;

        measurement.DiastolicPressure =
            request.DiastolicPressure;

        measurement.Pulse =
            request.Pulse;

        measurement.MeasuredAt =
            request.MeasuredAt.Value
                .ToUniversalTime();

        measurement.Notes =
            NormalizeNotes(
                request.Notes);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return MapToResponse(
            measurement);
    }

    public async Task<bool> DeleteAsync(
        Guid userId,
        Guid measurementId,
        CancellationToken cancellationToken = default)
    {
        var measurement =
            await _dbContext.VitalMeasurements
                .SingleOrDefaultAsync(
                    x =>
                        x.Id == measurementId &&
                        x.UserId == userId,
                    cancellationToken);

        if (measurement is null)
        {
            return false;
        }

        _dbContext.VitalMeasurements.Remove(
            measurement);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return true;
    }

    private static VitalMeasurementResponse MapToResponse(
        VitalMeasurement measurement)
    {
        return new VitalMeasurementResponse
        {
            Id =
                measurement.Id,

            MigraineAttackId =
                measurement.MigraineAttackId,

            BodyPosition =
                measurement.BodyPosition,

            SystolicPressure =
                measurement.SystolicPressure,

            DiastolicPressure =
                measurement.DiastolicPressure,

            Pulse =
                measurement.Pulse,

            MeasuredAt =
                measurement.MeasuredAt,

            Notes =
                measurement.Notes,

            CreatedAt =
                measurement.CreatedAt
        };
    }

    private async Task ValidateAttackOwnershipAsync(
        Guid userId,
        Guid? attackId,
        CancellationToken cancellationToken)
    {
        if (!attackId.HasValue)
        {
            return;
        }

        var belongsToUser = await _dbContext.MigraineAttacks
            .AnyAsync(
                x => x.Id == attackId.Value && x.UserId == userId,
                cancellationToken);

        if (!belongsToUser)
        {
            throw new ArgumentException(
                "Указанный приступ мигрени не найден или недоступен.");
        }
    }

    private static string? NormalizeNotes(
        string? notes)
    {
        if (string.IsNullOrWhiteSpace(
            notes))
        {
            return null;
        }

        return notes.Trim();
    }
}
