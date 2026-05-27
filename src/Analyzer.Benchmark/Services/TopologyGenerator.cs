using System.Diagnostics;
using System.Text.Json;

namespace Analyzer.Benchmark.Services;

public class TopologyGenerator(string scriptPath)
{
    private readonly string _scriptPath = scriptPath;
    private readonly string _outputJsonPath = "generated.json";

    public string OutputFilePath => _outputJsonPath;

    public async Task GenerateAsync(int nodes, int links, int cycles)
    {
        // 1. Запуск Python
        var processInfo = new ProcessStartInfo
        {
            FileName = "python",
            Arguments = $"{_scriptPath} -c {nodes} -l {links} -cy {cycles}",
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(processInfo) 
            ?? throw new Exception("Не удалось запустить Python процесс.");
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            string error = await process.StandardError.ReadToEndAsync();
            throw new Exception($"Ошибка выполнения Python: {error}");
        }

        if (!File.Exists(_outputJsonPath))
            throw new FileNotFoundException($"Файл {_outputJsonPath} не был создан.");
    }
}