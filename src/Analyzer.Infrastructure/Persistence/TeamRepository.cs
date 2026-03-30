using Analyzer.Application.Interfaces.Repositories;
using Analyzer.Domain.Entities;
using Analyzer.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Analyzer.Infrastructure.Persistence;

public class TeamRepository(AnalyzerDbContext context) : ITeamRepository
{
    public async Task<Team?> GetByIdAsync(Guid teamId)
    {
        var team = await context.Teams.FirstOrDefaultAsync(t => t.Id == teamId);
        
        if (team is not null)
        {
            var userIds = await context.Users
                .Where(u => u.TeamId == teamId)
                .Select(u => u.Id)
                .ToListAsync();

            team.LoadMembers(userIds);
        }

        return team;
    }

    public async Task<IReadOnlyCollection<Team>> GetAllTeamsAsync()
    {
        var teams = await context.Teams.ToListAsync();
        
        var teamUserMap = await context.Users
            .Where(u => u.TeamId != Guid.Empty)
            .GroupBy(u => u.TeamId)
            .ToDictionaryAsync(g => g.Key, g => g.Select(u => u.Id).ToList());

        foreach (var team in teams)
        {
            if (teamUserMap.TryGetValue(team.Id, out var userIds))
            {
                team.LoadMembers(userIds);
            }
        }

        return teams;
    }

    public async Task AddAsync(Team team)
    {
        await context.Teams.AddAsync(team);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Team team)
    {
        context.Teams.Update(team);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid teamId)
    {
        var team = await context.Teams.FindAsync(teamId);
        if (team != null)
        {
            context.Teams.Remove(team);
            await context.SaveChangesAsync();
        }
    }
}