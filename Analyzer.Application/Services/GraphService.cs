namespace Analyzer.Application.Services;

using Analyzer.Application.Interfaces;
using Analyzer.Shared.DTO;
using Analyzer.Domain.Exceptions;

public class GraphService(IGraphRepository repository) : IGraphService
{
    readonly IGraphRepository _repository = repository;
    public async Task<Guid> CreateComponentAsync(CreateComponentDto component)
    {
        var (type, name, desc) = component;
        if (name.Length == 0)
            throw new InvalidComponentNameException("Имя компонента не может быть пустой строкой");
            
        return await _repository.AddComponentAsync(type, name, desc);
    }

    public async Task<List<ComponentDto>> GetAllComponentsAsync()
    {
        var components = await _repository.GetAllComponentsAsync();
        List<ComponentDto> componentDtos = [];
        foreach (var component in components)
        {
            componentDtos.Add(
                new (component.Id, component.Type, component.Name, component.Description)
            );
        }

        return componentDtos;
    }

    public async Task<ComponentDto> GetComponentDetailsAsync(Guid id)
    {
        var component = await _repository.GetComponentAsync(id);
        System.Console.WriteLine($"name: {component.Name}, type: {component.Type}, desc: {component.Description}");
        ComponentDto componentDto = new (component.Id, component.Type, component.Name, component.Description);

        return componentDto;
    }
}