using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Analyzer.Shared.DTO;

namespace Analyzer.Benchmark.Services;

public class BM(HttpClient http, TopologyGenerator generator)
{
    private readonly HttpClient _http = http;
    private readonly TopologyGenerator _generator = generator;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
    public string Token { get; set; } = "";

    public async Task RunBenchmarkAsync(Guid teamId, int minNodes, int maxNodes, int step)
    {
        string csvPath = "benchmark_results.csv";
        await File.WriteAllTextAsync(csvPath, "Nodex,AvgExecutionTimeMs\n");

        Console.WriteLine("📊 Начинаем замеры...");

        // Прогрев (Warm-up)
        Console.WriteLine("🔥 Прогрев системы...");
        await RunSingleTopologyTestAsync(teamId, 15, 30);

        for (int nodes = minNodes; nodes <= maxNodes; nodes += step)
        {
            int links = nodes * 2;

            double avgTimeMs = await RunSingleTopologyTestAsync(teamId, nodes, links);

            await File.AppendAllTextAsync(csvPath, $"{nodes},{avgTimeMs.ToString(System.Globalization.CultureInfo.InvariantCulture)}\n");
            
            Console.WriteLine($" Готово ({avgTimeMs:F2} мс).");
        }

        Console.WriteLine($"\n✅ Все замеры завершены. Данные сохранены в {csvPath}");
    }

    private async Task<double> RunSingleTopologyTestAsync(Guid teamId, int nodeCount, int linkCount)
    {
        // 1. Генерация
        int targetCyclesCount = 20;
        await _generator.GenerateAsync(nodeCount, linkCount, targetCyclesCount);

        // 2. Импорт в БД
        Guid systemId = await ImportSystemAsync(_generator.OutputFilePath, teamId);
        var responseComponents = await _http.GetFromJsonAsync<List<ComponentDto>>(
            $"api/v1/components?systemId={systemId}", _jsonOptions);

        if (responseComponents == null || responseComponents.Count == 0)
            throw new Exception("Не удалось получить созданные компоненты от API для системы " + systemId);

        // Вытаскиваем список реальных новых GUID
        var componentIds = responseComponents.Select(c => c.Id).ToList();

        Console.WriteLine($"Анализируем систему {systemId} с {nodeCount} компонентами и {linkCount} связями.");

        // 3. Анализ 10 случайных узлов
        double totalExecutionTime = 0;
        int testRuns = 10;
        int nonzero = 0;
        var random = new Random();

        for (int i = 0; i < testRuns; i++)
        {
            Guid targetNodeId = componentIds[random.Next(componentIds.Count)];

            var response = await _http.GetAsync($"/api/v1/analysis/simulate/{targetNodeId}");
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<CascadingFailureResultDto>(_jsonOptions);
            if (result != null)
            {
                totalExecutionTime += result.ExecutionTime;
                if (result.Nodes.Any())
                    nonzero++;
            }
        }

        // 4. Очистка БД
        var deleteResponse = await _http.DeleteAsync($"/api/v1/systems/{systemId}");
        deleteResponse.EnsureSuccessStatusCode();
        Console.WriteLine($"TOT: {totalExecutionTime}, NZ: {nonzero}");
        return (totalExecutionTime / 1000.0) / testRuns;
    }

    private async Task<double> RunSingleCycleAsync(Guid teamId, int nodeCount, int linkCount, int cycles)
    {
        // 1. Генерация
        await _generator.GenerateAsync(nodeCount, linkCount, cycles);

        // 2. Импорт в БД
        Guid systemId = await ImportSystemAsync(_generator.OutputFilePath, teamId);
        Console.WriteLine($"Анализируем систему {systemId} с {nodeCount} компонентами, {linkCount} связями и {cycles} циклами.");

        // 3. Анализ 10 раз
        double totalExecutionTime = 0;
        int testRuns = 10;

        for (int i = 0; i < testRuns; i++)
        {
            var response = await _http.GetAsync($"/api/v1/analysis/cycles/{systemId}");
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<CycleAnalysisResultDto>(_jsonOptions);
            if (result != null)
                totalExecutionTime += result.ExecutionTime;
        }

        // 4. Очистка БД
        var deleteResponse = await _http.DeleteAsync($"/api/v1/systems/{systemId}");
        deleteResponse.EnsureSuccessStatusCode();
        return totalExecutionTime / testRuns;
    }

    private async Task<Guid> ImportSystemAsync(string filePath, Guid teamId)
    {
        var dto = new CreateITSystemDto
        {
            Name = $"Benchmark System {DateTime.Now.Ticks}",
            Description = "Автоматически сгенерированная топология",
            TeamId = teamId
        };

        using var content = new MultipartFormDataContent();

        await using var fileStream = File.OpenRead(filePath);
        var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        content.Add(fileContent, "file", "generated.json");

        var jsonDto = JsonSerializer.Serialize(dto);
        var dtoContent = new StringContent(jsonDto, System.Text.Encoding.UTF8, "application/json");
        content.Add(dtoContent, "importData");

        var response = await _http.PostAsync("api/v1/systems/import", content);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<Guid>();
    }
}