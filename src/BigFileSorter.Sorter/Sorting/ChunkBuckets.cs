namespace BigFileSorter.Sorter.Sorting;

/// <summary>
/// Groups parsed lines by their string part.
/// Each unique string maps to a list of numbers that share it.
/// </summary>
public sealed class ChunkBuckets
{
    private readonly Dictionary<byte[], List<long>> _data = new(ByteArraySpanComparer.Instance);
    private readonly Dictionary<byte[], List<long>>.AlternateLookup<ReadOnlySpan<byte>> _lookup;

    public ChunkBuckets()
    {
        _lookup = _data.GetAlternateLookup<ReadOnlySpan<byte>>();
    }

    public int Count => _data.Count;

    /// <summary>
    /// Adds a number for the given string key. Creates the key entry if new.
    /// </summary>
    public void Add(ReadOnlySpan<byte> stringBytes, long number)
    {
        if (!_lookup.TryGetValue(stringBytes, out var numbers))
        {
            numbers = [];
            _lookup[stringBytes] = numbers;
        }

        numbers.Add(number);
    }

    /// <summary>
    /// Returns all entries sorted by string (ordinal byte comparison),
    /// with numbers within each string sorted ascending.
    /// </summary>
    public IEnumerable<(byte[] StringBytes, List<long> Numbers)> GetSortedEntries()
    {
        var keys = _data.Keys.ToArray();
        Array.Sort(keys, (x, y) => x.AsSpan().SequenceCompareTo(y));

        foreach (var key in keys)
        {
            var numbers = _data[key];
            numbers.Sort();
            yield return (key, numbers);
        }
    }
}
