namespace BigFileSorter.Sorter.Sorting;

/// <summary>
/// Equality comparer for byte[] with content-based comparison.
/// </summary>
internal sealed class ByteArraySpanComparer
    : IEqualityComparer<byte[]>,
      IAlternateEqualityComparer<ReadOnlySpan<byte>, byte[]>
{
    public static readonly ByteArraySpanComparer Instance = new();

    // IEqualityComparer<byte[]>
    public bool Equals(byte[]? x, byte[]? y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        if (x is null || y is null)
        {
            return false;
        }
        return x.AsSpan().SequenceEqual(y);
    }

    public int GetHashCode(byte[] obj)
    {
        var hash = new HashCode();
        hash.AddBytes(obj);
        return hash.ToHashCode();
    }

    // IAlternateEqualityComparer<ReadOnlySpan<byte>, byte[]>
    public bool Equals(ReadOnlySpan<byte> alternate, byte[] other) => alternate.SequenceEqual(other);

    public int GetHashCode(ReadOnlySpan<byte> alternate)
    {
        var hash = new HashCode();
        hash.AddBytes(alternate);
        return hash.ToHashCode();
    }

    /// <summary>
    /// Called by Dictionary only when a new key needs to be created (new unique string).
    /// </summary>
    public byte[] Create(ReadOnlySpan<byte> alternate) => alternate.ToArray();
}
