using Analyzer.Domain.Entities;
using Analyzer.Domain.Enums;
using Analyzer.IntegrationTests.Fixtures;
using Analyzer.Infrastructure.Persistence;
using FluentAssertions;

namespace Analyzer.IntegrationTests.Repositories;

[Collection("Database collection")]
public class UserRepositoryTests(SharedDatabaseFixture fixture)
{
    [Fact]
    public async Task AddAsync_ShouldSaveUserToDatabase()
    {
        // Arrange
        var team = new Team("User Test Team", "Desc");

        await using var setupContext = fixture.CreateContext();
        await setupContext.Teams.AddAsync(team);
        await setupContext.SaveChangesAsync();

        var username = $"user_{Guid.NewGuid()}";
        var email = $"{Guid.NewGuid()}@test.com";
        var user = User.CreateInvitedUser(
            username, 
            email, 
            "hash123", 
            Role.Developer, 
            team.Id
        );

        await using var context = fixture.CreateContext();
        var repository = new UserRepository(context);

        // Act
        await repository.AddAsync(user);

        // Assert
        await using var assertContext = fixture.CreateContext();
        var savedUser = await assertContext.Users.FindAsync(user.Id);

        savedUser.Should().NotBeNull();
        savedUser!.Username.Should().Be(username);
        savedUser.Email.Should().Be(email);
        savedUser.Role.Should().Be(Role.Developer);
    }

    [Fact]
    public async Task GetByUsernameAsync_ShouldReturnUser_WhenUserExists()
    {
        // Arrange
        var team = new Team("User Test Team", "Desc");

        await using var setupContext = fixture.CreateContext();
        await setupContext.Teams.AddAsync(team);
        await setupContext.SaveChangesAsync();

        var username = $"findme_{Guid.NewGuid()}";
        var user = User.CreateInvitedUser(
            username, 
            $"{Guid.NewGuid()}@test.com", 
            "hash",
            Role.Developer,
            team.Id
        );

        await using var context = fixture.CreateContext();
        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();

        var repository = new UserRepository(context);

        // Act
        var result = await repository.GetByUsernameAsync(username);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task UpdateAsync_ShouldModifyUserDetails()
    {
        // Arrange
        var team = new Team("User Test Team", "Desc");

        await using var setupContext = fixture.CreateContext();
        await setupContext.Teams.AddAsync(team);
        
        var user = User.CreateInvitedUser(
            $"old_{Guid.NewGuid()}", 
            $"{Guid.NewGuid()}@test.com", 
            "oldHash",
            Role.Developer,
            team.Id
        );
        
        await setupContext.Users.AddAsync(user);
        await setupContext.SaveChangesAsync();
        
        await using var actContext = fixture.CreateContext();
        var repository = new UserRepository(actContext);

        // Act
        var userToUpdate = await repository.GetByIdAsync(user.Id);
        var newUsername = $"new_{Guid.NewGuid()}";
        userToUpdate!.UpdateProfile(newUsername, "new@test.com");
        userToUpdate.ChangePassword("newHash");

        await repository.UpdateAsync(userToUpdate);

        // Assert
        await using var assertContext = fixture.CreateContext();
        var updatedUser = await assertContext.Users.FindAsync(user.Id);

        updatedUser!.Username.Should().Be(newUsername);
        updatedUser.Email.Should().Be("new@test.com");
        updatedUser.PasswordHash.Should().Be("newHash");
        updatedUser.Role.Should().Be(Role.Developer);
        updatedUser.TeamId.Should().Be(team.Id);
    }

    [Fact]
    public async Task ExistsByUsernameAsync_ShouldReturnTrue_WhenExists()
    {
        // Arrange
        var team = new Team("User Test Team", "Desc");

        await using var setupContext = fixture.CreateContext();
        await setupContext.Teams.AddAsync(team);
        await setupContext.SaveChangesAsync();

        var username = $"exists_{Guid.NewGuid()}";
        var user = User.CreateInvitedUser(
            username, 
            $"{Guid.NewGuid()}@test.com", 
            "hash",
            Role.Developer,
            team.Id
        );

        await using var context = fixture.CreateContext();
        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();

        var repository = new UserRepository(context);

        // Act
        var exists = await repository.ExistsByUsernameAsync(username);

        // Assert
        exists.Should().BeTrue();
    }
}