namespace BigFileSorter.Sorter;

public static class SorterConstants
{
    public const long DefaultChunkSizeBytes = 256L * 1024 * 1024;
    public const int DefaultParallelSorters = 8;
    public const int InputReadBufferSize = 4 * 1024 * 1024;
    public const int ChunkWriteBufferSize = 256 * 1024 * 1024;
    public const int MergeReadBufferSize = 512 * 1024;
    public const int MergeWriteBufferSize = 256 * 1024 * 1024;
    public const int ProgressReportInterval = 10_000_000;
    public const string TempDirectoryName = "sort_temp";
    public const string ChunkFileNameFormat = "chunk_{0:D5}.tmp";
}
