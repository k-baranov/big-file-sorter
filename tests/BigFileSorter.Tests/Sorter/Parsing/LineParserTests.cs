using System.Text;
using BigFileSorter.Sorter.Parsing;

namespace BigFileSorter.Tests.Sorter.Parsing;

[TestFixture]
public class LineParserTests
{
    [Test]
    public void TryParse_ValidLine_ReturnsTrue()
    {
        var bytes = "42. Hello World"u8;

        bool result = LineParser.TryParse(bytes, out var number, out var stringBytes);

        Assert.That(result, Is.True);
        Assert.That(number, Is.EqualTo(42));
        Assert.That(Encoding.ASCII.GetString(stringBytes), Is.EqualTo("Hello World"));
    }

    [Test]
    public void TryParse_LargeNumber_ReturnsTrue()
    {
        var bytes = "9223372036854775807. Test String"u8;

        bool result = LineParser.TryParse(bytes, out var number, out var stringBytes);

        Assert.That(result, Is.True);
        Assert.That(number, Is.EqualTo(9223372036854775807));
        Assert.That(Encoding.ASCII.GetString(stringBytes), Is.EqualTo("Test String"));
    }

    [Test]
    public void TryParse_EmptyLine_ReturnsFalse()
    {
        bool result = LineParser.TryParse(ReadOnlySpan<byte>.Empty, out _, out _);
        Assert.That(result, Is.False);
    }

    [Test]
    public void TryParse_NoDotSeparator_ReturnsFalse()
    {
        var bytes = "42 Hello"u8;
        bool result = LineParser.TryParse(bytes, out _, out _);
        Assert.That(result, Is.False);
    }

    [Test]
    public void TryParse_NoSpaceAfterDot_ReturnsFalse()
    {
        var bytes = "42.Hello"u8;
        bool result = LineParser.TryParse(bytes, out _, out _);
        Assert.That(result, Is.False);
    }

    [Test]
    public void TryParse_NegativeNumber_ParsesCorrectly()
    {
        var bytes = "-5. Hello"u8;
        bool result = LineParser.TryParse(bytes, out var number, out var stringBytes);

        Assert.That(result, Is.True);
        Assert.That(number, Is.EqualTo(-5));
        Assert.That(Encoding.ASCII.GetString(stringBytes), Is.EqualTo("Hello"));
    }

    [Test]
    public void TryParse_LeadingZeros_ParsesNumber()
    {
        var bytes = "007. Bond"u8;
        bool result = LineParser.TryParse(bytes, out var number, out var stringBytes);

        Assert.That(result, Is.True);
        Assert.That(number, Is.EqualTo(7));
        Assert.That(Encoding.ASCII.GetString(stringBytes), Is.EqualTo("Bond"));
    }

    [Test]
    public void TryParse_NumberOverflow_ReturnsFalse()
    {
        var bytes = "99999999999999999999. Overflow"u8;
        bool result = LineParser.TryParse(bytes, out _, out _);
        Assert.That(result, Is.False);
    }

    [Test]
    public void TryParse_OnlyDotSpace_ReturnsFalse()
    {
        var bytes = ". Hello"u8;
        bool result = LineParser.TryParse(bytes, out _, out _);
        Assert.That(result, Is.False);
    }

    [Test]
    public void TryParse_LettersInsteadOfNumber_ReturnsFalse()
    {
        var bytes = "abc. Hello"u8;
        bool result = LineParser.TryParse(bytes, out _, out _);
        Assert.That(result, Is.False);
    }    
}
