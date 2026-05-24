namespace Analyzer.Application.Interfaces.Services;

using Analyzer.Domain.Entities;
using Analyzer.Shared.DTO;

public interface IGraphService
{
    Task<IReadOnlyCollection<ComponentDto>> GetComponentsBySystemIdAsync(Guid systemId);
    Task<ComponentDto> GetComponentDetailsAsync(Guid id);
    Task<Guid> CreateComponentAsync(CreateComponentDto dto);
    Task UpdateComponentAsync(ComponentDto dto);
    Task DeleteComponentAsync(Guid id);
    Task DeleteSystemAsync(Guid systemId);

    Task<IReadOnlyCollection<LinkDto>> GetLinksBySystemIdAsync(Guid systemId);
    Task<Guid> CreateLinkAsync(CreateLinkDto dto);
    Task DeleteLinkAsync(Guid id);

    Task ImportBulkAsync(List<Component> components, List<CreateLinkDto> links);
}