namespace Analyzer.Infrastructure.Persistence;

using System.Diagnostics;
using Analyzer.Application.Interfaces.Repositories;
using Analyzer.Domain.Entities;
using Analyzer.Domain.Enums;
using Analyzer.Shared.DTO;
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

    public async Task<IReadOnlyCollection<Component>> GetComponentsBySystemIdAsync(Guid systemId)
    {
        Log.Information($"Получаем все компоненты системы {systemId}..");

        var query = @"
            MATCH (n) 
            WHERE n.system_id = $SystemId 
            RETURN n.name AS Name, 
                   n.id AS Id, 
                   n.desc AS Desc,
                   n.system_id AS SystemId, 
                   labels(n) AS Type";

        try
        {
            var (result, _, _) = await _driver.ExecutableQuery(query)
                                          .WithConfig(_queryConfig)
                                          .WithParameters(new { SystemId = systemId.ToString() })
                                          .ExecuteAsync();

            var components = result.Select(record => new Component
            {
                Name = record["Name"].As<string>(),
                Description = record["Desc"].As<string>(),

                Type = Enum.TryParse<ComponentType>(record["Type"].As<List<string>>().First(),
                        true, out var type)
                        ? type : ComponentType.Unknown,

                Id = Guid.TryParse(record["Id"].As<string>(), out var guid)
                        ? guid : throw new KeyNotFoundException("Ошибка парсинга Id"),

                SystemId = Guid.TryParse(record["SystemId"].As<string>(), out var sysId)
                        ? sysId : throw new KeyNotFoundException("Ошибка парсинга System Id")
            }).ToList();

            Log.Information("Все компоненты получены.");
            return components;
        }
        catch (KeyNotFoundException e)
        {
            Log.Error($"Невозможно распарсить ответ БД: {e.Message}");
            throw;
        }
        catch (Exception e)
        {
            Log.Error($"Неизвестная ошибка: {e.Message}");
            throw;
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
                   n.system_id AS SystemId, 
                   labels(n) AS Type";

        try
        {
            var (result, _, _) = await _driver.ExecutableQuery(query)
                                         .WithConfig(_queryConfig)
                                         .WithParameters(new { Id = id.ToString() })
                                         .ExecuteAsync();

            if (!result.Any())
                // TODO: переделать на норм исключение
                throw new Exception($"Объект с GUID {id} не найден.");

            var record = result!.First();

            var type = Enum.TryParse<ComponentType>(record["Type"].As<List<string>>().First(),
                true, out var t)
                ? t : ComponentType.Unknown;

            if (!Guid.TryParse(record["SystemId"].As<string>(), out var systemId))
                throw new KeyNotFoundException("Ошибка парсинга System Id");

            var desc = record["Desc"].As<string>();
            var name = record["Name"].As<string>();

            Log.Information($"Получен компонент с GUID: {id}.");
            return new Component
            {
                Id = id,
                Name = name,
                Type = type,
                Description = desc,
                SystemId = systemId
            };
        }
        catch (KeyNotFoundException e)
        {
            Log.Error($"Невозможно распарсить ответ БД: {e.Message}");
            throw;
        }
        catch (Exception e)
        {
            Log.Error($"Неизвестная ошибка: {e.Message}");
            throw;
        }
    }

    public async Task AddComponentAsync(Component component)
    {
        Log.Information($@"Добавляем компонент типа 
                        {component.Type} с именем {component.Name}, 
                        GUID: {component.Id} и описанием 
                        {component.Description} для системы 
                        {component.SystemId}");
        
        var query = @$"
            CREATE (:{component.Type} {{
                name: $Name, 
                id: $Guid, 
                desc: $Description,
                system_id: $SystemId
            }})";

        try
        {
            await _driver.ExecutableQuery(query)
                        .WithConfig(_queryConfig)
                        .WithParameters(new
                        {
                            Name = component.Name,
                            Guid = component.Id.ToString(),
                            Description = component.Description,
                            SystemId = component.SystemId.ToString()
                        })
                        .ExecuteAsync();

        }
        catch (Exception e)
        {
            Log.Error($"Неизвестная ошибка: {e.Message}");
            throw;
        }
        Log.Information($"Добавлен компонент с GUID: {component.Id}");
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
            throw;
        }

        Log.Information($"Обновлен компонент с GUID: {component.Id}.");
    }

    public async Task DeleteComponentAsync(Guid id)
    {
        Log.Information($"Удаляем компонент с GUID: {id}..");
        
        var query = @"
                MATCH (c {id: $Id})
                DETACH DELETE c";

        try
        {
            await _driver.ExecutableQuery(query)
                         .WithConfig(_queryConfig)
                         .WithParameters(new { Id = id.ToString() })
                         .ExecuteAsync();

        }
        catch (Exception e)
        {
            Log.Error($"Неизвестная ошибка: {e.Message}");
            throw;
        }
        Log.Information($"Удален компонент с GUID: {id}.");
    }

    public async Task AddLinkAsync(Link link)
    {
        Log.Information($"Создаем связь {link.SourceId} -> {link.TargetId}..");
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
                            SourceId = link.SourceId.ToString(),
                            TargetId = link.TargetId.ToString(),
                            Severity = link.Severity.ToString(),
                            Protocol = link.Protocol.ToString()
                        })
                        .ExecuteAsync();

        }
        catch (Exception e)
        {
            Log.Error($"Неизвестная ошибка: {e.Message}");
            throw;
        }
        Log.Information($"Создана связь с GUID: {guid}");
    }

    public async Task<IReadOnlyCollection<Link>> GetLinksBySystemIdAsync(Guid systemId)
    {
        Log.Information("Получаем все связи..");

        var query = @"
                MATCH (source)-[r:DEPENDS_ON]->(target)
                WHERE source.system_id = $SystemId AND target.system_id = $SystemId
                RETURN source.id AS SourceId, target.id AS TargetId, 
                    r.severity AS Severity, r.protocol AS Protocol, r.id AS Id";

        try
        {
            var (records, _, _) = await _driver.ExecutableQuery(query)
                                               .WithConfig(_queryConfig)
                                               .WithParameters(new { SystemId = systemId.ToString() })
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
            throw;
        }
        catch (Exception e)
        {
            Log.Error($"Неизвестная ошибка: {e.Message}");
            throw;
        }
    }
    
    public async Task DeleteLinkAsync(Guid id)
    {
        Log.Information($"Удаляем связь с GUID: {id}..");

        var query = @"
                MATCH ()-[r:DEPENDS_ON]->()
                WHERE r.id = $Id
                DELETE r";
        
        try
        {
            await _driver.ExecutableQuery(query)
                         .WithConfig(_queryConfig)
                         .WithParameters(new { Id = id.ToString() })
                         .ExecuteAsync();

        }
        catch (Exception e)
        {
            Log.Error($"Неизвестная ошибка: {e.Message}");
            throw;
        }
        Log.Information($"Удален компонент с GUID: {id}.");
    }

    public async Task<IReadOnlyCollection<Guid>> GetCascadingFailureImpactAsync(Guid failedComponentId)
    {
        Log.Information($"Запуск поиска критического пути для компонента: {failedComponentId}..");

        var query = @"
                MATCH 
                (failed {id: $FailedId})<-[:DEPENDS_ON*]-(affected)
                RETURN affected.id AS Id";

        try
        {
            Stopwatch stopWatch = new();
            stopWatch.Start();
            var (result, _, _) = await _driver.ExecutableQuery(query)
                         .WithConfig(_queryConfig)
                         .WithParameters(new { FailedId = failedComponentId.ToString() })
                         .ExecuteAsync();
            stopWatch.Stop();

            var components = result.Select(record =>    
                        Guid.TryParse(record["Id"].As<string>(), out var guid)
                        ? guid : throw new KeyNotFoundException("Ошибка парсинга GUID")
            ).ToList();

            Log.Information($"Поиск критического пути завершен. Время исполнения: {stopWatch.ElapsedMilliseconds} мс");
            return components;
        }
        catch (KeyNotFoundException e)
        {
            Log.Error($"Невозможно распарсить ответ БД: {e.Message}");
            throw;
        }
        catch (Exception e)
        {
            Log.Error($"Неизвестная ошибка: {e.Message}");
            throw;
        }        
    }

    public Task<IReadOnlyCollection<IReadOnlyCollection<Guid>>> GetCyclicDependenciesAsync(Guid systemId)
    {
        throw new NotImplementedException();
    }

    public Task<Dictionary<Guid, int>> GetSinglePointsOfFailureAsync(Guid systemId, int threshold = 3)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyCollection<Guid>> GetDecommissioningImpactAsync(Guid targetComponentId)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyCollection<GraphPathDto>> GetDeploymentRiskPathsAsync(Guid deployComponentId)
    {
        throw new NotImplementedException();
    }
}