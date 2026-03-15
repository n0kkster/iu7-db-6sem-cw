namespace Analyzer.Application.Interfaces.Repositories;

using Analyzer.Domain.Entities;
using Analyzer.Domain.Enums;
using Analyzer.Shared.DTO;

public interface IGraphRepository
{
    // Компоненты
    Task<Guid> AddComponentAsync(ComponentType type, string name, string description);
    Task<Component> GetComponentAsync(Guid id);
    Task UpdateComponentAsync(Component component);
    Task DeleteComponentAsync(Guid id);
    Task<List<Component>> GetAllComponentsAsync();

    // Связи
    Task<Guid> AddLinkAsync(Guid sourceId, Guid targetId, LinkSeverity severity, ProtocolType protocol);
    Task<List<Link>> GetAllLinksAsync();
    Task<List<Link>> GetComponentInboundLinksAsync(Guid id);
    Task<List<Link>> GetComponentOutboundLinksAsync(Guid id);
    Task DeleteLinkAsync(Guid id);

    // Анализ
    Task<IReadOnlyCollection<Guid>> GetCascadingFailureImpactAsync(Guid failedComponentId);
    Task<IReadOnlyCollection<IReadOnlyCollection<Guid>>> GetCyclicDependenciesAsync(Guid systemId);
    Task<Dictionary<Guid, int>> GetSinglePointsOfFailureAsync(Guid systemId, int threshold = 3);
    Task<IReadOnlyCollection<Guid>> GetDecommissioningImpactAsync(Guid targetComponentId);
    Task<IReadOnlyCollection<GraphPathDto>> GetDeploymentRiskPathsAsync(Guid deployComponentId);
}