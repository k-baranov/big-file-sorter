using System.Text;

namespace BigFileSorter.Sorter.Parsing;

public static class LineParser
{
    /// <summary>
    /// Parses a line in format "Number. String" from raw ASCII bytes.
    /// Returns number and the byte span of the string part.
    /// </summary>
    public static bool TryParse(ReadOnlySpan<byte> line, out long number, out ReadOnlySpan<byte> stringBytes)
    {
        number = default;
        stringBytes = default;

        if (line.IsEmpty)
        {
            return false;
        }

        int dotIndex = line.IndexOf((byte)'.');
        if (dotIndex < 1 || dotIndex + 2 > line.Length || line[dotIndex + 1] != (byte)' ')
        {
            return false;
        }

        if (!long.TryParse(Encoding.ASCII.GetString(line[..dotIndex]), out number))
        {
            return false;
        }

        stringBytes = line[(dotIndex + 2)..];
        return true;
    }
}
