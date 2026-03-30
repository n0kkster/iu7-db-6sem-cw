using Analyzer.Domain.Entities;
using Analyzer.Domain.Enums;
using Analyzer.IntegrationTests.Fixtures;
using Analyzer.Infrastructure.Persistence;
using FluentAssertions;

namespace Analyzer.IntegrationTests.Repositories;

[Collection("Database collection")]
public class InviteRepositoryTests(SharedDatabaseFixture fixture)
{
    private async Task<Team> CreateTestTeamAsync()
    {
        var team = new Team($"Team_{Guid.NewGuid()}", "Test Team");
        await using var context = fixture.CreateContext();
        await context.Teams.AddAsync(team);
        await context.SaveChangesAsync();
        return team;
    }

    [Fact]
    public async Task AddAsync_ShouldSaveInviteToDatabase()
    {
        // Arrange
        var team = await CreateTestTeamAsync();
        var targetEmail = $"invite_{Guid.NewGuid()}@test.com";
        var invite = new Invite(targetEmail, 7, team.Id, Role.Developer);

        await using var context = fixture.CreateContext();
        var repository = new InviteRepository(context);

        // Act
        await repository.AddAsync(invite);

        // Assert
        await using var assertContext = fixture.CreateContext();
        var savedInvite = await assertContext.Invites.FindAsync(invite.Id);

        savedInvite.Should().NotBeNull();
        savedInvite!.Code.Should().Be(invite.Code);
        savedInvite.TeamId.Should().Be(team.Id);
        savedInvite.Role.Should().Be(Role.Developer);
        savedInvite.Status.Should().Be(InviteStatus.Pending);
    }

    [Fact]
    public async Task GetByCodeAsync_ShouldReturnInvite()
    {
        // Arrange
        var team = await CreateTestTeamAsync();
        var invite = new Invite($"code_{Guid.NewGuid()}@test.com", 7, team.Id, Role.SRE);

        await using var context = fixture.CreateContext();
        await context.Invites.AddAsync(invite);
        await context.SaveChangesAsync();

        var repository = new InviteRepository(context);

        // Act
        var result = await repository.GetByCodeAsync(invite.Code);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(invite.Id);
    }

    [Fact]
    public async Task UpdateAsync_ShouldModifyInviteStatus()
    {
        // Arrange
        var team = await CreateTestTeamAsync();
        var invite = new Invite($"update_{Guid.NewGuid()}@test.com", 7, team.Id, Role.Admin);

        await using var setupContext = fixture.CreateContext();
        await setupContext.Invites.AddAsync(invite);
        await setupContext.SaveChangesAsync();

        await using var actContext = fixture.CreateContext();
        var repository = new InviteRepository(actContext);

        // Act
        var inviteToUpdate = await repository.GetByIdAsync(invite.Id);

        inviteToUpdate!.Revoke();
        await repository.UpdateAsync(inviteToUpdate);

        // Assert
        await using var assertContext = fixture.CreateContext();
        var updatedInvite = await assertContext.Invites.FindAsync(invite.Id);

        updatedInvite!.Status.Should().Be(InviteStatus.Revoked);
    }
}