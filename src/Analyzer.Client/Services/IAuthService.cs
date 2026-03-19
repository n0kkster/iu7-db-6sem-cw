using Analyzer.Shared.DTO;

namespace Analyzer.Client.Services;

public interface IAuthService
{
    Task<LoginResult> LoginAsync(LoginDto dto);
}