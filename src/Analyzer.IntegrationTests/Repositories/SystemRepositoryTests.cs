using Analyzer.Domain.Entities;
using Analyzer.IntegrationTests.Fixtures;
using Analyzer.Infrastructure.Persistence;
using FluentAssertions;

namespace Analyzer.IntegrationTests.Repositories;

[Collection("Database collection")]
public class SystemRepositoryTests(SharedDatabaseFixture fixture)
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
    public async Task AddAsync_ShouldSaveSystemToDatabase()
    {
        // Arrange
        var team = await CreateTestTeamAsync();
        var system = new ITSystem($"System_{Guid.NewGuid()}", "Main billing system", team.Id);

        await using var context = fixture.CreateContext();
        var repository = new SystemRepository(context);

        // Act
        await repository.AddAsync(system);

        // Assert
        await using var assertContext = fixture.CreateContext();
        var savedSystem = await assertContext.ITSystems.FindAsync(system.Id);

        savedSystem.Should().NotBeNull();
        savedSystem!.Name.Should().Be(system.Name);
        savedSystem.TeamId.Should().Be(team.Id);
    }

    [Fact]
    public async Task GetByTeamIdAsync_ShouldReturnAllTeamSystems()
    {
        // Arrange
        var team = await CreateTestTeamAsync();
        var system1 = new ITSystem($"Sys1_{Guid.NewGuid()}", "Desc1", team.Id);
        var system2 = new ITSystem($"Sys2_{Guid.NewGuid()}", "Desc2", team.Id);

        await using var context = fixture.CreateContext();
        await context.ITSystems.AddRangeAsync(system1, system2);
        await context.SaveChangesAsync();

        var repository = new SystemRepository(context);

        // Act
        var systems = await repository.GetByTeamIdAsync(team.Id);

        // Assert
        systems.Should().HaveCountGreaterThanOrEqualTo(2);
        systems.Select(s => s.Id).Should().Contain(new[] { system1.Id, system2.Id });
    }

    [Fact]
    public async Task UpdateAsync_ShouldModifySystemDetails()
    {
        // Arrange
        var team = await CreateTestTeamAsync();
        var system = new ITSystem("Old Name", "Old Desc", team.Id);

        await using var setupContext = fixture.CreateContext();
        await setupContext.ITSystems.AddAsync(system);
        await setupContext.SaveChangesAsync();

        await using var actContext = fixture.CreateContext();
        var repository = new SystemRepository(actContext);

        // Act
        var systemToUpdate = await repository.GetByIdAsync(system.Id);
        var newName = $"New Name {Guid.NewGuid()}";
        systemToUpdate!.UpdateDetails(newName, "New Desc");
        await repository.UpdateAsync(systemToUpdate);

        // Assert
        await using var assertContext = fixture.CreateContext();
        var updatedSystem = await assertContext.ITSystems.FindAsync(system.Id);

        updatedSystem!.Name.Should().Be(newName);
        updatedSystem.Description.Should().Be("New Desc");
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveSystem()
    {
        // Arrange
        var team = await CreateTestTeamAsync();
        var system = new ITSystem($"To Delete {Guid.NewGuid()}", "Desc", team.Id);

        await using var setupContext = fixture.CreateContext();
        await setupContext.ITSystems.AddAsync(system);
        await setupContext.SaveChangesAsync();

        await using var actContext = fixture.CreateContext();
        var repository = new SystemRepository(actContext);

        // Act
        await repository.DeleteAsync(system.Id);

        // Assert
        await using var assertContext = fixture.CreateContext();
        var deletedSystem = await assertContext.ITSystems.FindAsync(system.Id);

        deletedSystem.Should().BeNull();
    }
}