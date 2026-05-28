using System.Diagnostics;

namespace Analyzer.Benchmark.Services;

public class Plotter
{
    public void GenerateGraph(string csvFilePath)
    {
        string pltScript = $@"
set terminal pdfcairo enhanced color size 20cm, 10cm font 'Arial,16'
set output 'plot.pdf'

set title 'Зависимость времени анализа от размера графа' font ',16'
set xlabel 'Количество узлов, единиц'
set ylabel 'Время выполнения, мс'

set grid ytics lc rgb '#e0e0e0' lw 1 lt 0
set grid xtics lc rgb '#e0e0e0' lw 1 lt 0

set datafile separator ','

plot '{csvFilePath}' using 1:2 with linespoints \
        linewidth 2 pt 5 ps 0.5 \
        title 'Время выполнения анализа, мс', \
";


        File.WriteAllText("plot.gp", pltScript);

        var processInfo = new ProcessStartInfo
        {
            FileName = "gnuplot", 
            Arguments = "plot.gp",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        
        try 
        {
            using var process = Process.Start(processInfo);
            process?.WaitForExit();
            Console.WriteLine("📈 График сгенерирован.");
        }
        catch 
        {
            Console.WriteLine("⚠️ Ошибка: Gnuplot не установлен или не добавлен в PATH.");
        }
    }
}