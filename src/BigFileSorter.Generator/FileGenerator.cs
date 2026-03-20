using System.Diagnostics;
using System.Text;
using BigFileSorter.Core;
using BigFileSorter.Core.IO;

namespace BigFileSorter.Generator;

public sealed class FileGenerator(string outputPath, long targetSize)
{
    private readonly string _outputPath = outputPath;
    private readonly long _targetSize = targetSize;
    private readonly Random _rng = new();

    public void Generate()
    {
        PrintHeader();
        var sw = Stopwatch.StartNew();

        var stringPool = BuildStringPool();
        WriteLines(stringPool, sw);

        sw.Stop();
        PrintSummary(sw.Elapsed);
    }

    private string[] BuildStringPool()
    {
        // Ensure pool is at most half the estimated line count so duplicates are guaranteed
        int avgLineSize = (GeneratorConstants.MaxStringLength + GeneratorConstants.MinStringLength) / 2 + 7; // avg string + "N. " + "\r\n"
        long estimatedLines = _targetSize / avgLineSize;
        int poolSize = (int)Math.Min(GeneratorConstants.StringPoolSize, Math.Max(1, estimatedLines / 2));

        Console.Write("Generating string pool...");
        var pool = new string[poolSize];

        for (int i = 0; i < poolSize; i++)
        {
            int len = _rng.Next(GeneratorConstants.MinStringLength, GeneratorConstants.MaxStringLength + 1);
            pool[i] = GenerateRandomString(len);
        }

        Console.WriteLine($" done ({poolSize:N0} strings)");
        return pool;
    }

    private void WriteLines(string[] stringPool, Stopwatch sw)
    {
        long bytesWritten = 0;
        long lineCount = 0;
        var lastReport = sw.Elapsed;

        using var writer = new BufferedLineWriter(_outputPath);

        while (bytesWritten < _targetSize)
        {
            long number = _rng.NextInt64(1, GeneratorConstants.MaxNumber + 1);
            var str = stringPool[_rng.Next(stringPool.Length)];

            writer.WriteLine(number, Encoding.ASCII.GetBytes(str));

            bytesWritten += number.ToString().Length + FormatConstants.DotSpace.Length + str.Length + FormatConstants.NewLine.Length;
            lineCount++;

            if (sw.Elapsed - lastReport > TimeSpan.FromSeconds(5))
            {
                ReportProgress(bytesWritten, lineCount);
                lastReport = sw.Elapsed;
            }
        }
    }

    private void PrintHeader()
    {
        Console.WriteLine($"Generating file: {_outputPath}");
        Console.WriteLine($"Target size: {_targetSize / (1024.0 * 1024 * 1024):F2} GB ({_targetSize:N0} bytes)");
    }

    private void ReportProgress(long bytesWritten, long lineCount)
    {
        var pct = (double)bytesWritten / _targetSize * 100;
        Console.WriteLine($"  Progress: {pct:F1}% ({bytesWritten / (1024.0 * 1024 * 1024):F2} GB, {lineCount:N0} lines)");
    }

    private void PrintSummary(TimeSpan elapsed)
    {
        var fileInfo = new FileInfo(_outputPath);
        Console.WriteLine($"Done! File size: {fileInfo.Length / (1024.0 * 1024 * 1024):F2} GB ({fileInfo.Length:N0} bytes)");
        Console.WriteLine($"Time: {elapsed.TotalSeconds:F1}s");
    }

    private string GenerateRandomString(int length)
    {
        var chars = new char[length];
        for (int i = 0; i < length; i++)
        {
            chars[i] = GeneratorConstants.AllowedChars[_rng.Next(GeneratorConstants.AllowedChars.Length)];
        }
        return new string(chars);
    }
}
