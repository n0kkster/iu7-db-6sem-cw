namespace Analyzer.Application.Interfaces;

using Analyzer.Shared.DTO;

public interface ISystemsService
{
    Task<(List<ComponentDto>, List<LinkDto>)> ExportSystem();
    Task<Guid> ImportSystem(List<ComponentDto> components, List<LinkDto> links);
}