using Analyzer.Application.Interfaces.Repositories;
using Analyzer.Domain.Entities;

namespace Analyzer.Infrastructure.Persistence;

public class TeamRepository : ITeamRepository
{
    public Task AddAsync(Team team)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(Guid teamId)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyCollection<Team>> GetAllTeamsAsync()
    {
        throw new NotImplementedException();
    }

    public Task<Team?> GetByIdAsync(Guid teamId)
    {
        throw new NotImplementedException();
    }

    public Task UpdateAsync(Team team)
    {
        throw new NotImplementedException();
    }
}