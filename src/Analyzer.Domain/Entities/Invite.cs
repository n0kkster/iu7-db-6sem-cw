using Analyzer.Domain.Enums;
using System.Security.Cryptography;
using System.Text;

namespace Analyzer.Domain.Entities;

public class Invite(string targetEmail, int validForDays, Guid teamId, Role role)
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public InviteStatus Status
    {
        get
        {
            if (field == InviteStatus.Pending && DateTimeOffset.UtcNow > ExpirationDate)
                field = InviteStatus.Expired;

            return field;
        }
     
        private set;
    }
    public Guid? ActivatedByUserId { get; private set; } = null;
    public Role Role { get; init; } = role;

    public string Code { get; init; } = 
        Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(targetEmail)));

    public DateTimeOffset ExpirationDate { get; private set; } = 
        DateTimeOffset.UtcNow.AddDays(validForDays);

    public Guid TeamId { get; init; } = 
        teamId == Guid.Empty ? 
        throw new ArgumentException("Команда обязательна", nameof(teamId)) : 
        teamId;

    private bool CheckTarget(string targetEmail)
    {
        return Code == Convert.ToHexString(
                            SHA256.HashData(
                                Encoding.UTF8.GetBytes(targetEmail)));
    }

    public void ActivateUser(User user)
    {
        if (!CheckTarget(user.Email))
            throw new ArgumentException("Приглашение не предназначен для этого пользователя");
        
        if (Status == InviteStatus.Expired)
            throw new InvalidOperationException("Приглашение истекло");

        if (Status == InviteStatus.Activated)
            throw new InvalidOperationException("Приглашение уже активировано");

        if (Status == InviteStatus.Revoked)
            throw new InvalidOperationException("Приглашение был отозвано");

        Status = InviteStatus.Activated;
        ActivatedByUserId = user.Id;
        
        user.SetRole(Role);
        user.AttachToTeam(teamId);
    }

    public void Revoke()
    {
        if (Status == InviteStatus.Activated)
            throw new InvalidOperationException("Нельзя отозвать уже принятое приглашение");

        Status = InviteStatus.Revoked;
    }
}