using System.Buffers;

namespace BigFileSorter.Sorter.IO;

/// <summary>
/// High-performance line reader that reads raw bytes from a file stream
/// and yields lines without allocating strings for each line.
/// </summary>
public sealed class BufferedLineReader(string path, int bufferSize = 64 * 1024) : IDisposable
{
    private readonly FileStream _stream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 1, FileOptions.SequentialScan);
    private byte[] _buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
    private int _bufferLength = 0;
    private int _bufferOffset = 0;
    private bool _eof = false;

    /// <summary>
    /// Reads the next line as a span of bytes. Returns false when no more lines.
    /// The returned span is valid only until the next call to TryReadLine.
    /// </summary>
    public bool TryReadLine(out ReadOnlySpan<byte> line)
    {
        while (true)
        {
            // Search for newline in current buffer
            var available = _buffer.AsSpan(_bufferOffset, _bufferLength - _bufferOffset);
            int newlineIndex = available.IndexOf((byte)'\n');

            if (newlineIndex >= 0)
            {
                line = StripCR(available[..newlineIndex]);
                _bufferOffset += newlineIndex + 1;
                return true;
            }

            if (_eof)
            {
                if (_bufferOffset < _bufferLength)
                {
                    line = StripCR(available);
                    _bufferOffset = _bufferLength;
                    return line.Length > 0;
                }
                line = default;
                return false;
            }

            CompactBuffer();

            int bytesRead = _stream.Read(_buffer, _bufferLength, _buffer.Length - _bufferLength);
            if (bytesRead == 0)
            {
                _eof = true;
                continue;
            }
            _bufferLength += bytesRead;
        }
    }

    /// <summary>
    /// Shifts unprocessed bytes to the start of the buffer, growing it if needed.
    /// </summary>
    private void CompactBuffer()
    {
        int remaining = _bufferLength - _bufferOffset;
        if (remaining > 0)
        {
            if (remaining >= _buffer.Length / 2)
            {
                var newBuffer = ArrayPool<byte>.Shared.Rent(_buffer.Length * 2);
                _buffer.AsSpan(_bufferOffset, remaining).CopyTo(newBuffer);
                ArrayPool<byte>.Shared.Return(_buffer);
                _buffer = newBuffer;
            }
            else
            {
                _buffer.AsSpan(_bufferOffset, remaining).CopyTo(_buffer);
            }
        }

        _bufferOffset = 0;
        _bufferLength = remaining;
    }

    private static ReadOnlySpan<byte> StripCR(ReadOnlySpan<byte> line)
    {
        if (line.Length > 0 && line[^1] == (byte)'\r')
        {
            return line[..^1];
        }

        return line;
    }

    public void Dispose()
    {
        _stream.Dispose();
        ArrayPool<byte>.Shared.Return(_buffer);
    }
}
