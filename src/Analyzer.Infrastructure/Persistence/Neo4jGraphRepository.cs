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
        Log.Information("Подключаемся к базе...");
        try
        {
            _driver.VerifyConnectivityAsync().Wait();
        }
        catch
        {
            Log.Error("Невозможно подключиться к базе.");
            throw;
        }
        _queryConfig = new QueryConfig(database: "neo4j");
    }

    public async Task<Guid> AddComponentAsync(ComponentType type, string name, string description)
    {
        var guid = Guid.NewGuid();
        Log.Information($"Создаем компонент типа {type} с именем {name}, GUID: {guid} и описанием {description}");
        
        var query = @$"
            CREATE (:{type} {{
                name: $Name, 
                id: $Guid, 
                desc: $Description
            }})";

        try
        {
            await _driver.ExecutableQuery(query)
                        .WithConfig(_queryConfig)
                        .WithParameters(new
                        {
                            Name = name,
                            Guid = guid.ToString(),
                            Description = description
                        })
                        .ExecuteAsync();

        }
        catch (Exception e)
        {
            Log.Error($"Неизвестная ошибка: {e.Message}");
            throw e;
        }
        Log.Information($"Создан компонент с GUID: {guid}");

        return guid;
    }

    public async Task<List<Component>> GetAllComponentsAsync()
    {
        Log.Information("Получаем все компоненты..");

        var query = @"
            MATCH (n) 
            RETURN n.name AS Name, 
                   n.id AS Id, 
                   n.desc AS Desc, 
                   labels(n) AS Type";

        try
        {
            var (result, _, _) = await _driver.ExecutableQuery(query)
                                          .WithConfig(_queryConfig)
                                          .ExecuteAsync();

            var components = result.Select(record => new Component(
                name: record["Name"].As<string>(),
                desription: record["Desc"].As<string>(),

                type: Enum.TryParse<ComponentType>(record["Type"].As<List<string>>().First(),
                        true, out var type)
                        ? type : ComponentType.Unknown,

                guid: Guid.TryParse(record["Id"].As<string>(), out var guid)
                        ? guid : throw new KeyNotFoundException("Ошибка парсинга GUID")
            )).ToList();

            Log.Information("Все компоненты получены.");
            return components;
        }
        catch (KeyNotFoundException e)
        {
            Log.Error($"Невозможно распарсить ответ БД: {e.Message}");
            throw e;
        }
        catch (Exception e)
        {
            Log.Error($"Неизвестная ошибка: {e.Message}");
            throw e;
        }
    }

    public async Task<Component> GetComponentAsync(Guid id)
    {
        Log.Information($"Получаем компонент с GUID: {id}..");

        var query = @"
            MATCH (n) 
            WHERE n.id = $Id 
            RETURN n.name AS Name, 
                   n.desc AS Desc, 
                   labels(n) AS Type";

        try
        {
            var (result, _, _) = await _driver.ExecutableQuery(query)
                                         .WithConfig(_queryConfig)
                                         .WithParameters(new { Id = id.ToString() })
                                         .ExecuteAsync();

            if ((result?.Count ?? 0) == 0)
                // TODO: переделать на норм исключение
                throw new Exception($"Объект с GUID {id} не найден.");

            var record = result!.First();

            var type = Enum.TryParse<ComponentType>(record["Type"].As<List<string>>().First(),
                true, out var t)
                ? t : ComponentType.Unknown;

            var desc = record["Desc"].As<string>();
            var name = record["Name"].As<string>();

            Log.Information($"Получен компонент с GUID: {id}.");
            return new(name, type, desc, id);
        }
        catch (KeyNotFoundException e)
        {
            Log.Error($"Невозможно распарсить ответ БД: {e.Message}");
            throw e;
        }
        catch (Exception e)
        {
            Log.Error($"Неизвестная ошибка: {e.Message}");
            throw e;
        }
    }

    public async Task UpdateComponentAsync(Component component)
    {
        Log.Information($"Обновляем компонент с GUID: {component.Id}..");

        var query = @"
            MATCH (c {id: $Id})
            SET c.name = $Name, 
                c.desc = $Description";

        try
        {
            await _driver.ExecutableQuery(query)
                         .WithConfig(_queryConfig)
                         .WithParameters(new
                         {
                             Id = component.Id.ToString(),
                             Name = component.Name,
                             Description = component.Description
                         })
                         .ExecuteAsync();
        }
        catch (Exception e)
        {
            Log.Error($"Неизвестная ошибка: {e.Message}");
            throw e;
        }

        Log.Information($"Обновлен компонент с GUID: {component.Id}.");
    }

    public async Task DeleteComponentAsync(Guid id)
    {
        Log.Information($"Удаляем компонент с GUID: {id}..");
        try
        {
            var query = @"
                MATCH (c {id: $Id})
                DETACH DELETE c";

            await _driver.ExecutableQuery(query)
                         .WithConfig(_queryConfig)
                         .WithParameters(new { Id = id.ToString() })
                         .ExecuteAsync();

        }
        catch (Exception e)
        {
            Log.Error($"Неизвестная ошибка: {e.Message}");
            throw e;
        }
        Log.Information($"Удален компонент с GUID: {id}.");
    }

    public async Task<Guid> AddLinkAsync(Guid sourceId, Guid targetId, LinkSeverity severity, ProtocolType protocol)
    {
        Log.Information($"Создаем связь {sourceId} -> {targetId}..");
        var guid = Guid.NewGuid();
        
        var query = @$"
            MATCH (s {{
                id: $SourceId 
            }})
            MATCH (t {{
                id: $TargetId 
            }})
            CREATE (s)-[:DEPENDS_ON {{id: $Id, severity: $Severity, protocol: $Protocol}}]->(t)";

        try
        {
            await _driver.ExecutableQuery(query)
                        .WithConfig(_queryConfig)
                        .WithParameters(new
                        {
                            Id = guid.ToString(),
                            SourceId = sourceId.ToString(),
                            TargetId = targetId.ToString(),
                            Severity = severity.ToString(),
                            Protocol = protocol.ToString()
                        })
                        .ExecuteAsync();

        }
        catch (Exception e)
        {
            Log.Error($"Неизвестная ошибка: {e.Message}");
            throw e;
        }
        Log.Information($"Создана связь с GUID: {guid}");

        return guid;
    }

    public async Task<List<Link>> GetAllLinksAsync()
    {
        Log.Information("Получаем все связи..");

        try
        {
            var query = @"
                MATCH (source)-[r:DEPENDS_ON]->(target)
                RETURN source.id AS SourceId, target.id AS TargetId, 
                    r.severity AS Severity, r.protocol AS Protocol, r.id AS Id";

            var (records, _, _) = await _driver.ExecutableQuery(query)
                                               .WithConfig(_queryConfig)
                                               .ExecuteAsync();

            var links = records.Select(record => new Link
            {
                Id = Guid.Parse(record["Id"].As<string>()),
                SourceId = Guid.Parse(record["SourceId"].As<string>()),
                TargetId = Guid.Parse(record["TargetId"].As<string>()),

                Severity = Enum.TryParse<LinkSeverity>(record["Severity"].As<string>(), true, out var sev)
                        ? sev : LinkSeverity.Unknown,

                Protocol = Enum.TryParse<ProtocolType>(record["Protocol"].As<string>(), true, out var prot)
                        ? prot : ProtocolType.Unknown
            }).ToList();

            Log.Information("Все связи получены.");
            return links;
        }
        catch (KeyNotFoundException e)
        {
            Log.Error($"Невозможно распарсить ответ БД: {e.Message}");
            throw e;
        }
        catch (Exception e)
        {
            Log.Error($"Неизвестная ошибка: {e.Message}");
            throw e;
        }
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