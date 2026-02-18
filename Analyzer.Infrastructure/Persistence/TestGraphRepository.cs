namespace Analyzer.Infrastructure.Persistence;

using Analyzer.Application.Interfaces;
using Analyzer.Domain.Entities;
using Analyzer.Domain.Enums;

public sealed class TestGraphRepository : IGraphRepository
{
    public TestGraphRepository()
    {
        Console.WriteLine("Ctor called!");
    }

    public async Task<Guid> CreateComponentAsync(ComponentType type, string name)
    {
        Component comp = new() { Type = type, Name = name };
        Console.WriteLine($"[TEST] Creating node of type {type} with name {name}");
        Console.WriteLine($"[TEST] Created node of type {type} with name {name}");
        return comp.Id;
    }

    public async Task<Component> GetComponentAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public async Task UpdateComponentAsync(Component node)
    {
        throw new NotImplementedException();
    }
    
    public async Task DeleteComponentAsync(Guid id)
    {
        throw new NotImplementedException();
    }
}