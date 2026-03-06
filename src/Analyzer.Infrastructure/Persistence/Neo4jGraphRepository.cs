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
        try
        {
            var components = result.Select(record => new Component(
                name: record["name"].As<string>(),
                desription: record["desc"].As<string>(),

                type: Enum.TryParse<ComponentType>(record["type"].As<List<string>>().First(),
                        true, out var type)
                        ? type : ComponentType.Unknown,

                guid: Guid.TryParse(record["id"].As<string>(), out var guid)
                        ? guid : throw new KeyNotFoundException("Cannot parse GUID")
            )).ToList();

            return components;
        }
        catch (KeyNotFoundException e)
        {
            Log.Error($"Cannot parse Neo4J answer.");
            throw e;
        }
        catch (Exception e)
        {
            Log.Error($"Unknown error: {e.Message}");
            throw e;
        }
    }

    public async Task<Component> GetComponentAsync(Guid id)
    {
        Log.Information($"Getting components {id}..");

        var query = @$"MATCH (n) 
            WHERE n.id = '{id}' 
            RETURN n.name AS Name, n.id AS Id, n.desc AS Desc, labels(n) AS Type";

        var (result, _, _) = await _driver.ExecutableQuery(query)
                                          .WithConfig(_queryConfig)
                                          .ExecuteAsync();

        if ((result?.Count ?? 0) == 0)
            // TODO: переделать на норм исключение
            throw new Exception($"Объект с GUID {id} не найден.");

        var record = result!.First();

        var type = Enum.TryParse<ComponentType>(record["Type"].As<List<string>>().First(),
            true, out var t)
            ? t : ComponentType.Unknown;

        var desc = record["Desc"].As<string>();

        return new(record["Name"].As<string>(), type, desc, id);
    }

    public async Task UpdateComponentAsync(Component component)
    {
        Log.Information($"Updating component {component.Id}..");

        var query = @$"
            MATCH (c {{id: '{component.Id}'}})
            SET c.name = '{component.Name}', c.desc = '{component.Description}'";

        await _driver.ExecutableQuery(query)
                     .WithConfig(_queryConfig)
                     .ExecuteAsync();
    }

    public async Task DeleteComponentAsync(Guid id)
    {
        Log.Information($"Deleting component {id}..");
        var query = @$"
            MATCH (c {{id: '{id}'}})
            DETACH DELETE c";

        await _driver.ExecutableQuery(query)
                     .WithConfig(_queryConfig)
                     .ExecuteAsync();
    }

    public async Task<List<Link>> GetAllLinksAsync()
    {
        Log.Information("Getting all links..");

        var query = @"
        MATCH (source)-[r:DEPENDS_ON]->(target)
        RETURN source.id AS SourceId, target.id AS TargetId, 
               r.severity AS Severity, r.protocol AS Protocol";

        var (records, _, _) = await _driver.ExecutableQuery(query)
                                           .WithConfig(_queryConfig)
                                           .ExecuteAsync();

        var links = records.Select(record => new Link
        {
            SourceId = Guid.Parse(record["SourceId"].As<string>()),
            TargetId = Guid.Parse(record["TargetId"].As<string>()),

            Severity = Enum.TryParse<LinkSeverity>(record["Severity"].As<string>(), true, out var sev)
                    ? sev : LinkSeverity.Unknown,

            Protocol = Enum.TryParse<ProtocolType>(record["Protocol"].As<string>(), true, out var prot)
                    ? prot : ProtocolType.Unknown
        }).ToList();

        return links;
    }

    public async Task<List<Link>> GetComponentInboundLinksAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public async Task<List<Link>> GetComponentOutboundLinksAsync(Guid id)
    {
        throw new NotImplementedException();
    }
}