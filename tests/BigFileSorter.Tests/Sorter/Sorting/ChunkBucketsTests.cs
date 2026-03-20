using System.Text;
using BigFileSorter.Sorter.Sorting;

namespace BigFileSorter.Tests.Sorter.Sorting;

[TestFixture]
public class ChunkBucketsTests
{
    [Test]
    public void Add_SingleEntry_CountIsOne()
    {
        var buckets = new ChunkBuckets();
        buckets.Add("Apple"u8, 1);

        Assert.That(buckets.Count, Is.EqualTo(1));
    }

    [Test]
    public void Add_SameStringSameNumber_GroupsUnderOneKey()
    {
        var buckets = new ChunkBuckets();
        buckets.Add("Apple"u8, 1);
        buckets.Add("Apple"u8, 2);

        Assert.That(buckets.Count, Is.EqualTo(1));
    }

    [Test]
    public void Add_DifferentStrings_SeparateKeys()
    {
        var buckets = new ChunkBuckets();
        buckets.Add("Apple"u8, 1);
        buckets.Add("Banana"u8, 2);

        Assert.That(buckets.Count, Is.EqualTo(2));
    }

    [Test]
    public void GetSortedEntries_SortsByStringAscending()
    {
        var buckets = new ChunkBuckets();
        buckets.Add("Cherry"u8, 1);
        buckets.Add("Apple"u8, 2);
        buckets.Add("Banana"u8, 3);

        var entries = buckets.GetSortedEntries().ToList();

        Assert.That(entries, Has.Count.EqualTo(3));
        Assert.That(Encoding.ASCII.GetString(entries[0].StringBytes), Is.EqualTo("Apple"));
        Assert.That(Encoding.ASCII.GetString(entries[1].StringBytes), Is.EqualTo("Banana"));
        Assert.That(Encoding.ASCII.GetString(entries[2].StringBytes), Is.EqualTo("Cherry"));
    }

    [Test]
    public void GetSortedEntries_NumbersWithinKeySortedAscending()
    {
        var buckets = new ChunkBuckets();
        buckets.Add("Same"u8, 5);
        buckets.Add("Same"u8, 1);
        buckets.Add("Same"u8, 3);

        var entries = buckets.GetSortedEntries().ToList();

        Assert.That(entries, Has.Count.EqualTo(1));
        Assert.That(entries[0].Numbers, Is.EqualTo(new List<long> { 1, 3, 5 }));
    }

    [Test]
    public void GetSortedEntries_Empty_ReturnsNothing()
    {
        var buckets = new ChunkBuckets();

        var entries = buckets.GetSortedEntries().ToList();

        Assert.That(entries, Is.Empty);
    }

    [Test]
    public void Add_SpanLookup_MatchesByteContent()
    {
        var buckets = new ChunkBuckets();

        // Add via two separate spans with same content
        byte[] bytes1 = Encoding.ASCII.GetBytes("Test");
        byte[] bytes2 = Encoding.ASCII.GetBytes("Test");

        buckets.Add(bytes1.AsSpan(), 1);
        buckets.Add(bytes2.AsSpan(), 2);

        Assert.That(buckets.Count, Is.EqualTo(1));

        var entries = buckets.GetSortedEntries().ToList();
        Assert.That(entries[0].Numbers, Is.EqualTo(new List<long> { 1, 2 }));
    }

    [Test]
    public void GetSortedEntries_CaseSensitiveOrdering()
    {
        var buckets = new ChunkBuckets();
        buckets.Add("banana"u8, 1);
        buckets.Add("Apple"u8, 2);
        buckets.Add("Banana"u8, 3);

        var entries = buckets.GetSortedEntries().ToList();

        // ASCII: uppercase < lowercase
        Assert.That(Encoding.ASCII.GetString(entries[0].StringBytes), Is.EqualTo("Apple"));
        Assert.That(Encoding.ASCII.GetString(entries[1].StringBytes), Is.EqualTo("Banana"));
        Assert.That(Encoding.ASCII.GetString(entries[2].StringBytes), Is.EqualTo("banana"));
    }
}
