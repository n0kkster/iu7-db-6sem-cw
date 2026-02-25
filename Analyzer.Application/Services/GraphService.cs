namespace Analyzer.Application.Services;

using Analyzer.Application.Interfaces;
using Analyzer.Shared.DTO;
using Analyzer.Domain.Exceptions;

public class GraphService(IGraphRepository repository) : IGraphService
{
    readonly IGraphRepository _repository = repository;
    public async Task<Guid> CreateComponentAsync(CreateComponentDto component)
    {
        var (type, name) = component;
        if (name.Length == 0)
            throw new InvalidComponentNameException("Имя компонента не может быть пустой строкой");
            
        return await _repository.AddComponentAsync(type, name);
    }

    public async Task<List<ComponentDto>> GetAllComponentsAsync()
    {
        var components = await _repository.GetAllComponentsAsync();
        List<ComponentDto> componentDtos = [];
        foreach (var component in components)
        {
            componentDtos.Add(
                new (component.Id, component.Type, component.Name, component.Links)
            );
        }

        return componentDtos;
    }
}