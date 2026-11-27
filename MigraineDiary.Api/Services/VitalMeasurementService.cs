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

    public async Task<PagedResponse<VitalMeasurementResponse>>
        GetAllAsync(
            Guid userId,
            PaginationRequest pagination,
            CancellationToken cancellationToken = default)
    {
        var query =
            _dbContext.VitalMeasurements
                .AsNoTracking()
                .Where(x =>
                    x.UserId == userId);

        var totalCount =
            await query.CountAsync(
                cancellationToken);

        var skip =
            ((long)pagination.Page - 1) *
            pagination.PageSize;

        IReadOnlyList<VitalMeasurementResponse> items;

        if (skip >= totalCount)
        {
            items =
                Array.Empty<VitalMeasurementResponse>();
        }
        else
        {
            items =
                await query
                    .OrderByDescending(
                        x => x.MeasuredAt)
                    .ThenBy(
                        x => x.Id)
                    .Skip(
                        (int)skip)
                    .Take(
                        pagination.PageSize)
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

                            Pulse =
                                x.Pulse,

                            MeasuredAt =
                                x.MeasuredAt,

                            Notes =
                                x.Notes,

                            CreatedAt =
                                x.CreatedAt
                        })
                    .ToListAsync(
                        cancellationToken);
        }

        var totalPages =
            totalCount == 0
                ? 0
                : (int)Math.Ceiling(
                    totalCount /
                    (double)pagination.PageSize);

        return new PagedResponse<VitalMeasurementResponse>
        {
            Items = items,
            Page = pagination.Page,
            PageSize = pagination.PageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }

    public async Task<VitalMeasurementStatisticsResponse> GetStatisticsAsync(
        Guid userId,
        VitalMeasurementStatisticsRequest request,
        CancellationToken cancellationToken = default)
    {
        if ((request.From.HasValue || request.To.HasValue) &&
            (request.FromDate.HasValue || request.ToDate.HasValue))
        {
            throw new ArgumentException(
                "Timestamp and calendar date filters cannot be combined.");
        }

        if ((request.From.HasValue && request.To.HasValue &&
             request.From.Value > request.To.Value) ||
            (request.FromDate.HasValue && request.ToDate.HasValue &&
             request.FromDate.Value > request.ToDate.Value))
        {
            throw new ArgumentException(
                "The period end cannot be earlier than the period start.");
        }

        var query = _dbContext.VitalMeasurements
            .AsNoTracking()
            .Where(x => x.UserId == userId);

        if (request.From.HasValue)
        {
            var from = request.From.Value.ToUniversalTime();
            query = query.Where(x => x.MeasuredAt >= from);
        }

        if (request.To.HasValue)
        {
            var to = request.To.Value.ToUniversalTime();
            query = query.Where(x => x.MeasuredAt <= to);
        }

        if (request.FromDate.HasValue)
        {
            var from = new DateTimeOffset(
                request.FromDate.Value.ToDateTime(TimeOnly.MinValue),
                TimeSpan.Zero);
            query = query.Where(x => x.MeasuredAt >= from);
        }

        // Exclusive midnight of the following UTC date includes the entire
        // requested final day, including fractional seconds.
        if (request.ToDate.HasValue && request.ToDate.Value != DateOnly.MaxValue)
        {
            var toExclusive = new DateTimeOffset(
                request.ToDate.Value.AddDays(1).ToDateTime(TimeOnly.MinValue),
                TimeSpan.Zero);
            query = query.Where(x => x.MeasuredAt < toExclusive);
        }

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
