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

    public async Task<Guid> AddComponentAsync(ComponentType type, string name)
    {
        var guid = Guid.NewGuid();
        Log.Information($"Creating node of type {type} with name {name} and guid: {guid}");
        var query = $"CREATE (:{type} {{name: '{name}', id: '{guid}'}})";
        
        await _driver.ExecutableQuery(query)
                     .WithConfig(_queryConfig)
                     .ExecuteAsync();
        Log.Information($"Created node of type {type} with name {name} and guid: {guid}");

        return guid;
    }

    public async Task<List<Component>> GetAllComponentsAsync()
    {
        Console.WriteLine("Getting all components..");

        var query = "MATCH (n) RETURN n.name AS name, n.id as id, labels(n) as type";
        var (result, _, _) = await _driver.ExecutableQuery(query)
                                          .WithConfig(_queryConfig)
                                          .ExecuteAsync();
        List<Component> components = [];
        foreach (var record in result)
        {
            var type = record["type"].As<List<string>>().First() switch
            {
                "Microservice" => ComponentType.Microservice,
                "Database" => ComponentType.Database,
                "MessageBroker" => ComponentType.MessageBroker,
                "ExternalAPI" => ComponentType.ExternalAPI,
                _ => ComponentType.Unknown
            };

            if (Guid.TryParse(record["id"].As<string>(), out var id) != true)
            {
                // mb throw?
                Console.WriteLine("Cannot parse GUID");
            }

            Console.WriteLine($"Name: {record["name"].As<string>()}, type: {type}, id: {id}");
            components.Add(
                new(record["name"].As<string>(), type, id)
            );
        }

        return components;
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