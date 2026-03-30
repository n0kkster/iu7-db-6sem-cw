using Analyzer.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Analyzer.IntegrationTests.Fixtures;

public class SharedDatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:17")
        .WithDatabase("AnalyzerTestDb")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public string ConnectionString => _dbContainer.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        var options = new DbContextOptionsBuilder<AnalyzerDbContext>()
            .UseNpgsql(ConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        await using var context = new AnalyzerDbContext(options);
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
    }

    public AnalyzerDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AnalyzerDbContext>()
            .UseNpgsql(ConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new AnalyzerDbContext(options);
    }
}

[CollectionDefinition("Database collection")]
public class DatabaseCollection : ICollectionFixture<SharedDatabaseFixture>
{ }