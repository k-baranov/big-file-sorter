using System.Buffers;
using System.Buffers.Binary;

namespace BigFileSorter.Sorter.IO;

/// <summary>
/// Writes entries in binary format: [8 bytes number][4 bytes string length][N bytes ASCII string].
/// </summary>
public sealed class BinaryChunkWriter(string path, int bufferSize = 1024 * 1024) : IDisposable
{
    private readonly FileStream _stream = new(path, FileMode.Create, FileAccess.Write, FileShare.None,
            bufferSize: 1, FileOptions.None);
    private byte[] _buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
    private int _position = 0;

    public void Write(long number, ReadOnlySpan<byte> stringBytes)
    {
        const int headerSize = sizeof(long) + sizeof(int); // 12 bytes: number + string length
        int totalBytes = headerSize + stringBytes.Length;

        if (_position + totalBytes > _buffer.Length)
        {
            Flush();
        }

        if (totalBytes > _buffer.Length)
        {
            var newBuffer = ArrayPool<byte>.Shared.Rent(totalBytes * 2);
            _buffer.AsSpan(0, _position).CopyTo(newBuffer);
            ArrayPool<byte>.Shared.Return(_buffer);
            _buffer = newBuffer;
        }

        BinaryPrimitives.WriteInt64LittleEndian(_buffer.AsSpan(_position), number);
        _position += sizeof(long);

        BinaryPrimitives.WriteInt32LittleEndian(_buffer.AsSpan(_position), stringBytes.Length);
        _position += sizeof(int);

        stringBytes.CopyTo(_buffer.AsSpan(_position));
        _position += stringBytes.Length;
    }

    public void Flush()
    {
        if (_position > 0)
        {
            _stream.Write(_buffer, 0, _position);
            _position = 0;
        }
    }

    public void Dispose()
    {
        Flush();
        _stream.Dispose();
        ArrayPool<byte>.Shared.Return(_buffer);
    }
}
