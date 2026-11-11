using Xunit;

namespace EventApi.IntegrationTests;

/// <summary>
/// Коллекция тестов с одним контейнером PostgreSQL.
/// </summary>
[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<DatabaseHelper>
{
}
