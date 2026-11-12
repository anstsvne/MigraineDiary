using MigraineDiary.Api.Contracts.Medications;
using MigraineDiary.Api.Data;
using MigraineDiary.Api.Entities;
using Microsoft.EntityFrameworkCore;
using MigraineDiary.Api.Exceptions;

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

    public async Task<MedicationResponse> CreateAsync(
        Guid userId,
        CreateMedicationRequest request,
        CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Ќазвание препарата не может быть пустым.");
        }

        var alreadyExists = await _dbContext.Medications
            .AnyAsync(
                x =>
                    x.UserId == userId &&
                    x.Name == name,
                cancellationToken);

        if (alreadyExists)
        {
            throw new ConflictException(
                 "ѕрепарат с таким названием уже существует.");
        }

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
}