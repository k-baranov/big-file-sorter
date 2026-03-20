using BigFileSorter.Core.IO;
using BigFileSorter.Sorter.IO;
using BigFileSorter.Sorter.Parsing;

namespace BigFileSorter.Tests.Sorter.IO;

[TestFixture]
public class BufferedLineReaderTests
{
    private string _tempDir = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"bigfilesorter_reader_test_{Guid.NewGuid():N}");
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
    public void TryReadLine_EmptyFile_ReturnsNoLines()
    {
        var path = Path.Combine(_tempDir, "empty.txt");
        File.WriteAllText(path, "");

        using var reader = new BufferedLineReader(path);
        Assert.That(reader.TryReadLine(out _), Is.False);
    }

    [Test]
    public void TryReadLine_SingleLineNoNewline_ReadsLine()
    {
        var path = Path.Combine(_tempDir, "single.txt");
        File.WriteAllText(path, "1. Hello");

        using var reader = new BufferedLineReader(path);
        Assert.That(reader.TryReadLine(out var line), Is.True);
        Assert.That(System.Text.Encoding.ASCII.GetString(line), Is.EqualTo("1. Hello"));
        Assert.That(reader.TryReadLine(out _), Is.False);
    }

    [Test]
    public void TryReadLine_HandlesWindowsLineEndings()
    {
        var path = Path.Combine(_tempDir, "crlf.txt");
        File.WriteAllBytes(path, "1. First\r\n2. Second\r\n"u8.ToArray());

        using var reader = new BufferedLineReader(path);

        Assert.That(reader.TryReadLine(out var line1), Is.True);
        Assert.That(System.Text.Encoding.ASCII.GetString(line1), Is.EqualTo("1. First"));

        Assert.That(reader.TryReadLine(out var line2), Is.True);
        Assert.That(System.Text.Encoding.ASCII.GetString(line2), Is.EqualTo("2. Second"));
    }

    [Test]
    public void TryReadLine_LineLongerThanBuffer_ReadsCorrectly()
    {
        var path = Path.Combine(_tempDir, "longline.txt");
        var longString = new string('A', 500);
        File.WriteAllText(path, $"1. {longString}");

        using var reader = new BufferedLineReader(path, bufferSize: 64);

        Assert.That(reader.TryReadLine(out var line), Is.True);
        LineParser.TryParse(line, out var number, out var stringBytes);
        Assert.That(stringBytes.Length, Is.EqualTo(500));
        Assert.That(number, Is.EqualTo(1));
    }
}
