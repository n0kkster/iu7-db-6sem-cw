using Analyzer.Domain.Entities;
using Analyzer.Shared.DTO;

namespace Analyzer.Application.Interfaces.Services;

public interface IInviteService
{
    Task<string> GenerateInviteAsync(GenerateInviteDto dto);
    Task AcceptInviteAsync(string code, User user);
    Task RevokeInviteAsync(Guid inviteId);
    Task<IReadOnlyCollection<InviteDto>> GetTeamInvitesAsync(Guid teamId);
}