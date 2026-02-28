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

    public async Task<Guid> AddComponentAsync(ComponentType type, string name, string description)
    {
        Component comp = new(name, type, description);
        Console.WriteLine($"[TEST] Creating node of type {type} with name {name}");
        Console.WriteLine($"[TEST] Created node of type {type} with name {name} and guid {comp.Id}");
        return comp.Id;
    }

    public async Task<List<Component>> GetAllComponentsAsync()
    {
        throw new NotImplementedException();
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