namespace Analyzer.Application.Services;

using Analyzer.Application.Interfaces.Services;
using Analyzer.Application.Interfaces.Repositories;
using Analyzer.Shared.DTO;
using Analyzer.Domain.Exceptions;
using Analyzer.Domain.Entities;

public class GraphService(IGraphRepository repository) : IGraphService
{
    readonly IGraphRepository _repository = repository;
    public async Task<Guid> CreateComponentAsync(CreateComponentDto component)
    {
        var (type, name, desc) = component;
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidComponentPropertyException("Имя компонента не может быть пустой строкой");

        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidComponentPropertyException("Описание компонента не может быть пустой строкой");

        return await _repository.AddComponentAsync(type, name, desc);
    }

    public async Task<List<ComponentDto>> GetAllComponentsAsync()
    {
        var components = await _repository.GetAllComponentsAsync();
        var componentDtos = components.Select(component => new ComponentDto() {
            Id = component.Id,
            Type = component.Type,
            Name = component.Name,
            Description = component.Description
        }).ToList();

        return componentDtos;
    }

    public async Task<ComponentDto> GetComponentDetailsAsync(Guid id)
    {
        var component = await _repository.GetComponentAsync(id);
        ComponentDto componentDto = new()
        {
            Id = component.Id,
            Type = component.Type,
            Name = component.Name,
            Description = component.Description
        };

        return componentDto;
    }

    public async Task UpdateComponentAsync(ComponentDto dto)
    {
        Component component = new(dto.Name, dto.Type, dto.Description, dto.Id);
        await _repository.UpdateComponentAsync(component);
    }

    public async Task DeleteComponentAsync(Guid id)
    {
        await _repository.DeleteComponentAsync(id);
    }

    public async Task<Guid> CreateLinkAsync(CreateLinkDto dto)
    {
        var (sourceId, targetId, severity, protocol) = dto;
        return await _repository.AddLinkAsync(sourceId, targetId, severity, protocol);
    }
    public async Task<List<LinkDto>> GetAllLinksAsync()
    {
        var links = await _repository.GetAllLinksAsync();
        var linkDtos = links.Select(link => new LinkDto() {
            Id = link.Id,
            SourceId = link.SourceId,
            TargetId = link.TargetId,
            Severity = link.Severity,
            Protocol = link.Protocol
        }).ToList();

        return linkDtos;
    }

    public async Task DeleteLinkAsync(Guid id)
    {
        await _repository.DeleteLinkAsync(id);
    }
}