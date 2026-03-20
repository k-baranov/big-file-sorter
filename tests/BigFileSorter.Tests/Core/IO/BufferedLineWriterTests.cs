using BigFileSorter.Core.IO;

namespace BigFileSorter.Tests.Core.IO;

[TestFixture]
public class BufferedLineWriterTests
{
    private string _tempDir = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"bigfilesorter_writer_test_{Guid.NewGuid():N}");
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
    public void WriteLine_WritesCorrectFormat()
    {
        var path = Path.Combine(_tempDir, "format.txt");

        using (var writer = new BufferedLineWriter(path))
        {
            writer.WriteLine(42, "Hello World"u8);
            writer.WriteLine(1, "Test"u8);
        }

        var lines = File.ReadAllLines(path);
        Assert.That(lines, Has.Length.EqualTo(2));
        Assert.That(lines[0], Is.EqualTo("42. Hello World"));
        Assert.That(lines[1], Is.EqualTo("1. Test"));
    }

    [Test]
    public void WriteLine_LargeNumber_WritesCorrectly()
    {
        var path = Path.Combine(_tempDir, "large_num.txt");

        using (var writer = new BufferedLineWriter(path))
        {
            writer.WriteLine(long.MaxValue, "Max"u8);
        }

        var lines = File.ReadAllLines(path);
        Assert.That(lines[0], Is.EqualTo($"{long.MaxValue}. Max"));
    }

    [Test]
    public void WriteLine_ManyLines_FlushesCorrectly()
    {
        var path = Path.Combine(_tempDir, "many.txt");
        const int count = 10_000;

        using (var writer = new BufferedLineWriter(path, bufferSize: 1024))
        {
            for (int i = 1; i <= count; i++)
            {
                writer.WriteLine(i, "Line"u8);
            }
        }

        var lines = File.ReadAllLines(path);
        Assert.That(lines, Has.Length.EqualTo(count));
        Assert.That(lines[0], Is.EqualTo("1. Line"));
        Assert.That(lines[^1], Is.EqualTo($"{count}. Line"));
    }

    [Test]
    public void WriteLine_EmptyString_WritesCorrectly()
    {
        var path = Path.Combine(_tempDir, "empty_str.txt");

        using (var writer = new BufferedLineWriter(path))
        {
            writer.WriteLine(1, ReadOnlySpan<byte>.Empty);
        }

        var lines = File.ReadAllLines(path);
        Assert.That(lines[0], Is.EqualTo("1. "));
    }

    [Test]
    public void WriteLine_LongString_WritesCorrectly()
    {
        var path = Path.Combine(_tempDir, "long_str.txt");
        var longBytes = new byte[5000];
        Array.Fill(longBytes, (byte)'X');

        using (var writer = new BufferedLineWriter(path, bufferSize: 256))
        {
            writer.WriteLine(1, longBytes);
        }

        var lines = File.ReadAllLines(path);
        Assert.That(lines[0], Is.EqualTo($"1. {new string('X', 5000)}"));
    }
}
