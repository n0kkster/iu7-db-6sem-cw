namespace Analyzer.Infrastructure.Persistence;

using Analyzer.Application.Interfaces;
using Analyzer.Domain.Entities;
using Analyzer.Domain.Enums;

using Neo4j.Driver;
using Serilog;

public sealed class Neo4jGraphRepository : IGraphRepository
{
    private readonly IDriver _driver;
    private readonly QueryConfig _queryConfig;

    public Neo4jGraphRepository(IDriver driver)
    {
        _driver = driver;
        Log.Information("Trying to connect to Neo4j...");
        try
        {
            _driver.VerifyConnectivityAsync().Wait();
        }
        catch
        {
            Log.Error("Cannot connect to Neo4j.");
            throw;
        }
        _queryConfig = new QueryConfig(database: "neo4j");
    }

    public async Task<Guid> CreateComponentAsync(ComponentType type, string name)
    {
        var guid = Guid.NewGuid();
        Log.Information($"Creating node of type {type} with name {name} and guid: {guid}");
        await _driver.ExecutableQuery($"CREATE (:{type} {{name: '{name}', id: '{guid}'}})")
                     .WithConfig(_queryConfig)
                     .ExecuteAsync();
        Log.Information($"Created node of type {type} with name {name} and guid: {guid}");
        
        return guid;
    }

    public async Task<Component> GetComponentAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public async Task UpdateComponentAsync(Component node)
    {
        throw new NotImplementedException();
    }

    public async Task DeleteComponentAsync(Guid id)
    {
        throw new NotImplementedException();
    }
}