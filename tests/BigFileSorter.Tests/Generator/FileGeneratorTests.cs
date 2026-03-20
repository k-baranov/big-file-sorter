using BigFileSorter.Generator;
using BigFileSorter.Sorter.Parsing;
using System.Text;

namespace BigFileSorter.Tests.Generator;

[TestFixture]
public class FileGeneratorTests
{
    private string _tempDir = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"bigfilesorter_gen_test_{Guid.NewGuid():N}");
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
    public void Generate_CreatesFileOfApproximateTargetSize()
    {
        var path = Path.Combine(_tempDir, "output.txt");
        long targetSize = 10_000;

        var generator = new FileGenerator(path, targetSize);
        generator.Generate();

        var actualSize = new FileInfo(path).Length;
        Assert.That(actualSize, Is.GreaterThanOrEqualTo(targetSize));
    }

    [Test]
    public void Generate_AllLinesMatchExpectedFormat()
    {
        var path = Path.Combine(_tempDir, "output.txt");

        var generator = new FileGenerator(path, targetSize: 5_000);
        generator.Generate();

        var lines = File.ReadAllLines(path);
        Assert.That(lines, Is.Not.Empty);

        foreach (var line in lines)
        {
            bool parsed = LineParser.TryParse(Encoding.ASCII.GetBytes(line), out var number, out var stringPart);
            Assert.That(parsed, Is.True, $"Failed to parse line: '{line}'");
            Assert.That(number, Is.GreaterThanOrEqualTo(1));
            Assert.That(Encoding.ASCII.GetString(stringPart), Is.Not.Empty);
        }
    }

    [Test]
    public void Generate_StringPartsContainOnlyExpectedCharacters()
    {
        var path = Path.Combine(_tempDir, "output.txt");
        var allowedChars = GeneratorConstants.AllowedChars;

        new FileGenerator(path, targetSize: 5_000).Generate();

        var lines = File.ReadAllLines(path);
        foreach (var line in lines)
        {
            LineParser.TryParse(Encoding.ASCII.GetBytes(line), out _, out var stringPart);
            foreach (char c in stringPart)
            {
                Assert.That(allowedChars, Does.Contain(c), $"Unexpected char '{c}' in '{Encoding.ASCII.GetString(stringPart)}'");
            }
        }
    }
}
