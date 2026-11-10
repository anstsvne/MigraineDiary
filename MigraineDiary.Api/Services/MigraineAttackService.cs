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

        _dbContext.MigraineAttacks.Add(attack);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return new MigraineAttackResponse
        {
            Id = attack.Id,
            StartAt = attack.StartAt,
            EndAt = attack.EndAt,
            DurationMinutes =
                (attack.EndAt - attack.StartAt)
                .TotalMinutes,
            Intensity = attack.Intensity,
            PainLocation = attack.PainLocation,
            PainLocationDescription =
                attack.PainLocationDescription,
            Notes = attack.Notes,
            CreatedAt = attack.CreatedAt
        };
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
                CreatedAt = x.CreatedAt
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
                CreatedAt = x.CreatedAt
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

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return new MigraineAttackResponse
        {
            Id = attack.Id,
            StartAt = attack.StartAt,
            EndAt = attack.EndAt,
            DurationMinutes =
                (attack.EndAt - attack.StartAt)
                .TotalMinutes,
            Intensity = attack.Intensity,
            PainLocation = attack.PainLocation,
            PainLocationDescription =
                attack.PainLocationDescription,
            Notes = attack.Notes,
            CreatedAt = attack.CreatedAt
        };
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
}