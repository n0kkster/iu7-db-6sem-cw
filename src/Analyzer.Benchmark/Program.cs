using System.Net.Http.Json;
using Analyzer.Benchmark.Models;
using Analyzer.Benchmark.Services;
using Analyzer.Shared.DTO;
using Microsoft.Extensions.DependencyInjection;

namespace Analyzer.Benchmark;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("🚀 Запуск бенчмарка...\n");

        // === НАСТРОЙКИ ===
        string pythonScriptPath = "../../misc/generator.py";
        Guid targetTeamId = Guid.Parse("59964fd2-45fc-4511-ad5d-3b6897d1f563");
        string UserLogin = "archi";
        string UserPassword = "aboba123";
        string apiBaseUrl = "http://localhost:1555";

        int minNodes = 1000;
        int maxNodes = 10000;
        int stepNodes = 100;

        int minCycles = 10;
        int maxCycles = 50;
        int stepCycles = 1;
        // =================

        // 1. Инициализация DI контейнера
        var services = new ServiceCollection();

        services.AddSingleton<BenchmarkSession>();
        services.AddTransient<BenchmarkAuthHandler>();
        
        services.AddHttpClient("ApiClient", client => 
        {
            client.BaseAddress = new Uri(apiBaseUrl);
        }).AddHttpMessageHandler<BenchmarkAuthHandler>();

        services.AddSingleton(sp => new TopologyGenerator(pythonScriptPath));
        services.AddSingleton<Plotter>();
        services.AddSingleton(sp => 
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var generator = sp.GetRequiredService<TopologyGenerator>();
            return new BM(httpClientFactory.CreateClient("ApiClient"), generator);
        });

        var serviceProvider = services.BuildServiceProvider();

        // 2. Получение зависимостей
        var http = serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("ApiClient");
        var benchmark = serviceProvider.GetRequiredService<BM>();
        var plotter = serviceProvider.GetRequiredService<Plotter>();
        var session = serviceProvider.GetRequiredService<BenchmarkSession>();

        try
        {
            // 3. Авторизация
            Console.WriteLine("🔐 Авторизация...");
            var loginResponse = await http.PostAsJsonAsync(
                "/api/v1/users/login", new LoginDto()
                {
                    Username = UserLogin,
                    Password = UserPassword
                });
            loginResponse.EnsureSuccessStatusCode();
            
            var authResponse = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
            session.Token = authResponse?.Token;

            // string filename = await benchmark.RunBenchmarkCascadeAsync(
                // targetTeamId, minNodes, maxNodes, stepNodes);
            string filename = await benchmark.RunBenchmarkCyclesAsync(
                targetTeamId, minCycles, maxCycles, stepCycles);

            // 5. Отрисовка графика
            plotter.GenerateGraph(filename);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Произошла критическая ошибка: {ex.Message}");
        }
    }
}