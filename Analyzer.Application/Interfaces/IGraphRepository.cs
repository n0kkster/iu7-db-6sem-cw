namespace Analyzer.Application.Interfaces;

using Analyzer.Domain.Entities;
using Analyzer.Domain.Enums;

public interface IGraphRepository
{
    Task<Guid> CreateComponentAsync(ComponentType type, string name);
    Task<Component> GetComponentAsync(Guid id);
    Task UpdateComponentAsync(Component node);
    Task DeleteComponentAsync(Guid id);
}