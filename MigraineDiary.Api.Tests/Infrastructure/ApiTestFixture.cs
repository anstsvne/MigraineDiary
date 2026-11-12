using MigraineDiary.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace MigraineDiary.Api.Tests.Infrastructure;

public sealed class ApiTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres =
        new PostgreSqlBuilder("postgres:16")
            .WithDatabase("migraine_diary_tests")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

    public MigraineDiaryApiFactory Factory { get; private set; }
        = null!;

    public HttpClient Client { get; private set; }
        = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        Factory = new MigraineDiaryApiFactory(
            _postgres.GetConnectionString());

        using var scope =
            Factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        await dbContext.Database.MigrateAsync();

        Client = Factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();

        Factory.Dispose();

        await _postgres.DisposeAsync();
    }
}