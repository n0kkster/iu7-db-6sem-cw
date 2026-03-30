using Analyzer.Domain.Entities;
using Analyzer.IntegrationTests.Fixtures;
using Analyzer.Infrastructure.Persistence;
using FluentAssertions;

namespace Analyzer.IntegrationTests.Repositories;

[Collection("Database collection")]
public class TeamRepositoryTests(SharedDatabaseFixture fixture)
{
    [Fact]
    public async Task AddAsync_ShouldSaveTeamToDatabase()
    {
        // Arrange
        var team = new Team("Alpha Team", "Core backend developers");
        await using var context = fixture.CreateContext();
        var repository = new TeamRepository(context);

        // Act
        await repository.AddAsync(team);

        // Assert
        await using var assertContext = fixture.CreateContext();
        var savedTeam = await assertContext.Teams.FindAsync(team.Id);

        savedTeam.Should().NotBeNull();
        savedTeam!.Name.Should().Be("Alpha Team");
        savedTeam.Description.Should().Be("Core backend developers");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnTeam_WithLoadMembers()
    {
        // Arrange
        var team = new Team("Beta Team", "Frontend developers");

        var user1 = new User("user1", "user1@test.com", "hash");
        var user2 = new User("user2", "user2@test.com", "hash");
        user1.AttachToTeam(team.Id);
        user2.AttachToTeam(team.Id);

        await using var context = fixture.CreateContext();
        await context.Teams.AddAsync(team);
        await context.Users.AddRangeAsync(user1, user2);
        await context.SaveChangesAsync();

        var repository = new TeamRepository(context);

        // Act
        var result = await repository.GetByIdAsync(team.Id);

        // Assert
        result.Should().NotBeNull();
        result!.MemberIds.Should().HaveCount(2);
        result.MemberIds.Should().Contain([user1.Id, user2.Id]);
    }

    [Fact]
    public async Task UpdateAsync_ShouldModifyTeamDetails()
    {
        // Arrange
        var team = new Team("Old Name", "Old Desc");
        await using var setupContext = fixture.CreateContext();
        await setupContext.Teams.AddAsync(team);
        await setupContext.SaveChangesAsync();

        await using var actContext = fixture.CreateContext();
        var repository = new TeamRepository(actContext);

        // Act
        var teamToUpdate = await repository.GetByIdAsync(team.Id);
        teamToUpdate!.UpdateProfile("New Name", "New Desc");
        await repository.UpdateAsync(teamToUpdate);

        // Assert
        await using var assertContext = fixture.CreateContext();
        var updatedTeam = await assertContext.Teams.FindAsync(team.Id);

        updatedTeam!.Name.Should().Be("New Name");
        updatedTeam.Description.Should().Be("New Desc");
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveTeamFromDatabase()
    {
        // Arrange
        var team = new Team("Team to delete", "Will be deleted");
        await using var setupContext = fixture.CreateContext();
        await setupContext.Teams.AddAsync(team);
        await setupContext.SaveChangesAsync();

        await using var actContext = fixture.CreateContext();
        var repository = new TeamRepository(actContext);

        // Act
        await repository.DeleteAsync(team.Id);

        // Assert
        await using var assertContext = fixture.CreateContext();
        var deletedTeam = await assertContext.Teams.FindAsync(team.Id);

        deletedTeam.Should().BeNull();
    }
}