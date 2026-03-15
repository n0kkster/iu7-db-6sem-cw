namespace Analyzer.Application.Interfaces.Services;

using Analyzer.Shared.DTO;

public interface ISystemsService
{
    Task<(IReadOnlyCollection<ComponentDto>, IReadOnlyCollection<LinkDto>)> ExportSystem(Guid systemId);
    Task<Guid> ImportSystem(IReadOnlyCollection<ComponentDto> components, 
                            IReadOnlyCollection<LinkDto> links,
                            string name,
                            string description);

    Task<IReadOnlyCollection<ITSystemDto>> GetSystemsByTeamIdAsync(Guid teamId);
}