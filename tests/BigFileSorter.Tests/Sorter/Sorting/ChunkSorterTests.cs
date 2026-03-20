using System.Text;
using BigFileSorter.Sorter.IO;
using BigFileSorter.Sorter.Sorting;

namespace BigFileSorter.Tests.Sorter.Sorting;

[TestFixture]
public class ChunkSorterTests
{
    private string _tempDir = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"bigfilesorter_chunk_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    [Test]
    public void SortAndWriteChunk_SortsEntriesAndWritesToFile()
    {
        var buckets = BuildBuckets(("Banana", 3), ("Apple", 1), ("Cherry", 2));

        var path = ChunkSorter.SortAndWriteChunk(buckets, _tempDir, 0);

        Assert.That(File.Exists(path), Is.True);
        var result = ReadBinaryChunk(path);
        Assert.That(result[0], Is.EqualTo((1L, "Apple")));
        Assert.That(result[1], Is.EqualTo((3L, "Banana")));
        Assert.That(result[2], Is.EqualTo((2L, "Cherry")));
    }

    [Test]
    public void SortAndWriteChunk_ChunkIndexAffectsFilename()
    {
        var buckets = BuildBuckets(("A", 1));

        var path0 = ChunkSorter.SortAndWriteChunk(buckets, _tempDir, 0);
        var path5 = ChunkSorter.SortAndWriteChunk(buckets, _tempDir, 5);

        Assert.That(Path.GetFileName(path0), Is.EqualTo("chunk_00000.tmp"));
        Assert.That(Path.GetFileName(path5), Is.EqualTo("chunk_00005.tmp"));
    }

    [Test]
    public void SortAndWriteChunk_SameStringSortsByNumber()
    {
        var buckets = BuildBuckets(("Same", 5), ("Same", 1), ("Same", 3));

        var path = ChunkSorter.SortAndWriteChunk(buckets, _tempDir, 0);

        var result = ReadBinaryChunk(path);
        Assert.That(result[0], Is.EqualTo((1L, "Same")));
        Assert.That(result[1], Is.EqualTo((3L, "Same")));
        Assert.That(result[2], Is.EqualTo((5L, "Same")));
    }

    [Test]
    public void SortAndWriteChunk_SingleEntry_WritesCorrectly()
    {
        var buckets = BuildBuckets(("Only one", 42));

        var path = ChunkSorter.SortAndWriteChunk(buckets, _tempDir, 0);

        var result = ReadBinaryChunk(path);
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0], Is.EqualTo((42L, "Only one")));
    }

    private static ChunkBuckets BuildBuckets(params (string Str, long Num)[] items)
    {
        var buckets = new ChunkBuckets();
        foreach (var (str, num) in items)
        {
            buckets.Add(Encoding.ASCII.GetBytes(str), num);
        }
        return buckets;
    }

    private static List<(long Number, string StringPart)> ReadBinaryChunk(string path)
    {
        var result = new List<(long, string)>();
        using var reader = new BinaryChunkReader(path);
        while (reader.TryReadEntry(out long number, out byte[] stringBytes))
        {
            var str = Encoding.ASCII.GetString(stringBytes);
            result.Add((number, str));
        }
        return result;
    }
}
