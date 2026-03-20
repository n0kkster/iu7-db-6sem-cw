using Analyzer.Domain.Entities;
using Analyzer.Domain.Enums;

namespace Analyzer.Tests.Entities;

public class InviteEntityTests
{
    [Fact]
    public void Invite_CreateValid_HasPendingStatusAndCorrectCode()
    {
        // Arrange & Act
        var email = "test@domain.com";
        var invite = new Invite(email, 7, Guid.NewGuid(), Role.Developer);

        // Assert
        Assert.Equal(InviteStatus.Pending, invite.Status);
        Assert.NotNull(invite.Code);
        Assert.True(invite.ExpirationDate > DateTimeOffset.UtcNow);
    }

    [Fact]
    public void InviteStatus_WhenDatePassed_AutomaticallyReturnsExpired()
    {
        // Arrange
        var invite = new Invite("test@domain.com", -1, Guid.NewGuid(), Role.Developer);

        // Act & Assert
        Assert.Equal(InviteStatus.Expired, invite.Status);
    }

    [Fact]
    public void ActivateUser_WithWrongEmail_ThrowsArgumentException()
    {
        // Arrange
        var invite = new Invite("admin@company.com", 7, Guid.NewGuid(), Role.Admin);
        var hackerUser = new User("hacker", "hacker@company.com", "hash");

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => invite.ActivateUser(hackerUser));
        Assert.Contains("не предназначен", ex.Message);
    }

    [Fact]
    public void ActivateUser_ValidUser_ChangesStatusAndUpdatesUserIntenals()
    {
        // Arrange
        var targetEmail = "dev@company.com";
        var teamId = Guid.NewGuid();
        var invite = new Invite(targetEmail, 7, teamId, Role.Developer);
        var user = new User("dev", targetEmail, "hash");

        // Act
        invite.ActivateUser(user);

        // Assert
        Assert.Equal(InviteStatus.Activated, invite.Status);
        Assert.Equal(user.Id, invite.ActivatedByUserId);

        Assert.Equal(Role.Developer, user.Role);
        Assert.Equal(teamId, user.TeamId);
    }

    [Fact]
    public void ActivateUser_AlreadyActivated_ThrowsInvalidOperationException()
    {
        // Arrange
        var targetEmail = "sre@company.com";
        var invite = new Invite(targetEmail, 7, Guid.NewGuid(), Role.SRE);
        var user = new User("sre_user", targetEmail, "hash");

        invite.ActivateUser(user);

        var user2 = new User("another_sre", targetEmail, "hash2");

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => invite.ActivateUser(user2));
        Assert.Contains("уже активировано", ex.Message);
    }

    [Fact]
    public void ActivateUser_UserAlreadyHasRoleAndTeam_ThrowsInvalidOperationException()
    {
        // Arrange
        var targetEmail = "arch@company.com";
        var invite1 = new Invite(targetEmail, 7, Guid.NewGuid(), Role.Developer);
        var invite2 = new Invite(targetEmail, 7, Guid.NewGuid(), Role.Architect);
        var user = new User("arch_user", targetEmail, "hash");

        // Act
        invite1.ActivateUser(user);

        // Assert
        var ex = Assert.Throws<InvalidOperationException>(() => invite2.ActivateUser(user));
        Assert.Contains("Роль пользователя уже установлена", ex.Message);
    }

    [Fact]
    public void RevokeInvite_WhenPending_ChangesToRevoked()
    {
        // Arrange
        var invite = new Invite("test@company.com", 7, Guid.NewGuid(), Role.Developer);

        // Act
        invite.Revoke();

        // Assert
        Assert.Equal(InviteStatus.Revoked, invite.Status);
    }

    [Fact]
    public void RevokeInvite_WhenActivated_ThrowsInvalidOperationException()
    {
        // Arrange
        var email = "test@company.com";
        var invite = new Invite(email, 7, Guid.NewGuid(), Role.Developer);
        var user = new User("test", email, "hash");
        invite.ActivateUser(user);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => invite.Revoke());
        Assert.Contains("Нельзя отозвать уже принятое", ex.Message);
    }
}