using Analyzer.Application.Interfaces.Repositories;
using Analyzer.Domain.Entities;

namespace Analyzer.Infrastructure.Persistence;

public class InviteRepository : IInviteRepository
{
    public Task AddAsync(Invite invite)
    {
        throw new NotImplementedException();
    }

    public Task<Invite?> GetByCodeAsync(string code)
    {
        throw new NotImplementedException();
    }

    public Task<Invite?> GetByIdAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyCollection<Invite>> GetByTeamIdAsync(Guid teamId)
    {
        throw new NotImplementedException();
    }

    public Task UpdateAsync(Invite invite)
    {
        throw new NotImplementedException();
    }
}