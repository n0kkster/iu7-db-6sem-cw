namespace Analyzer.Application.Interfaces;

using Analyzer.Domain.Entities;
using Analyzer.Domain.Enums;

public interface IGraphRepository
{
    Task<ComponentNode> GetNodeAsync(int id);
    Task CreateNodeAsync(ComponentType type, string name);
    Task DeleteNodeAsync(int id);
    Task UpdateNodeAsync(ComponentNode node);
}