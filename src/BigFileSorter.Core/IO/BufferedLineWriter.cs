using System.Buffers;
using System.Text;

namespace BigFileSorter.Core.IO;

/// <summary>
/// High-performance writer that formats and writes lines to a file
/// using a large write buffer to minimize syscalls.
/// </summary>
public sealed class BufferedLineWriter(string path, int bufferSize = 256 * 1024 * 1024) : IDisposable
{
    private const int MaxNumberDigits = 20;

    private readonly FileStream _stream = new(path, FileMode.Create, FileAccess.Write, FileShare.None,
            bufferSize: 1, FileOptions.None);
    private byte[] _buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
    private int _position = 0;
    private static readonly byte[] NewLineBytes = Encoding.ASCII.GetBytes(FormatConstants.NewLine);
    private static readonly byte[] DotSpaceBytes = Encoding.ASCII.GetBytes(FormatConstants.DotSpace);

    public void WriteLine(long number, ReadOnlySpan<byte> stringBytes)
    {
        // number (20) + ". " (2) + string bytes + newline (2)
        int maxBytes = MaxNumberDigits + DotSpaceBytes.Length + stringBytes.Length + NewLineBytes.Length;

        if (_position + maxBytes > _buffer.Length)
        {
            Flush();
        }

        if (maxBytes > _buffer.Length)
        {
            var newBuffer = ArrayPool<byte>.Shared.Rent(maxBytes * 2);
            _buffer.AsSpan(0, _position).CopyTo(newBuffer);
            ArrayPool<byte>.Shared.Return(_buffer);
            _buffer = newBuffer;
        }

        Span<byte> dest = _buffer.AsSpan(_position);
        number.TryFormat(dest, out int written);
        _position += written;

        DotSpaceBytes.CopyTo(_buffer, _position);
        _position += DotSpaceBytes.Length;

        stringBytes.CopyTo(_buffer.AsSpan(_position));
        _position += stringBytes.Length;

        NewLineBytes.CopyTo(_buffer, _position);
        _position += NewLineBytes.Length;
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
