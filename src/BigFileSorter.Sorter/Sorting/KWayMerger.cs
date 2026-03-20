using BigFileSorter.Core.IO;
using BigFileSorter.Sorter.IO;

namespace BigFileSorter.Sorter.Sorting;

/// <summary>
/// K-way merge of sorted binary chunk files using a PriorityQueue min-heap.
/// </summary>
public sealed class KWayMerger : IDisposable
{
    private readonly BinaryChunkReader[] _readers;
    private readonly MergeEntry[] _entries;
    private readonly PriorityQueue<int, int> _heap;

    public KWayMerger(string[] chunkPaths)
    {
        int k = chunkPaths.Length;
        _readers = new BinaryChunkReader[k];
        _entries = new MergeEntry[k];

        _heap = new PriorityQueue<int, int>(k, new ReaderIndexComparer(this));

        for (int i = 0; i < k; i++)
        {
            _readers[i] = new BinaryChunkReader(chunkPaths[i], bufferSize: SorterConstants.MergeReadBufferSize);
            if (ReadNext(i))
            {
                _heap.Enqueue(i, i);
            }
        }
    }

    public long MergeTo(string outputPath, Action<long>? onProgress = null)
    {
        long lineCount = 0;
        using var writer = new BufferedLineWriter(outputPath, bufferSize: SorterConstants.MergeWriteBufferSize);

        while (_heap.Count > 0)
        {
            _heap.TryDequeue(out int readerIndex, out _);

            var entry = _entries[readerIndex];
            writer.WriteLine(entry.Number, entry.StringBytes);
            lineCount++;

            if (onProgress != null && lineCount % 1_000_000 == 0)
            {
                onProgress(lineCount);
            }

            if (ReadNext(readerIndex))
            {
                _heap.Enqueue(readerIndex, readerIndex);
            }
        }

        return lineCount;
    }

    private bool ReadNext(int readerIndex)
    {
        if (!_readers[readerIndex].TryReadEntry(out long number, out byte[] stringBytes))
        {
            return false;
        }

        _entries[readerIndex] = new MergeEntry(number, stringBytes);
        return true;
    }

    public void Dispose()
    {
        foreach (var reader in _readers)
        {
            reader.Dispose();
        }
    }

    private readonly struct MergeEntry(long number, byte[] stringBytes)
    {
        public long Number { get; } = number;
        public byte[] StringBytes { get; } = stringBytes;
    }

    private sealed class ReaderIndexComparer(KWayMerger merger) : IComparer<int>
    {
        public int Compare(int x, int y)
        {
            int cmp = merger._entries[x].StringBytes.SequenceCompareTo(merger._entries[y].StringBytes);
            if (cmp != 0)
            {
                return cmp;
            }
            return merger._entries[x].Number.CompareTo(merger._entries[y].Number);
        }
    }
}
