using System.Buffers;
using System.Buffers.Binary;

namespace BigFileSorter.Sorter.IO;

/// <summary>
/// Reads entries from binary format: [8 bytes number][4 bytes string length][N bytes ASCII string].
/// </summary>
public sealed class BinaryChunkReader(string path, int bufferSize = 256 * 1024) : IDisposable
{
    private readonly FileStream _stream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 1, FileOptions.SequentialScan);
    private byte[] _buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
    private int _bufferLength = 0;
    private int _bufferOffset = 0;
    private bool _eof = false;

    /// <summary>
    /// Reads the next entry. Returns the number and a copy of the string bytes.
    /// </summary>
    public bool TryReadEntry(out long number, out byte[] stringBytes)
    {
        number = default;
        stringBytes = default!;

        const int headerSize = sizeof(long) + sizeof(int); // 12 bytes

        if (!EnsureAvailable(headerSize))
        {
            return false;
        }

        number = BinaryPrimitives.ReadInt64LittleEndian(_buffer.AsSpan(_bufferOffset));
        int stringLength = BinaryPrimitives.ReadInt32LittleEndian(_buffer.AsSpan(_bufferOffset + sizeof(long)));

        int totalBytes = headerSize + stringLength;
        if (!EnsureAvailable(totalBytes))
        {
            return false;
        }

        _bufferOffset += headerSize;
        stringBytes = _buffer.AsSpan(_bufferOffset, stringLength).ToArray();
        _bufferOffset += stringLength;

        return true;
    }

    private bool EnsureAvailable(int needed)
    {
        int available = _bufferLength - _bufferOffset;
        if (available >= needed)
        {
            return true;
        }

        if (_eof)
        {
            return false;
        }

        if (available > 0)
        {
            _buffer.AsSpan(_bufferOffset, available).CopyTo(_buffer);
        }

        _bufferOffset = 0;
        _bufferLength = available;

        if (needed > _buffer.Length)
        {
            var newBuffer = ArrayPool<byte>.Shared.Rent(needed * 2);
            _buffer.AsSpan(0, _bufferLength).CopyTo(newBuffer);
            ArrayPool<byte>.Shared.Return(_buffer);
            _buffer = newBuffer;
        }

        while (_bufferLength < needed && !_eof)
        {
            int bytesRead = _stream.Read(_buffer, _bufferLength, _buffer.Length - _bufferLength);
            if (bytesRead == 0)
            {
                _eof = true;
                break;
            }
            _bufferLength += bytesRead;
        }

        return _bufferLength >= needed;
    }

    public void Dispose()
    {
        _stream.Dispose();
        ArrayPool<byte>.Shared.Return(_buffer);
    }
}
