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

    public async Task<Guid> AddComponentAsync(ComponentType type, string name, string description)
    {
        var guid = Guid.NewGuid();
        Log.Information($"Creating node of type {type} with name {name} and guid: {guid} and desc: {description}");
        var query = $"CREATE (:{type} {{name: '{name}', id: '{guid}', desc: '{description}'}})";

        await _driver.ExecutableQuery(query)
                     .WithConfig(_queryConfig)
                     .ExecuteAsync();
        Log.Information($"Created node of type {type} with name {name} and guid: {guid}");

        return guid;
    }

    public async Task<List<Component>> GetAllComponentsAsync()
    {
        Log.Information("Getting all components..");

        var query = "MATCH (n) RETURN n.name AS name, n.id as id, n.desc as desc, labels(n) as type";
        var (result, _, _) = await _driver.ExecutableQuery(query)
                                          .WithConfig(_queryConfig)
                                          .ExecuteAsync();
        List<Component> components = [];
        try
        {
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
                    throw new KeyNotFoundException("Cannot parse GUID");
                }

                var desc = record["desc"].As<string>();

                components.Add(
                    new(record["name"].As<string>(), type, desc, id)
                );
            }
        }
        catch (KeyNotFoundException e)
        {
            Log.Error($"Cannot parse Neo4J answer.");
            throw e;
        }

        return components;
    }

    public async Task<Component> GetComponentAsync(Guid id)
    {
        Log.Information($"Getting components {id}..");

        var query = $"MATCH (n) WHERE n.id = '{id}' RETURN n.name AS name, n.id as id, n.desc as desc, labels(n) as type";
        var (result, _, _) = await _driver.ExecutableQuery(query)
                                          .WithConfig(_queryConfig)
                                          .ExecuteAsync();

        if ((result?.Count ?? 0) == 0)
            // TODO: переделать на норм исключение
            throw new Exception($"Объект с GUID {id} не найден.");
        
        var record = result!.First();

        var type = record["type"].As<List<string>>().First() switch
        {
            "Microservice" => ComponentType.Microservice,
            "Database" => ComponentType.Database,
            "MessageBroker" => ComponentType.MessageBroker,
            "ExternalAPI" => ComponentType.ExternalAPI,
            _ => ComponentType.Unknown
        };

        var desc = record["desc"].As<string>();

        return new(record["name"].As<string>(), type, desc, id);
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