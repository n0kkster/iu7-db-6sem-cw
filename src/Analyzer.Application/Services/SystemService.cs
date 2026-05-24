namespace Analyzer.Application.Services;

using Analyzer.Application.Interfaces.Repositories;
using Analyzer.Application.Interfaces.Services;
using Analyzer.Domain.Entities;
using Analyzer.Shared.DTO;
using Serilog;

public class SystemService(IGraphService graphService, 
                           ISystemRepository systemsRepository) : ISystemService
{
    readonly IGraphService _graphService = graphService;
    readonly ISystemRepository _systemsRepository = systemsRepository;

    public async Task<IReadOnlyCollection<ITSystemDto>> GetSystemsByTeamIdAsync(Guid teamId)
    {
        var systems = await _systemsRepository.GetByTeamIdAsync(teamId);
        var tasks = systems.Select(async system =>
        {
            var components = await _graphService.GetComponentsBySystemIdAsync(system.Id);
            return new { system.Id, Count = components.Count() };
        });

        var results = await Task.WhenAll(tasks);
        var systemsComponentsCount = results.ToDictionary(x => x.Id, x => x.Count);

        return systems.Select(system => new ITSystemDto(
            system.Id,
            system.Name,
            system.Description,
            system.CreatedAt,
            system.UpdatedAt,
            system.TeamId,
            systemsComponentsCount[system.Id]
        )).ToList();
    }

    public async Task<(IReadOnlyCollection<ComponentDto>, IReadOnlyCollection<LinkDto>)> ExportSystemAsync(
        Guid systemId)
    {
        var components = await _graphService.GetComponentsBySystemIdAsync(systemId);
        var links = await _graphService.GetLinksBySystemIdAsync(systemId);
        return (components, links);
    }

    public async Task<Guid> ImportSystemAsync(IReadOnlyCollection<ComponentDto> components,
                                         IReadOnlyCollection<LinkDto> links,
                                         CreateITSystemDto systemDto)
    {
        var guidMap = new Dictionary<Guid, Guid>();

        var newSystemId = await CreateSystemAsync(systemDto);

        Log.Information("Импорт системы: {compsCount} узлов и {linksCount} связей...", components.Count, links.Count);

        var domainComponents = new List<Component>();
        foreach (var compDto in components)
        {
            var newGuid = Guid.NewGuid();
            guidMap.Add(compDto.Id, newGuid);

            domainComponents.Add(new Component
            {
                Id = newGuid,
                SystemId = newSystemId,
                Type = compDto.Type,
                Name = compDto.Name,
                Description = compDto.Description
            });
        }

        var domainLinks = new List<CreateLinkDto>();
        foreach (var link in links)
        {
            if (guidMap.TryGetValue(link.SourceId, out var newSourceId) &&
                guidMap.TryGetValue(link.TargetId, out var newTargetId))
            {
                domainLinks.Add(new CreateLinkDto(
                    newSourceId,
                    newTargetId,
                    link.Severity,
                    link.Protocol
                ));
            }
            else
            {
                Log.Warning($"Пропущена битая ссылка при импорте: {link.SourceId} -> {link.TargetId}");
            }
        }

        await _graphService.ImportBulkAsync(domainComponents, domainLinks);

        Log.Information("Импорт успешно завершен. Система создана с id: {id}", newSystemId);

        return newSystemId;
    }

    public async Task<Guid> CreateSystemAsync(CreateITSystemDto dto)
    {
        var newSystem = new ITSystem(dto.Name, dto.Description, dto.TeamId);
        await _systemsRepository.AddAsync(newSystem);

        return newSystem.Id;
    }

    public async Task UpdateSystemAsync(ITSystemDto dto)
    {
        var system = await _systemsRepository.GetByIdAsync(dto.Id)
            ?? throw new KeyNotFoundException("Система не найдена");

        system.UpdateDetails(dto.Name, dto.Description);
    }

    public async Task DeleteSystemAsync(Guid systemId)
    {
        await _graphService.DeleteSystemAsync(systemId);

        // Удаляем метаданные из Postgres
        await _systemsRepository.DeleteAsync(systemId);
    }
}