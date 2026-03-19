using Analyzer.Application.Interfaces.Repositories;
using Analyzer.Application.Interfaces.Services;
using Analyzer.Domain.Entities;
using Analyzer.Shared.DTO;

namespace Analyzer.Application.Services;

public class InviteService(IInviteRepository inviteRepository, ITeamRepository teamRepository) : IInviteService
{
    private readonly IInviteRepository _inviteRepository = inviteRepository;
    private readonly ITeamRepository _teamRepository = teamRepository;

    public async Task<string> GenerateInviteAsync(GenerateInviteDto dto)
    {
        if (await _teamRepository.GetByIdAsync(dto.TeamId) is null) 
            throw new KeyNotFoundException("Команда не найдена");

        var invite = new Invite(dto.Email, dto.ValidForDays, dto.TeamId, dto.Role);
        await _inviteRepository.AddAsync(invite);

        return invite.Code;
    }

    public async Task AcceptInviteAsync(string code, User user)
    {
        var invite = await _inviteRepository.GetByCodeAsync(code) 
            ?? throw new KeyNotFoundException("Приглашение не найдено или код неверен");

        invite.ActivateUser(user); 
        await _inviteRepository.UpdateAsync(invite);
    }

    public async Task RevokeInviteAsync(Guid inviteId)
    {
        var invite = await _inviteRepository.GetByIdAsync(inviteId) 
            ?? throw new KeyNotFoundException("Приглашение не найдено");

        invite.Revoke();

        await _inviteRepository.UpdateAsync(invite);
    }

    public async Task<IReadOnlyCollection<InviteDto>> GetTeamInvitesAsync(Guid teamId)
    {
        var invites = await _inviteRepository.GetByTeamIdAsync(teamId);
        
        return invites.Select(MapToDto).ToList();
    }

    private InviteDto MapToDto(Invite invite)
    {
        return new InviteDto
        {
            Id = invite.Id,
            Code = invite.Code,
            ExpirationDate = invite.ExpirationDate,
            Status = invite.Status,
            TeamId = invite.TeamId,
            ActivatedByUserId = invite.ActivatedByUserId
        };
    }
}