namespace Analyzer.Application.Services;

using Analyzer.Application.Interfaces;
using Analyzer.Shared.DTO;

public class SystemsService(IGraphService graphService) : ISystemsService
{
    readonly IGraphService _graphService = graphService;

    public async Task<(List<ComponentDto>, List<LinkDto>)> ExportSystem()
    {
        var components = await _graphService.GetAllComponentsAsync();
        var links = await _graphService.GetAllLinksAsync();
        return (components, links);
    }

    public async Task<Guid> ImportSystem(List<ComponentDto> components, List<LinkDto> links)
    {
        var guidMap = new Dictionary<Guid, Guid>();
        foreach (var component in components)
        {
            var newGuid = await _graphService.CreateComponentAsync(new (
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
                Console.WriteLine($"Откуда-то взялась битая ссылка? ({link.SourceId}, {link.TargetId})");
            }
        }

        return Guid.NewGuid();
    }
}