using System.Text;
using Analyzer.Application.Interfaces.Providers;
using Analyzer.Application.Interfaces.Repositories;
using Analyzer.Application.Interfaces.Services;
using Analyzer.Application.Services;
using Analyzer.Infrastructure.Data;
using Analyzer.Infrastructure.Persistence;
using Analyzer.Infrastructure.Providers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Neo4j.Driver;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

// ============================================================================
// 🔐 НАСТРОЙКА ЛОГГЕРА
// ============================================================================
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .WriteTo.Console(theme: AnsiConsoleTheme.Code)
    .CreateLogger();

try
{
    Log.Information("🚀 Запуск API-сервиса FaultAnalyzer");

    var builder = WebApplication.CreateBuilder(args);

    // ============================================================================
    // 🗄️ 1. БАЗЫ ДАННЫХ (SQL & GRAPH)
    // ============================================================================
    
    // PostgreSQL + EF Core
    builder.Services.AddDbContext<AnalyzerDbContext>(options =>
    {
        options.UseNpgsql(builder.Configuration.GetConnectionString("PGConnection"));
        options.UseSnakeCaseNamingConvention();
    });

    // Neo4j Graph Database
    var neo4jUri = builder.Configuration["Neo4jSettings:Uri"];
    var neo4jUser = builder.Configuration["Neo4jSettings:User"];
    var neo4jPass = builder.Configuration["Neo4jSettings:Password"];

    builder.Services.AddSingleton(sp =>
        GraphDatabase.Driver(neo4jUri, AuthTokens.Basic(neo4jUser, neo4jPass)));

    // ============================================================================
    // 🏗️ 2. РЕПОЗИТОРИИ (DATA ACCESS)
    // ============================================================================
    
    builder.Services.AddScoped<IGraphRepository, Neo4jGraphRepository>();
    builder.Services.AddScoped<ISystemRepository, SystemRepository>();
    builder.Services.AddScoped<ITeamRepository, TeamRepository>();
    builder.Services.AddScoped<IInviteRepository, InviteRepository>();
    builder.Services.AddScoped<IUserRepository, UserRepository>();

    // ============================================================================
    // ⚙️ 3. БИЗНЕС-ЛОГИКА (SERVICES)
    // ============================================================================
    
    builder.Services.AddScoped<IJwtProvider, JwtProvider>();
    builder.Services.AddScoped<IGraphService, GraphService>();
    builder.Services.AddScoped<IAnalysisService, AnalysisService>();
    builder.Services.AddScoped<ISystemService, SystemService>();
    builder.Services.AddScoped<ITeamService, TeamService>();
    builder.Services.AddScoped<IInviteService, InviteService>();
    builder.Services.AddScoped<IUserService, UserService>();

    // ============================================================================
    // 🔐 4. АУТЕНТИФИКАЦИЯ И БЕЗОПАСНОСТЬ
    // ============================================================================
    
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
            };
        });

    builder.Services.AddAuthorization();

    // Настройка CORS для Blazor-клиентов
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("BlazorClientPolicy", policy =>
        {
            policy.WithOrigins(
                    "https://localhost:1337", 
                    "https://localhost:1777")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });

    // ============================================================================
    // 📖 5. API, КОНТРОЛЛЕРЫ И SWAGGER
    // ============================================================================
    
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddOpenApi();

    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo 
        { 
            Title = "FaultAnalyzer API", 
            Version = "v1",
            Description = "API для анализа отказоустойчивости систем на базе микросервисной архитектуры"
        });
    });

    // ============================================================================
    // 🚀 BUILD & CONFIGURE APP
    // ============================================================================
    
    var app = builder.Build();

    // ============================================================================
    // 🔐 6. MIDDLEWARE PIPELINE
    // ============================================================================
    
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.UseSwagger();
        app.UseSwaggerUI(options => 
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "FaultAnalyzer API v1");
            options.RoutePrefix = string.Empty;
        });
    }

    app.UseHttpsRedirection();

    // Применяем политику CORS
    app.UseCors("BlazorClientPolicy");

    app.UseAuthentication();
    app.UseAuthorization();

    // Маппинг контроллеров
    app.MapControllers();

    // ============================================================================
    // 🚀 RUN
    // ============================================================================
    
    Log.Information("✅ API готов к приему запросов");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "❌ Критическая ошибка при запуске API");
    throw;
}
finally
{
    Log.CloseAndFlush();
}