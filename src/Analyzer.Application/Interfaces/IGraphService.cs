namespace Analyzer.Application.Interfaces;

using Analyzer.Shared.DTO;

public interface IGraphService
{
    Task<Guid> CreateComponentAsync(CreateComponentDto dto);
    Task<List<ComponentDto>> GetAllComponentsAsync();
    Task<ComponentDto> GetComponentDetailsAsync(Guid id);
    Task UpdateComponentAsync(ComponentDto dto);
    Task DeleteComponentAsync(Guid id);

    Task<Guid> CreateLinkAsync(CreateLinkDto dto);
    Task<List<LinkDto>> GetAllLinksAsync();
    Task DeleteLinkAsync(Guid id);
}