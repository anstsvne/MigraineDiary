using MigraineDiary.Api.Contracts.Symptoms;
using MigraineDiary.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace MigraineDiary.Api.Services;

public sealed class SymptomService
{
    private readonly AppDbContext _dbContext;

    public SymptomService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<SymptomResponse>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Symptoms
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new SymptomResponse
            {
                Id = x.Id,
                Name = x.Name
            })
            .ToListAsync(cancellationToken);
    }
}