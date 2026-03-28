using Analyzer.Domain.Enums;
using System.Security.Cryptography;
using System.Text;

namespace Analyzer.Domain.Entities;

public class Invite
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
    
    public Guid? ActivatedByUserId { get; private set; }
    public Role Role { get; init; }
    public string Code { get; init; }
    public DateTimeOffset ExpirationDate { get; private set; }
    public Guid TeamId { get; init; }

    // Давим варнинг, потому что данный конструктор нужен только для EF.Core
#pragma warning disable CS8618
    private Invite() { }
#pragma warning restore CS8618

    public Invite(string targetEmail, int validForDays, Guid teamId, Role role)
    {
        TeamId = teamId == Guid.Empty ? 
            throw new ArgumentException("Команда обязательна", nameof(teamId)) : 
            teamId;

        Role = role;
        Status = InviteStatus.Pending;
        
        Code = Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(targetEmail)));

        ExpirationDate = DateTimeOffset.UtcNow.AddDays(validForDays);
    }

    private bool CheckTarget(string targetEmail)
    {
        return Code == Convert.ToHexString(
                            SHA256.HashData(
                                Encoding.UTF8.GetBytes(targetEmail)));
    }

    public void ActivateUser(User user)
    {
        if (!CheckTarget(user.Email))
            throw new ArgumentException("Приглашение не предназначено для этого пользователя");
        
        if (Status == InviteStatus.Expired)
            throw new InvalidOperationException("Приглашение истекло");

        if (Status == InviteStatus.Activated)
            throw new InvalidOperationException("Приглашение уже активировано");

        if (Status == InviteStatus.Revoked)
            throw new InvalidOperationException("Приглашение было отозвано");

        Status = InviteStatus.Activated;
        ActivatedByUserId = user.Id;
        
        user.SetRole(Role);
        user.AttachToTeam(TeamId); 
    }

    public void Revoke()
    {
        if (Status == InviteStatus.Activated)
            throw new InvalidOperationException("Нельзя отозвать уже принятое приглашение");

        Status = InviteStatus.Revoked;
    }
}