namespace Analyzer.Application.Interfaces.Repositories;

using Analyzer.Domain.Entities;
using Analyzer.Domain.Enums;

public interface IGraphRepository
{
    Task<Guid> AddComponentAsync(ComponentType type, string name, string description);
    Task<Component> GetComponentAsync(Guid id);
    Task UpdateComponentAsync(Component component);
    Task DeleteComponentAsync(Guid id);
    Task<List<Component>> GetAllComponentsAsync();

    Task<Guid> AddLinkAsync(Guid sourceId, Guid targetId, LinkSeverity severity, ProtocolType protocol);
    Task<List<Link>> GetAllLinksAsync();
    Task<List<Link>> GetComponentInboundLinksAsync(Guid id);
    Task<List<Link>> GetComponentOutboundLinksAsync(Guid id);
    Task DeleteLinkAsync(Guid id);

    Task<List<Component>> GetImpactedComponentsAsync(Guid failedComponentId);
}