using Xunit;

namespace MigraineDiary.Api.Tests.Infrastructure;

[CollectionDefinition("API integration tests")]
public sealed class ApiTestCollection
    : ICollectionFixture<ApiTestFixture>
{
}