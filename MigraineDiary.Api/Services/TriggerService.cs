using MigraineDiary.Api.Contracts.Triggers;
using MigraineDiary.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace MigraineDiary.Api.Services;

public sealed class TriggerService
{
    private readonly AppDbContext _dbContext;

    public TriggerService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<TriggerResponse>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Triggers
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new TriggerResponse
            {
                Id = x.Id,
                Name = x.Name
            })
            .ToListAsync(cancellationToken);
    }
}