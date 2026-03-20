using System.Text;
using BigFileSorter.Sorter.Sorting;

namespace BigFileSorter.Tests.Sorter.Sorting;

[TestFixture]
public class ByteArraySpanComparerTests
{
    private readonly ByteArraySpanComparer _comparer = ByteArraySpanComparer.Instance;

    // IEqualityComparer<byte[]> tests

    [Test]
    public void Equals_SameContent_ReturnsTrue()
    {
        var a = "Hello"u8.ToArray();
        var b = "Hello"u8.ToArray();

        Assert.That(_comparer.Equals(a, b), Is.True);
    }

    [Test]
    public void Equals_DifferentContent_ReturnsFalse()
    {
        var a = "Hello"u8.ToArray();
        var b = "World"u8.ToArray();

        Assert.That(_comparer.Equals(a, b), Is.False);
    }

    [Test]
    public void Equals_SameReference_ReturnsTrue()
    {
        var a = "Hello"u8.ToArray();

        Assert.That(_comparer.Equals(a, a), Is.True);
    }

    [Test]
    public void Equals_BothNull_ReturnsTrue()
    {
        Assert.That(_comparer.Equals(null, null), Is.True);
    }

    [Test]
    public void Equals_OneNull_ReturnsFalse()
    {
        var a = "Hello"u8.ToArray();

        Assert.That(_comparer.Equals(a, null), Is.False);
        Assert.That(_comparer.Equals(null, a), Is.False);
    }

    [Test]
    public void Equals_DifferentLengths_ReturnsFalse()
    {
        var a = "Hi"u8.ToArray();
        var b = "Hello"u8.ToArray();

        Assert.That(_comparer.Equals(a, b), Is.False);
    }

    [Test]
    public void GetHashCode_SameContent_SameHash()
    {
        var a = "Hello"u8.ToArray();
        var b = "Hello"u8.ToArray();

        Assert.That(_comparer.GetHashCode(a), Is.EqualTo(_comparer.GetHashCode(b)));
    }

    [Test]
    public void GetHashCode_DifferentContent_DifferentHash()
    {
        var a = "Hello"u8.ToArray();
        var b = "World"u8.ToArray();

        Assert.That(_comparer.GetHashCode(a), Is.Not.EqualTo(_comparer.GetHashCode(b)));
    }

    // IAlternateEqualityComparer<ReadOnlySpan<byte>, byte[]> tests

    [Test]
    public void SpanEquals_MatchingContent_ReturnsTrue()
    {
        ReadOnlySpan<byte> span = "Hello"u8;
        var array = "Hello"u8.ToArray();

        Assert.That(_comparer.Equals(span, array), Is.True);
    }

    [Test]
    public void SpanEquals_DifferentContent_ReturnsFalse()
    {
        ReadOnlySpan<byte> span = "Hello"u8;
        var array = "World"u8.ToArray();

        Assert.That(_comparer.Equals(span, array), Is.False);
    }

    [Test]
    public void SpanGetHashCode_MatchesArrayGetHashCode()
    {
        ReadOnlySpan<byte> span = "Hello"u8;
        var array = "Hello"u8.ToArray();

        Assert.That(
            _comparer.GetHashCode(span),
            Is.EqualTo(_comparer.GetHashCode(array)));
    }

    [Test]
    public void Create_ReturnsArrayCopyOfSpan()
    {
        ReadOnlySpan<byte> span = "Hello"u8;

        var result = _comparer.Create(span);

        Assert.That(result, Is.EqualTo("Hello"u8.ToArray()));
    }

    [Test]
    public void Equals_EmptyArrays_ReturnsTrue()
    {
        Assert.That(_comparer.Equals([], []), Is.True);
    }

    [Test]
    public void SpanEquals_EmptySpanAndEmptyArray_ReturnsTrue()
    {
        Assert.That(_comparer.Equals(ReadOnlySpan<byte>.Empty, []), Is.True);
    }
}
