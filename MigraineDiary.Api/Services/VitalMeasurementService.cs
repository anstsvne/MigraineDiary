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

    public async Task<IReadOnlyCollection<VitalMeasurementResponse>> GetAllAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.VitalMeasurements
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.MeasuredAt)
            .Select(x => new VitalMeasurementResponse
            {
                Id = x.Id,
                SystolicPressure = x.SystolicPressure,
                DiastolicPressure = x.DiastolicPressure,
                Pulse = x.Pulse,
                MeasuredAt = x.MeasuredAt,
                Notes = x.Notes,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);
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
            .Select(x => new VitalMeasurementResponse
            {
                Id = x.Id,
                SystolicPressure = x.SystolicPressure,
                DiastolicPressure = x.DiastolicPressure,
                Pulse = x.Pulse,
                MeasuredAt = x.MeasuredAt,
                Notes = x.Notes,
                CreatedAt = x.CreatedAt
            })
            .SingleOrDefaultAsync(cancellationToken);
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

        var measurement = new VitalMeasurement
        {
            UserId = userId,
            SystolicPressure = request.SystolicPressure,
            DiastolicPressure = request.DiastolicPressure,
            Pulse = request.Pulse,
            MeasuredAt = request.MeasuredAt.Value.ToUniversalTime(),
            Notes = NormalizeNotes(request.Notes)
        };

        _dbContext.VitalMeasurements.Add(measurement);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToResponse(measurement);
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

        var measurement = await _dbContext.VitalMeasurements
            .SingleOrDefaultAsync(
                x =>
                    x.Id == measurementId &&
                    x.UserId == userId,
                cancellationToken);

        if (measurement is null)
        {
            return null;
        }

        measurement.SystolicPressure =
            request.SystolicPressure;

        measurement.DiastolicPressure =
            request.DiastolicPressure;

        measurement.Pulse =
            request.Pulse;

        measurement.MeasuredAt =
            request.MeasuredAt.Value.ToUniversalTime();

        measurement.Notes =
            NormalizeNotes(request.Notes);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToResponse(measurement);
    }

    public async Task<bool> DeleteAsync(
        Guid userId,
        Guid measurementId,
        CancellationToken cancellationToken = default)
    {
        var measurement = await _dbContext.VitalMeasurements
            .SingleOrDefaultAsync(
                x =>
                    x.Id == measurementId &&
                    x.UserId == userId,
                cancellationToken);

        if (measurement is null)
        {
            return false;
        }

        _dbContext.VitalMeasurements.Remove(measurement);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    private static VitalMeasurementResponse MapToResponse(
        VitalMeasurement measurement)
    {
        return new VitalMeasurementResponse
        {
            Id = measurement.Id,
            SystolicPressure = measurement.SystolicPressure,
            DiastolicPressure = measurement.DiastolicPressure,
            Pulse = measurement.Pulse,
            MeasuredAt = measurement.MeasuredAt,
            Notes = measurement.Notes,
            CreatedAt = measurement.CreatedAt
        };
    }

    private static string? NormalizeNotes(
        string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            return null;
        }

        return notes.Trim();
    }
}