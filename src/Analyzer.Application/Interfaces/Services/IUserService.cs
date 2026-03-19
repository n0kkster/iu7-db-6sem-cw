using Analyzer.Shared.DTO;

namespace Analyzer.Application.Interfaces.Services;

public interface IUserService
{
    Task<Guid> RegisterAsync(RegisterUserDto dto);
    Task<string> LoginAsync(LoginDto dto);
    
    Task<UserDto> GetProfileAsync(Guid userId);
    Task UpdateProfileAsync(Guid userId, UpdateProfileDto dto);
    Task ChangePasswordAsync(Guid userId, string oldPassword, string newPassword);
}