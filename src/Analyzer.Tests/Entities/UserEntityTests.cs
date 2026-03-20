using Analyzer.Domain.Entities;

namespace Analyzer.Tests.Entities;

public class UserEntityTests
{
    [Theory]
    [InlineData("", "test@test.com", "hash")]
    [InlineData("user", "", "hash")]
    [InlineData("user", "test@test.com", "")]
    [InlineData(" ", " ", " ")]
    public void User_Constructor_WithEmptyArguments_ThrowsArgumentException(string username, string email, string hash)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new User(username, email, hash));
    }

    [Fact]
    public void User_Constructor_WithInvalidEmail_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new User("user", "invalid_email", "hash"));
    }

    [Fact]
    public void User_UpdateProfile_WithEmptyValues_DoesNotChangeProperties()
    {
        // Arrange
        var user = new User("old_name", "old@test.com", "hash");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => user.UpdateProfile("", "  ")); 
    }

    [Fact]
    public void User_UpdateProfile_WithInvalidEmail_DoesNotChangeProperties()
    {
        // Arrange
        var user = new User("old_name", "old@test.com", "hash");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => user.UpdateProfile("user", "invalid_email"));
    }

    [Fact]
    public void User_UpdateProfile_WithValidValues_UpdatesProperties()
    {
        // Arrange
        var user = new User("old_name", "old@test.com", "hash");

        // Act
        user.UpdateProfile("new_name", "new@test.com");

        // Assert
        Assert.Equal("new_name", user.Username);
        Assert.Equal("new@test.com", user.Email);
    }

    [Fact]
    public void User_ChangePassword_WithEmptyHash_ThrowsArgumentException()
    {
        // Arrange
        var user = new User("name", "test@test.com", "old_hash");

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => user.ChangePassword("   "));
        Assert.Contains("не может быть пустым", ex.Message);
    }
}