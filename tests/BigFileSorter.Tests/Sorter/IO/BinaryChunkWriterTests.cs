using System.Text;
using BigFileSorter.Sorter.IO;

namespace BigFileSorter.Tests.Sorter.IO;

[TestFixture]
public class BinaryChunkWriterTests
{
    private string _tempDir = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"bigfilesorter_bw_test_{Guid.NewGuid():N}");
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
    public void Write_SingleEntry_CanBeReadBack()
    {
        var path = Path.Combine(_tempDir, "single.bin");

        using (var writer = new BinaryChunkWriter(path))
        {
            writer.Write(42, "Hello"u8);
        }

        using var reader = new BinaryChunkReader(path);
        Assert.That(reader.TryReadEntry(out var number, out var stringBytes), Is.True);
        Assert.That(number, Is.EqualTo(42));
        Assert.That(Encoding.ASCII.GetString(stringBytes), Is.EqualTo("Hello"));
        Assert.That(reader.TryReadEntry(out _, out _), Is.False);
    }

    [Test]
    public void Write_MultipleEntries_AllReadBackInOrder()
    {
        var path = Path.Combine(_tempDir, "multi.bin");
        var entries = new (long Num, string Str)[]
        {
            (1, "Apple"), (99999, "Banana"), (42, "Cherry pie")
        };

        using (var writer = new BinaryChunkWriter(path))
        {
            foreach (var (num, str) in entries)
            {
                writer.Write(num, Encoding.ASCII.GetBytes(str));
            }
        }

        using var reader = new BinaryChunkReader(path);
        foreach (var (expectedNum, expectedStr) in entries)
        {
            Assert.That(reader.TryReadEntry(out var number, out var stringBytes), Is.True);
            Assert.That(number, Is.EqualTo(expectedNum));
            Assert.That(Encoding.ASCII.GetString(stringBytes), Is.EqualTo(expectedStr));
        }
        Assert.That(reader.TryReadEntry(out _, out _), Is.False);
    }

    [Test]
    public void Write_EmptyString_WritesCorrectly()
    {
        var path = Path.Combine(_tempDir, "empty_str.bin");

        using (var writer = new BinaryChunkWriter(path))
        {
            writer.Write(7, ReadOnlySpan<byte>.Empty);
        }

        using var reader = new BinaryChunkReader(path);
        Assert.That(reader.TryReadEntry(out var number, out var stringBytes), Is.True);
        Assert.That(number, Is.EqualTo(7));
        Assert.That(stringBytes, Is.Empty);
    }

    [Test]
    public void Write_LargeEntry_ExceedingBufferSize_WritesCorrectly()
    {
        var path = Path.Combine(_tempDir, "large.bin");
        var largeString = new string('X', 2000);
        var largeBytes = Encoding.ASCII.GetBytes(largeString);

        using (var writer = new BinaryChunkWriter(path, bufferSize: 64))
        {
            writer.Write(123, largeBytes);
        }

        using var reader = new BinaryChunkReader(path);
        Assert.That(reader.TryReadEntry(out var number, out var stringBytes), Is.True);
        Assert.That(number, Is.EqualTo(123));
        Assert.That(stringBytes, Has.Length.EqualTo(2000));
    }

    [Test]
    public void Write_NegativeNumber_PreservesValue()
    {
        var path = Path.Combine(_tempDir, "negative.bin");

        using (var writer = new BinaryChunkWriter(path))
        {
            writer.Write(-42, "Test"u8);
        }

        using var reader = new BinaryChunkReader(path);
        Assert.That(reader.TryReadEntry(out var number, out _), Is.True);
        Assert.That(number, Is.EqualTo(-42));
    }

    [Test]
    public void Write_NoEntries_CreatesEmptyFile()
    {
        var path = Path.Combine(_tempDir, "empty.bin");

        using (var writer = new BinaryChunkWriter(path))
        {
            // write nothing
        }

        Assert.That(new FileInfo(path).Length, Is.EqualTo(0));
    }
}
