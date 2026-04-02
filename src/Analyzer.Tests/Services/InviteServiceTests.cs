using Analyzer.Application.Interfaces.Repositories;
using Analyzer.Application.Interfaces.Services;
using Analyzer.Application.Services;
using Analyzer.Domain.Entities;
using Analyzer.Domain.Enums;
using Analyzer.Shared.DTO;
using Moq;

namespace Analyzer.Tests.Services;

public class InviteServiceTests
{
    private readonly Mock<IInviteRepository> _inviteRepoMock;
    private readonly Mock<ITeamService> _teamServiceMock;
    private readonly InviteService _inviteService;

    public InviteServiceTests()
    {
        _inviteRepoMock = new Mock<IInviteRepository>();
        _teamServiceMock = new Mock<ITeamService>();
        _inviteService = new InviteService(_inviteRepoMock.Object, _teamServiceMock.Object);
    }

    [Fact]
    public async Task GenerateInviteAsync_TeamExists_AddsInviteAndReturnsCode()
    {
        // Arrange
        var dto = new GenerateInviteDto { Email = "new@test.com", TeamId = Guid.NewGuid(), ValidForDays = 3, Role = Role.Developer };
        _teamServiceMock.Setup(t => t.ExistsAsync(dto.TeamId)).ReturnsAsync(true);

        // Act
        var invite = await _inviteService.GenerateInviteAsync(dto);

        // Assert
        Assert.NotEmpty(invite.Code);
        _inviteRepoMock.Verify(r => r.AddAsync(It.Is<Invite>(i => i.TeamId == dto.TeamId && i.Role == dto.Role)), Times.Once);
    }

    [Fact]
    public async Task GenerateInviteAsync_TeamNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var dto = new GenerateInviteDto { TeamId = Guid.NewGuid() };
        _teamServiceMock.Setup(t => t.ExistsAsync(dto.TeamId)).ReturnsAsync(false);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => _inviteService.GenerateInviteAsync(dto));
        Assert.Equal("Команда не найдена", ex.Message);
    }

    [Fact]
    public async Task AcceptInviteAsync_ValidCode_ActivatesAndAddsToTeam()
    {
        // Arrange
        var email = "test@test.com";
        var teamId = Guid.NewGuid();
        var invite = new Invite(email, 7, teamId, Role.Developer);
        var user = new User("username", email, "hash");

        _inviteRepoMock.Setup(r => r.GetByCodeAsync(invite.Code)).ReturnsAsync(invite);

        // Act
        await _inviteService.AcceptInviteAsync(invite.Code, user);

        // Assert
        Assert.Equal(InviteStatus.Activated, invite.Status);
        _teamServiceMock.Verify(t => t.AddMemberAsync(teamId, user), Times.Once);
        _inviteRepoMock.Verify(r => r.UpdateAsync(invite), Times.Once);
    }
    [Fact]
    public async Task AcceptInviteAsync_InvalidCode_ThrowsKeyNotFoundException()
    {
        // Arrange
        var user = new User("user", "test@test.com", "hash");
        _inviteRepoMock.Setup(r => r.GetByCodeAsync(It.IsAny<string>())).ReturnsAsync((Invite?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _inviteService.AcceptInviteAsync("wrong_code", user));
    }

    [Fact]
    public async Task RevokeInviteAsync_ValidInvite_CallsRevokeAndUpdate()
    {
        // Arrange
        var invite = new Invite("test@test.com", 7, Guid.NewGuid(), Role.Developer);
        _inviteRepoMock.Setup(r => r.GetByIdAsync(invite.Id)).ReturnsAsync(invite);

        // Act
        await _inviteService.RevokeInviteAsync(invite.Id);

        // Assert
        Assert.Equal(InviteStatus.Revoked, invite.Status);
        _inviteRepoMock.Verify(r => r.UpdateAsync(invite), Times.Once);
    }
}