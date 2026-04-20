using Analyzer.Application.Interfaces.Repositories;
using Analyzer.Application.Interfaces.Services;
using Analyzer.Domain.Entities;
using Analyzer.Shared.DTO;
using Analyzer.Application.Interfaces.Providers;

namespace Analyzer.Application.Services;

public class UserService(IUserRepository userRepository,
    IJwtProvider jwtProvider,
    IInviteService inviteService) : IUserService
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IJwtProvider _jwtProvider = jwtProvider;
    private readonly IInviteService _inviteService = inviteService;

    public async Task<Guid> RegisterAsync(RegisterDto dto)
    {
        if (await _userRepository.ExistsByUsernameAsync(dto.Username))
            throw new InvalidOperationException("Пользователь с таким именем уже зарегистрирован.");

        if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 8)
            throw new ArgumentException("Пароль должен содержать не менее 8 символов");

        var (Role, TeamId) = await _inviteService.GetValidatedInviteDetailsAsync(dto.InviteCode, dto.Email);

        string passwordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(dto.Password);
        var user = User.CreateInvitedUser(
            dto.Username, 
            dto.Email, 
            passwordHash, 
            Role, 
            TeamId
        );

        await _userRepository.AddAsync(user);
        await _inviteService.ConsumeInviteAsync(dto.InviteCode, user);

        return user.Id;
    }

    public async Task<string> LoginAsync(LoginDto dto)
    {
        var user = await _userRepository.GetByUsernameAsync(dto.Username);

        if (user is null || !BCrypt.Net.BCrypt.EnhancedVerify(dto.Password, user.PasswordHash))
            throw new InvalidOperationException("Неверный логин или пароль");

        string role = user.Role.ToString();

        return _jwtProvider.GenerateToken(user, role);
    }

    public async Task<UserDto> GetProfileAsync(Guid userId)
    {
        var user = await GetUserOrThrowAsync(userId);
        return MapToDto(user);
    }

    public async Task<IReadOnlyCollection<UserDto>> GetAllUsersAsync()
    {
        var users = await _userRepository.GetAllUsersAsync();
        return users.Select(MapToDto).ToList();
    }

    public async Task UpdateProfileAsync(Guid userId, UpdateProfileDto dto)
    {
        var user = await GetUserOrThrowAsync(userId);

        if (!string.Equals(user.Username, dto.Username, StringComparison.OrdinalIgnoreCase))
            if (await _userRepository.ExistsByUsernameAsync(dto.Username))
                throw new InvalidOperationException("Это имя пользователя уже используется другим пользователем.");

        if (!string.Equals(user.Email, dto.Email, StringComparison.OrdinalIgnoreCase))
            if (await _userRepository.ExistsByEmailAsync(dto.Email))
                throw new InvalidOperationException("Данная почта уже используется другим пользователем.");


        user.UpdateProfile(dto.Username, dto.Email);

        await _userRepository.UpdateAsync(user);
    }

    public async Task ChangePasswordAsync(Guid userId, string oldPassword, string newPassword)
    {
        var user = await GetUserOrThrowAsync(userId);

        if (!BCrypt.Net.BCrypt.EnhancedVerify(oldPassword, user.PasswordHash))
            throw new InvalidOperationException("Текущий пароль указан неверно.");

        string newHash = BCrypt.Net.BCrypt.EnhancedHashPassword(newPassword);

        user.ChangePassword(newHash);

        await _userRepository.UpdateAsync(user);
    }

    private async Task<User> GetUserOrThrowAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId)
                 ?? throw new KeyNotFoundException($"Пользователь с ID {userId} не найден.");

        return user;
    }

    public async Task DeleteAsync(Guid userId)
    {
        var _ = await _userRepository.GetByIdAsync(userId)
                 ?? throw new KeyNotFoundException($"Пользователь с ID {userId} не найден.");
                 
        await _userRepository.DeleteAsync(userId);
    }

    private UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role,
            TeamId = user.TeamId ?? Guid.Empty
        };
    }
}