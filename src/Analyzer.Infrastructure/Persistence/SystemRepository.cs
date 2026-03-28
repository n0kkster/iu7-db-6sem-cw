using Analyzer.Application.Interfaces.Repositories;
using Analyzer.Domain.Entities;

namespace Analyzer.Infrastructure.Persistence;

public class SystemRepository : ISystemRepository
{
    public async Task AddAsync(ITSystem system)
    {
        await Task.Delay(1000);
    }

    public async Task DeleteAsync(Guid systemId)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ExistsWithNameAsync(Guid teamId, string name)
    {
        throw new NotImplementedException();
    }

    public async Task<ITSystem?> GetByIdAsync(Guid systemId)
    {
        throw new NotImplementedException();
    }

    public async Task<IReadOnlyCollection<ITSystem>> GetByTeamIdAsync(Guid teamId)
    {
        return [
            new ("System A", "Desc A", Guid.Parse("52b78daa-c9f9-43c1-b3cf-4d671acb989f")),
            new ("System B", "Desc B", Guid.Parse("5404d167-8b3f-41c3-9d52-6f401bc76dc5")),
            new ("System C", "Desc C", Guid.Parse("e4c872bf-c452-441e-8689-0f86b3eaf2dc")),
        ];
    }

    public async Task UpdateAsync(ITSystem system)
    {
        throw new NotImplementedException();
    }
}