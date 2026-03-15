using Neo4j.Driver;
using Analyzer.Application.Interfaces.Repositories;
using Analyzer.Application.Interfaces.Services;
using Analyzer.Application.Services;
using Analyzer.Infrastructure.Persistence;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

using Microsoft.OpenApi;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(theme: AnsiConsoleTheme.Code)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var neo4jUri = builder.Configuration["Neo4jSettings:Uri"];
var neo4jUser = builder.Configuration["Neo4jSettings:User"];
var neo4jPass = builder.Configuration["Neo4jSettings:Password"];

builder.Services.AddSingleton(sp =>
    GraphDatabase.Driver(neo4jUri, AuthTokens.Basic(neo4jUser, neo4jPass)));

builder.Services.AddScoped<IGraphRepository, Neo4jGraphRepository>();
builder.Services.AddScoped<IGraphService, GraphService>();
builder.Services.AddScoped<IAnalysisService, AnalysisService>();
builder.Services.AddScoped<ISystemsService, SystemsService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "FaultAnalyzer API", 
        Version = "v1",
        Description = "API для анализа отказоустойчивости систем на базе микросервисной архитектуры"
    });    
});

// Add CORS to allow Blazor app to use API
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorHttpsOrigin",
        policy =>
        {
            policy.WithOrigins("https://localhost:1337")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorHttpOrigin",
        policy =>
        {
            policy.WithOrigins("https://localhost:1777")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
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

app.UseCors("AllowBlazorHttpsOrigin");
app.UseCors("AllowBlazorHttpOrigin");

app.MapControllers();

app.Run();
