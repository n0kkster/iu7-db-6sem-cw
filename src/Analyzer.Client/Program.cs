using MudBlazor.Services;
using Analyzer.Client.Components;
using Analyzer.Client.Infrastructure;

using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(theme: AnsiConsoleTheme.Code)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<ErrorHandler>();
builder.Services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<ErrorHandler>();    
    handler.InnerHandler = new HttpClientHandler();
    var client = new HttpClient(handler)
    {
        BaseAddress = new Uri("http://localhost:1555") 
    };

    return client;
});

builder.Services.AddMudServices();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();