using System.Buffers.Binary;
using System.Text;
using BigFileSorter.Sorter.IO;

namespace BigFileSorter.Tests.Sorter.IO;

[TestFixture]
public class BinaryChunkReaderTests
{
    private string _tempDir = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"bigfilesorter_br_test_{Guid.NewGuid():N}");
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
    public void TryReadEntry_EmptyFile_ReturnsFalse()
    {
        var path = Path.Combine(_tempDir, "empty.bin");
        File.WriteAllBytes(path, []);

        using var reader = new BinaryChunkReader(path);
        Assert.That(reader.TryReadEntry(out _, out _), Is.False);
    }

    [Test]
    public void TryReadEntry_TruncatedHeader_ReturnsFalse()
    {
        var path = Path.Combine(_tempDir, "truncated.bin");
        File.WriteAllBytes(path, new byte[5]); // less than 12-byte header

        using var reader = new BinaryChunkReader(path);
        Assert.That(reader.TryReadEntry(out _, out _), Is.False);
    }

    [Test]
    public void TryReadEntry_TruncatedStringData_ReturnsFalse()
    {
        var path = Path.Combine(_tempDir, "truncated_str.bin");

        // Write header claiming 100-byte string, but provide no string data
        var header = new byte[sizeof(long) + sizeof(int)];
        BinaryPrimitives.WriteInt64LittleEndian(header, 1);
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(sizeof(long)), 100);
        File.WriteAllBytes(path, header);

        using var reader = new BinaryChunkReader(path);
        Assert.That(reader.TryReadEntry(out _, out _), Is.False);
    }

    [Test]
    public void TryReadEntry_MultipleEntries_ReadsAll()
    {
        var path = Path.Combine(_tempDir, "multi.bin");
        WriteBinaryEntries(path, (10, "First"), (20, "Second"), (30, "Third"));

        using var reader = new BinaryChunkReader(path);
        var results = ReadAll(reader);

        Assert.That(results, Has.Count.EqualTo(3));
        Assert.That(results[0], Is.EqualTo((10L, "First")));
        Assert.That(results[1], Is.EqualTo((20L, "Second")));
        Assert.That(results[2], Is.EqualTo((30L, "Third")));
    }

    [Test]
    public void TryReadEntry_SmallBuffer_ReadsCorrectly()
    {
        var path = Path.Combine(_tempDir, "small_buf.bin");
        WriteBinaryEntries(path, (1, "Alpha"), (2, "Beta"));

        using var reader = new BinaryChunkReader(path, bufferSize: 16);
        var results = ReadAll(reader);

        Assert.That(results, Has.Count.EqualTo(2));
        Assert.That(results[0], Is.EqualTo((1L, "Alpha")));
        Assert.That(results[1], Is.EqualTo((2L, "Beta")));
    }

    [Test]
    public void TryReadEntry_EntryLargerThanBuffer_GrowsAndReads()
    {
        var path = Path.Combine(_tempDir, "grow.bin");
        var longStr = new string('Z', 500);
        WriteBinaryEntries(path, (99, longStr));

        using var reader = new BinaryChunkReader(path, bufferSize: 32);
        Assert.That(reader.TryReadEntry(out var number, out var stringBytes), Is.True);
        Assert.That(number, Is.EqualTo(99));
        Assert.That(stringBytes, Has.Length.EqualTo(500));
    }

    private static void WriteBinaryEntries(string path, params (long Num, string Str)[] entries)
    {
        using var writer = new BinaryChunkWriter(path);
        foreach (var (num, str) in entries)
        {
            writer.Write(num, Encoding.ASCII.GetBytes(str));
        }
    }

    private static List<(long Number, string StringPart)> ReadAll(BinaryChunkReader reader)
    {
        var results = new List<(long, string)>();
        while (reader.TryReadEntry(out var number, out var stringBytes))
        {
            results.Add((number, Encoding.ASCII.GetString(stringBytes)));
        }
        return results;
    }
}
