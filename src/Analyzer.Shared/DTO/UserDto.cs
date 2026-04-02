using Analyzer.Domain.Enums;

namespace Analyzer.Shared.DTO;

public record UserDto(Guid Id, string Username, string Email,
                      Role Role, Guid TeamId);

public class RegisterDto
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string InviteCode { get; set; } = string.Empty;
}

public class LoginDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public record UpdateProfileDto(string Username, string Email);