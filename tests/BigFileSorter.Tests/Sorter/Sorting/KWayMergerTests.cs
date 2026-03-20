using System.Text;
using BigFileSorter.Sorter.IO;
using BigFileSorter.Sorter.Sorting;

namespace BigFileSorter.Tests.Sorter.Sorting;

[TestFixture]
public class KWayMergerTests
{
    private string _tempDir = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"bigfilesorter_merge_test_{Guid.NewGuid():N}");
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
    public void MergeTo_TwoSortedChunks_MergesCorrectly()
    {
        var chunk1 = CreateBinaryChunk("chunk1.tmp", (1, "Apple"), (3, "Cherry"));
        var chunk2 = CreateBinaryChunk("chunk2.tmp", (2, "Banana"), (4, "Date"));
        var outputPath = Path.Combine(_tempDir, "merged.txt");

        using (var merger = new KWayMerger([chunk1, chunk2]))
        {
            var count = merger.MergeTo(outputPath);
            Assert.That(count, Is.EqualTo(4));
        }

        var result = File.ReadAllLines(outputPath);
        Assert.That(result[0], Is.EqualTo("1. Apple"));
        Assert.That(result[1], Is.EqualTo("2. Banana"));
        Assert.That(result[2], Is.EqualTo("3. Cherry"));
        Assert.That(result[3], Is.EqualTo("4. Date"));
    }

    [Test]
    public void MergeTo_SingleChunk_CopiesAsIs()
    {
        var chunk = CreateBinaryChunk("chunk.tmp", (1, "Apple"), (2, "Banana"));
        var outputPath = Path.Combine(_tempDir, "merged.txt");

        using (var merger = new KWayMerger([chunk]))
        {
            var count = merger.MergeTo(outputPath);
            Assert.That(count, Is.EqualTo(2));
        }

        var result = File.ReadAllLines(outputPath);
        Assert.That(result[0], Is.EqualTo("1. Apple"));
        Assert.That(result[1], Is.EqualTo("2. Banana"));
    }

    [Test]
    public void MergeTo_ChunksWithDuplicateStrings_SortsByNumber()
    {
        var chunk1 = CreateBinaryChunk("chunk1.tmp", (1, "Apple"), (3, "Apple"));
        var chunk2 = CreateBinaryChunk("chunk2.tmp", (2, "Apple"), (4, "Apple"));
        var outputPath = Path.Combine(_tempDir, "merged.txt");

        using (var merger = new KWayMerger([chunk1, chunk2]))
        {
            merger.MergeTo(outputPath);
        }

        var result = File.ReadAllLines(outputPath);
        Assert.That(result[0], Is.EqualTo("1. Apple"));
        Assert.That(result[1], Is.EqualTo("2. Apple"));
        Assert.That(result[2], Is.EqualTo("3. Apple"));
        Assert.That(result[3], Is.EqualTo("4. Apple"));
    }

    [Test]
    public void MergeTo_ThreeChunks_MergesCorrectly()
    {
        var chunk1 = CreateBinaryChunk("chunk1.tmp", (1, "Apple"));
        var chunk2 = CreateBinaryChunk("chunk2.tmp", (2, "Banana"));
        var chunk3 = CreateBinaryChunk("chunk3.tmp", (3, "Cherry"));
        var outputPath = Path.Combine(_tempDir, "merged.txt");

        using (var merger = new KWayMerger([chunk1, chunk2, chunk3]))
        {
            var count = merger.MergeTo(outputPath);
            Assert.That(count, Is.EqualTo(3));
        }

        var result = File.ReadAllLines(outputPath);
        Assert.That(result[0], Is.EqualTo("1. Apple"));
        Assert.That(result[1], Is.EqualTo("2. Banana"));
        Assert.That(result[2], Is.EqualTo("3. Cherry"));
    }

    [Test]
    public void MergeTo_CallsProgressCallback()
    {
        var items = Enumerable.Range(1, 100).Select(i => ((long)i, "Line")).ToArray();
        var chunk = CreateBinaryChunk("chunk.tmp", items);
        var outputPath = Path.Combine(_tempDir, "merged.txt");

        long lastReported = 0;
        using (var merger = new KWayMerger([chunk]))
        {
            merger.MergeTo(outputPath, count => lastReported = count);
        }

        Assert.That(lastReported, Is.EqualTo(0));
    }

    private string CreateBinaryChunk(string name, params (long Number, string Str)[] entries)
    {
        var path = Path.Combine(_tempDir, name);
        using var writer = new BinaryChunkWriter(path);
        foreach (var (number, str) in entries)
        {
            writer.Write(number, Encoding.ASCII.GetBytes(str));
        }
        return path;
    }
}
