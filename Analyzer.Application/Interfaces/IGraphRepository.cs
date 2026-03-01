namespace Analyzer.Application.Interfaces;

using Analyzer.Domain.Entities;
using Analyzer.Domain.Enums;

public interface IGraphRepository
{
    Task<Guid> AddComponentAsync(ComponentType type, string name, string description);
    Task<Component> GetComponentAsync(Guid id);
    Task UpdateComponentAsync(Component node);
    Task DeleteComponentAsync(Guid id);
    Task<List<Component>> GetAllComponentsAsync();

    Task<List<Link>> GetAllLinksAsync();
    Task<List<Link>> GetComponentInboundLinksAsync(Guid id);
    Task<List<Link>> GetComponentOutboundLinksAsync(Guid id);
}