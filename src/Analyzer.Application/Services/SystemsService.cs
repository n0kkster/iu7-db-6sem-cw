namespace Analyzer.Application.Services;

using Analyzer.Application.Interfaces.Repositories;
using Analyzer.Application.Interfaces.Services;
using Analyzer.Domain.Entities;
using Analyzer.Shared.DTO;

public class SystemsService(IGraphService graphService, ISystemsRepository systemsRepository) : ISystemsService
{
    readonly IGraphService _graphService = graphService;
    readonly ISystemsRepository _systemsRepository = systemsRepository;

    public async Task<IReadOnlyCollection<ITSystemDto>> GetSystemsByTeamIdAsync(Guid teamId)
    {
        var systems = await _systemsRepository.GetByTeamIdAsync(teamId);
        return systems.Select(system => new ITSystemDto(
            system.Id,
            system.Name,
            system.Description,
            system.CreatedAt,
            system.UpdatedAt
        )).ToList();
    }

    public async Task<(IReadOnlyCollection<ComponentDto>, IReadOnlyCollection<LinkDto>)> ExportSystem(Guid systemId)
    {
        var components = await _graphService.GetComponentsBySystemIdAsync(systemId);
        var links = await _graphService.GetLinksBySystemIdAsync(systemId);
        return (components, links);
    }

    public async Task<Guid> ImportSystem(IReadOnlyCollection<ComponentDto> components, 
                                         IReadOnlyCollection<LinkDto> links,
                                         string name, string description)
    {
        var guidMap = new Dictionary<Guid, Guid>();
        
        var newSystem = new ITSystem(name, description);
        await _systemsRepository.AddAsync(newSystem);

        foreach (var component in components)
        {
            var newGuid = await _graphService.CreateComponentAsync(new (
                newSystem.Id,
                component.Type,
                component.Name,
                component.Description
            ));
            guidMap.Add(component.Id, newGuid);
        }

        foreach (var link in links)
        {
            if (guidMap.TryGetValue(link.SourceId, out var newSourceId) &&
                guidMap.TryGetValue(link.TargetId, out var newTargetId))
            {
                var newLink = new CreateLinkDto(
                    newSourceId, 
                    newTargetId, 
                    link.Severity, 
                    link.Protocol
                );

                await _graphService.CreateLinkAsync(newLink);
            }
            else
            {
                Console.WriteLine($"Откуда-то взялась битая ссылка? ({link.SourceId} -> {link.TargetId})");
            }
        }

        return newSystem.Id;
    }
}