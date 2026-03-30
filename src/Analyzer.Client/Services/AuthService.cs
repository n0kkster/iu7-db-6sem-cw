using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Analyzer.Shared.DTO;
using Analyzer.Client.Utils;
using Serilog;

namespace Analyzer.Client.Services;

public class AuthService(HttpClient httpClient) : IAuthService
{
    public async Task<LoginResult> LoginAsync(LoginDto dto)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync("/api/v1/users/login", dto);
            
            if (!response.IsSuccessStatusCode)
                return new LoginResult(false, "Неверный логин или пароль", null);

            var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
            if (string.IsNullOrEmpty(authResponse?.Token))
                return new LoginResult(false, "Ошибка получения токена", null);
            
            // Парсим клаймы из токена и добавляем сам токен как клайм
            var claims = JwtHelper.ParseClaimsFromJwt(authResponse.Token).ToList();            
            claims.Add(new Claim("jwt-api-token", authResponse.Token));

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            return new LoginResult(true, null, principal);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка авторизации:");
            return new LoginResult(false, $"Ошибка: {ex.Message}", null);
        }
    }
}

public record LoginResult(bool Success, string? ErrorMessage, ClaimsPrincipal? Principal);
public record AuthResponse(string Token);