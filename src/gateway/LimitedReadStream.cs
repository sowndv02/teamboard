using Microsoft.AspNetCore.Http;

namespace Gateway;

/// <summary>
/// Read-only wrapper that fails once more than <c>maxBytes</c> of body have been read. It counts payload bytes only:
/// Kestrel's own limit also counts chunk framing, so it would reject a chunked body of exactly the limit (plan TB-025, test S04).
/// </summary>
internal sealed class LimitedReadStream(Stream inner, long maxBytes) : Stream
{
    private long _total;

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var read = await inner.ReadAsync(buffer, cancellationToken);
        _total += read;
        if (_total > maxBytes)
        {
            // Same exception type Kestrel uses, so the error handling for "body too large" is one path (UpstreamErrorClassifier).
            throw new BadHttpRequestException("Request body too large.", StatusCodes.Status413PayloadTooLarge);
        }

        return read;
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    public override int Read(byte[] buffer, int offset, int count) =>
        throw new NotSupportedException("Synchronous reads of the request body are not allowed.");

    public override void Flush()
    {
    }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
