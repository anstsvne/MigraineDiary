using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace MigraineDiary.Api.Tests.Infrastructure;

public sealed class MigraineDiaryApiFactory
    : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public MigraineDiaryApiFactory(
        string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override IHost CreateHost(
        IHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureHostConfiguration(
            configuration =>
            {
                configuration.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:Postgres"] =
                            _connectionString,

                        ["Jwt:Issuer"] =
                            "MigraineDiary.Api.Tests",

                        ["Jwt:Audience"] =
                            "MigraineDiary.Api.Tests.Client",

                        ["Jwt:Secret"] =
                            "migraine-diary-integration-tests-secret-key-2026-1234567890",

                        ["Jwt:LifetimeMinutes"] =
                            "60"
                    });
            });

        return base.CreateHost(builder);
    }
}