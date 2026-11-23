using MigraineDiary.Api.Contracts.Medications;
using MigraineDiary.Api.Data;
using MigraineDiary.Api.Entities;
using MigraineDiary.Api.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace MigraineDiary.Api.Services;

public sealed class MedicationService
{
    private readonly AppDbContext _dbContext;

    public MedicationService(
        AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<MedicationResponse>> GetAllAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Medications
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.Name)
            .Select(x => new MedicationResponse
            {
                Id = x.Id,
                Name = x.Name
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<MedicationResponse?> GetByIdAsync(
        Guid userId,
        Guid medicationId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Medications
            .AsNoTracking()
            .Where(x =>
                x.Id == medicationId &&
                x.UserId == userId)
            .Select(x => new MedicationResponse
            {
                Id = x.Id,
                Name = x.Name
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<MedicationResponse> CreateAsync(
        Guid userId,
        CreateMedicationRequest request,
        CancellationToken cancellationToken = default)
    {
        var name = NormalizeName(request.Name);

        await EnsureNameIsAvailableAsync(
            userId,
            name,
            excludedMedicationId: null,
            cancellationToken);

        var medication = new Medication
        {
            UserId = userId,
            Name = name
        };

        _dbContext.Medications.Add(medication);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return new MedicationResponse
        {
            Id = medication.Id,
            Name = medication.Name
        };
    }

    public async Task<MedicationResponse?> UpdateAsync(
        Guid userId,
        Guid medicationId,
        UpdateMedicationRequest request,
        CancellationToken cancellationToken = default)
    {
        var medication =
            await _dbContext.Medications
                .SingleOrDefaultAsync(
                    x =>
                        x.Id == medicationId &&
                        x.UserId == userId,
                    cancellationToken);

        if (medication is null)
        {
            return null;
        }

        var name = NormalizeName(request.Name);

        await EnsureNameIsAvailableAsync(
            userId,
            name,
            medicationId,
            cancellationToken);

        medication.Name = name;

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return new MedicationResponse
        {
            Id = medication.Id,
            Name = medication.Name
        };
    }

    public async Task<bool> DeleteAsync(
        Guid userId,
        Guid medicationId,
        CancellationToken cancellationToken = default)
    {
        var medication =
            await _dbContext.Medications
                .SingleOrDefaultAsync(
                    x =>
                        x.Id == medicationId &&
                        x.UserId == userId,
                    cancellationToken);

        if (medication is null)
        {
            return false;
        }

        var isUsed =
            await _dbContext.AttackMedications
                .AnyAsync(
                    x =>
                        x.MedicationId == medicationId,
                    cancellationToken);

        if (isUsed)
        {
            throw new ConflictException(
                "Нельзя удалить препарат, который используется в приступах.");
        }

        _dbContext.Medications.Remove(
            medication);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return true;
    }

    private async Task EnsureNameIsAvailableAsync(
        Guid userId,
        string name,
        Guid? excludedMedicationId,
        CancellationToken cancellationToken)
    {
        var alreadyExists =
            await _dbContext.Medications
                .AnyAsync(
                    x =>
                        x.UserId == userId &&
                        x.Name == name &&
                        (!excludedMedicationId.HasValue ||
                         x.Id != excludedMedicationId.Value),
                    cancellationToken);

        if (alreadyExists)
        {
            throw new ConflictException(
                "Препарат с таким названием уже существует.");
        }
    }

    private static string NormalizeName(
        string name)
    {
        var normalizedName = name.Trim();

        if (string.IsNullOrWhiteSpace(
            normalizedName))
        {
            throw new ArgumentException(
                "Название препарата не может быть пустым.");
        }

        return normalizedName;
    }
}
