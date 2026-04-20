using Analyzer.Domain.Entities;
using Analyzer.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Analyzer.Infrastructure.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AnalyzerDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AnalyzerDbContext>>();

        try
        {
            await context.Database.MigrateAsync();

            var adminExists = await context.Users.AnyAsync(u => u.Role == Role.Admin);
            
            if (!adminExists)
            {
                Log.Information("Администратор не найден. Создаю дефолтного администратора...");

                var adminConfig = configuration.GetSection("AdminSettings");
                var username = adminConfig["Username"] ?? "admin";
                var email = adminConfig["Email"] ?? "admin@analyzer.local";
                var rawPassword = adminConfig["Password"] ?? "admin";

                var passwordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(rawPassword);
                var adminUser = User.CreateAdmin(username, email, passwordHash);                
                
                await context.Users.AddAsync(adminUser);
                await context.SaveChangesAsync();

                Log.Information("Глобальный администратор успешно создан.");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка при инициализации базы данных.");
            throw;
        }
    }
}