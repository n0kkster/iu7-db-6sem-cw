// using Neo4j.Driver;
using Analyzer.Application.Interfaces;
using Analyzer.Application.Services;
using Analyzer.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSingleton<IGraphRepository, TestGraphRepository>();
builder.Services.AddScoped<IGraphService, GraphService>();

builder.Services.AddControllers();

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
}

app.UseHttpsRedirection();

app.UseCors("AllowBlazorHttpsOrigin");
app.UseCors("AllowBlazorHttpOrigin");

app.MapControllers();

app.Run();
