namespace Analyzer.Application.Interfaces;

using Analyzer.Shared.DTO;

public interface IGraphService
{
    Task<Guid> CreateComponentAsync(CreateComponentDto dto);
    Task<List<ComponentDto>> GetAllComponentsAsync();
    Task<ComponentDto> GetComponentDetailsAsync(Guid id);

    Task<List<LinkDto>> GetAllLinksAsync();
}