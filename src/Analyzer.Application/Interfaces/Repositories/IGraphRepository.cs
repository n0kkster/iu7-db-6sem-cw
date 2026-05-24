namespace Analyzer.Application.Interfaces.Repositories;

using Analyzer.Domain.Entities;
using Analyzer.Shared.DTO;

public interface IGraphRepository
{
    // Компоненты
    Task<IReadOnlyCollection<Component>> GetComponentsBySystemIdAsync(Guid systemId);
    Task<Component> GetComponentAsync(Guid id);
    Task AddComponentAsync(Component component);
    Task UpdateComponentAsync(Component component);
    Task DeleteComponentAsync(Guid id);

    // Связи
    Task<IReadOnlyCollection<Link>> GetLinksBySystemIdAsync(Guid systemId);
    Task AddLinkAsync(Link link);
    Task DeleteLinkAsync(Guid id);

    // Анализ
    Task<(IReadOnlyCollection<Guid>, long)> GetCascadingFailureImpactAsync(Guid failedComponentId);
    Task<(IReadOnlyCollection<IReadOnlyCollection<Guid>>, long)> GetCyclicDependenciesAsync(Guid systemId);
    Task<(Dictionary<Guid, int>, long)> GetSinglePointsOfFailureAsync(Guid systemId, int threshold = 3);
    Task<(IReadOnlyCollection<Guid>, long)> GetDecommissioningImpactAsync(Guid targetComponentId);
    Task<(IReadOnlyCollection<GraphPathDto>, long)> GetDeploymentRiskPathsAsync(Guid deployComponentId);
}