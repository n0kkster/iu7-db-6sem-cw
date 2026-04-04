namespace Analyzer.Infrastructure.Persistence;

using System.Diagnostics;
using Analyzer.Application.Interfaces.Repositories;
using Analyzer.Domain.Entities;
using Analyzer.Domain.Enums;
using Analyzer.Shared.DTO;
using Analyzer.Infrastructure.Queries;
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

        var query = CypherQueryFactory.GetComponentsBySystemId();

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

        var query = CypherQueryFactory.GetComponentById();

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
        
        var query = CypherQueryFactory.AddComponent(component.Type.ToString());

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

        var query = CypherQueryFactory.UpdateComponent();

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
        
        var query = CypherQueryFactory.DeleteComponent();
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
        
        var query = CypherQueryFactory.AddLink();

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

        var query = CypherQueryFactory.GetLinksBySystemId();

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

        var query = CypherQueryFactory.DeleteLink();
        
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

        var query = CypherQueryFactory.GetCascadingFailureImpact();

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

    public async Task<IReadOnlyCollection<IReadOnlyCollection<Guid>>> GetCyclicDependenciesAsync(Guid systemId)
    {
        Log.Information($"Запуск поиска циклических зависимостей для системы: {systemId}..");

        var query = CypherQueryFactory.GetCyclicDependencies();

        try
        {
            Stopwatch stopWatch = new();
            stopWatch.Start();

            var (result, _, _) = await _driver.ExecutableQuery(query)
                .WithConfig(_queryConfig)
                .WithParameters(new { SystemId = systemId.ToString() })
                .ExecuteAsync();

            stopWatch.Stop();

            var cycles = new List<IReadOnlyCollection<Guid>>();
            var fetchedCycles = new HashSet<string>();

            foreach (var record in result)
            {
                var stringIds = record["CycleIds"].As<List<string>>();
                
                var ids = stringIds.Select(idStr => 
                    Guid.TryParse(idStr, out var guid) 
                        ? guid 
                        : throw new KeyNotFoundException("Ошибка парсинга GUID узла в цикле")
                ).ToList();

                var cycleKey = string.Join(",", ids.OrderBy(x => x));
                if (fetchedCycles.Add(cycleKey))
                {
                    cycles.Add(ids);
                }
            }

            Log.Information($"Поиск циклов завершен. Найдено уникальных циклов: {cycles.Count}. Время исполнения: {stopWatch.ElapsedMilliseconds} мс");
            return cycles;
        }
        catch (Exception e)
        {
            Log.Error($"Ошибка при поиске циклических зависимостей: {e.Message}");
            throw;
        }
    }

    public async Task<Dictionary<Guid, int>> GetSinglePointsOfFailureAsync(Guid systemId, int threshold = 3)
    {
        Log.Information($"Запуск поиска единых точек отказа (SPOF) для системы: {systemId} с порогом {threshold}..");

        var query = CypherQueryFactory.GetSinglePointsOfFailure();

        try
        {
            Stopwatch stopWatch = new();
            stopWatch.Start();

            var (result, _, _) = await _driver.ExecutableQuery(query)
                .WithConfig(_queryConfig)
                .WithParameters(new { SystemId = systemId.ToString(), Threshold = threshold })
                .ExecuteAsync();

            stopWatch.Stop();

            var spofDict = new Dictionary<Guid, int>();
            
            foreach (var record in result)
            {
                if (!Guid.TryParse(record["Id"].As<string>(), out var guid))
                    throw new KeyNotFoundException("Ошибка парсинга GUID компонента");

                var count = record["ImpactCount"].As<int>();
                spofDict[guid] = count;
            }

            Log.Information($"Поиск SPOF завершен. Найдено узких мест: {spofDict.Count}. Время исполнения: {stopWatch.ElapsedMilliseconds} мс");
            return spofDict;
        }
        catch (Exception e)
        {
            Log.Error($"Ошибка при поиске единых точек отказа: {e.Message}");
            throw;
        }
    }

    public async Task<IReadOnlyCollection<Guid>> GetDecommissioningImpactAsync(Guid targetComponentId)
    {
        Log.Information($"Запуск оценки вывода из эксплуатации для компонента: {targetComponentId}..");

        var query = CypherQueryFactory.GetDecommissioningImpact();

        try
        {
            Stopwatch stopWatch = new();
            stopWatch.Start();

            var (result, _, _) = await _driver.ExecutableQuery(query)
                .WithConfig(_queryConfig)
                .WithParameters(new { TargetId = targetComponentId.ToString() })
                .ExecuteAsync();

            stopWatch.Stop();

            var components = result.Select(record =>    
                Guid.TryParse(record["Id"].As<string>(), out var guid)
                    ? guid 
                    : throw new KeyNotFoundException("Ошибка парсинга GUID зависимого компонента")
            ).ToList();

            Log.Information($"Оценка вывода из эксплуатации завершена. Затронуто сервисов: {components.Count}. Время исполнения: {stopWatch.ElapsedMilliseconds} мс");
            return components;
        }
        catch (Exception e)
        {
            Log.Error($"Ошибка при оценке вывода из эксплуатации: {e.Message}");
            throw;
        }
    }

    public async Task<IReadOnlyCollection<GraphPathDto>> GetDeploymentRiskPathsAsync(Guid deployComponentId)
    {
        Log.Information($"Запуск поиска путей рисков развертывания для компонента: {deployComponentId}..");

        var query = CypherQueryFactory.GetDeploymentRiskPaths();

        try
        {
            Stopwatch stopWatch = new();
            stopWatch.Start();

            var (result, _, _) = await _driver.ExecutableQuery(query)
                .WithConfig(_queryConfig)
                .WithParameters(new { TargetId = deployComponentId.ToString() })
                .ExecuteAsync();

            stopWatch.Stop();

            var paths = new List<GraphPathDto>();

            foreach (var record in result)
            {
                var stringIds = record["PathIds"].As<List<string>>();
                
                var ids = stringIds.Select(idStr => 
                    Guid.TryParse(idStr, out var guid) 
                        ? guid 
                        : throw new KeyNotFoundException("Ошибка парсинга GUID узла в пути")
                ).ToList();

                paths.Add(new GraphPathDto { NodeIds = ids });
            }

            Log.Information($"Поиск путей рисков завершен. Найдено цепочек: {paths.Count}. Время исполнения: {stopWatch.ElapsedMilliseconds} мс");
            return paths;
        }
        catch (Exception e)
        {
            Log.Error($"Ошибка при поиске путей рисков: {e.Message}");
            throw;
        }
    }
}