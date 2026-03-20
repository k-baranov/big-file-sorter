using BigFileSorter.Sorter.IO;

namespace BigFileSorter.Sorter.Sorting;

/// <summary>
/// Sorts a chunk's buckets and writes the result to a binary temp file.
/// </summary>
public static class ChunkSorter
{
    public static string SortAndWriteChunk(ChunkBuckets buckets, string tempDir, int chunkIndex)
    {
        string chunkPath = Path.Combine(tempDir, string.Format(SorterConstants.ChunkFileNameFormat, chunkIndex));
        using var writer = new BinaryChunkWriter(chunkPath, bufferSize: SorterConstants.ChunkWriteBufferSize);

        foreach (var (stringBytes, numbers) in buckets.GetSortedEntries())
        {
            foreach (var number in numbers)
            {
                writer.Write(number, stringBytes);
            }
        }

        return chunkPath;
    }
}
