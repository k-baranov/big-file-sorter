using System.Diagnostics;
using System.Threading.Channels;
using BigFileSorter.Core;
using BigFileSorter.Sorter.IO;
using BigFileSorter.Sorter.Parsing;

namespace BigFileSorter.Sorter.Sorting;

/// <summary>
/// External merge sort: splits input into sorted chunks, then k-way merges them.
/// Phase 1 reads the file sequentially, splitting into chunks sorted in parallel.
/// Phase 2 uses a k-way merge with binary chunk files.
/// </summary>
public sealed class ExternalSorter(string inputPath, string outputPath, string? tempDir = null,
    long chunkSizeBytes = SorterConstants.DefaultChunkSizeBytes,
    int parallelSorters = SorterConstants.DefaultParallelSorters)
{
    private readonly string _inputPath = inputPath;
    private readonly string _outputPath = outputPath;
    private readonly string _tempDir = tempDir ?? Path.Combine(Path.GetDirectoryName(inputPath)!, SorterConstants.TempDirectoryName);
    private readonly long _chunkSizeBytes = chunkSizeBytes;
    private readonly int _parallelSorters = parallelSorters;

    public async Task SortAsync()
    {
        var totalSw = Stopwatch.StartNew();

        Directory.CreateDirectory(_tempDir);

        try
        {
            Console.WriteLine("Phase 1: Splitting and sorting chunks...");
            var phase1Sw = Stopwatch.StartNew();
            var (chunkPaths, skippedLines) = await SplitAndSortChunksAsync();
            phase1Sw.Stop();
            Console.WriteLine($"Phase 1 complete: {chunkPaths.Count} chunks in {phase1Sw.Elapsed.TotalSeconds:F1}s");
            if (skippedLines > 0)
            {
                Console.WriteLine($"  Warning: {skippedLines:N0} lines skipped (failed to parse)");
            }

            Console.WriteLine($"Phase 2: Merging {chunkPaths.Count} chunks...");
            var phase2Sw = Stopwatch.StartNew();
            long lineCount;
            using (var merger = new KWayMerger([.. chunkPaths]))
            {
                lineCount = merger.MergeTo(_outputPath, lines =>
                {
                    if (lines % SorterConstants.ProgressReportInterval == 0)
                    {
                        Console.WriteLine($"  Merged {lines:N0} lines...");
                    }
                });
            }
            phase2Sw.Stop();
            Console.WriteLine($"Phase 2 complete: {lineCount:N0} lines merged in {phase2Sw.Elapsed.TotalSeconds:F1}s");

            totalSw.Stop();
            Console.WriteLine($"Sort complete in {totalSw.Elapsed.TotalSeconds:F1}s total");
            Console.WriteLine($"Peak working set: {Process.GetCurrentProcess().PeakWorkingSet64 / (1024.0 * 1024):F0} MB");
        }
        finally
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }
    }

    private async Task<(List<string> ChunkPaths, long SkippedLines)> SplitAndSortChunksAsync()
    {
        int chunkIndex = 0;

        var channel = Channel.CreateBounded<ChunkData>(
            new BoundedChannelOptions(_parallelSorters + 1)
            {
                SingleWriter = true,
                FullMode = BoundedChannelFullMode.Wait
            });

        // Start sorter tasks
        var sortTasks = new Task<List<string>>[_parallelSorters];
        for (int i = 0; i < _parallelSorters; i++)
        {
            sortTasks[i] = Task.Run(() => SortWorker(channel.Reader));
        }

        // Read the file sequentially and send chunks to sorters
        long skippedLines = 0;

        using (var reader = new BufferedLineReader(_inputPath, bufferSize: SorterConstants.InputReadBufferSize))
        {
            var buckets = new ChunkBuckets();
            long chunkBytes = 0;

            while (reader.TryReadLine(out var line))
            {
                if (!LineParser.TryParse(line, out long number, out var stringBytes))
                {
                    skippedLines++;
                    continue;
                }

                buckets.Add(stringBytes, number);
                chunkBytes += line.Length + FormatConstants.NewLine.Length;

                if (chunkBytes >= _chunkSizeBytes)
                {
                    await channel.Writer.WriteAsync(new ChunkData(buckets, chunkIndex++));
                    buckets = new ChunkBuckets();
                    chunkBytes = 0;
                }
            }

            if (buckets.Count > 0)
            {
                await channel.Writer.WriteAsync(new ChunkData(buckets, chunkIndex++));
            }
        }

        channel.Writer.Complete();

        // Wait for all sorters
        var results = await Task.WhenAll(sortTasks);
        var chunkPaths = new List<string>();
        foreach (var paths in results)
        {
            chunkPaths.AddRange(paths);
        }

        chunkPaths.Sort(StringComparer.Ordinal);

        return (chunkPaths, skippedLines);
    }

    private async Task<List<string>> SortWorker(ChannelReader<ChunkData> reader)
    {
        var paths = new List<string>();

        await foreach (var chunk in reader.ReadAllAsync())
        {
            var path = ChunkSorter.SortAndWriteChunk(chunk.Buckets, _tempDir, chunk.Index);
            paths.Add(path);
        }

        return paths;
    }

    private sealed record ChunkData(ChunkBuckets Buckets, int Index);
}
