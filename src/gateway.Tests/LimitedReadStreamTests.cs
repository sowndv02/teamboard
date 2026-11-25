using Microsoft.AspNetCore.Http;
using Xunit;

namespace Gateway.Tests;

/// <summary>Boundary rules of the chunked-body counter, checked without a server (the S04 and S05 scenarios cover it end to end).</summary>
public sealed class LimitedReadStreamTests
{
    [Fact]
    public async Task ReadAsync_BodyOfExactlyTheLimit_IsReadInFull()
    {
        using var stream = new LimitedReadStream(new MemoryStream(new byte[1024]), maxBytes: 1024);

        var total = await DrainAsync(stream, pieceSize: 100);

        Assert.Equal(1024, total);
    }

    [Fact]
    public async Task ReadAsync_BodyOneByteOverTheLimit_Fails413()
    {
        using var stream = new LimitedReadStream(new MemoryStream(new byte[1025]), maxBytes: 1024);

        var exception = await Assert.ThrowsAsync<BadHttpRequestException>(() => DrainAsync(stream, pieceSize: 100));

        Assert.Equal(StatusCodes.Status413PayloadTooLarge, exception.StatusCode);
    }

    [Fact]
    public async Task ReadAsync_SingleReadCrossingTheLimit_Fails413()
    {
        using var stream = new LimitedReadStream(new MemoryStream(new byte[2048]), maxBytes: 1024);

        var exception = await Assert.ThrowsAsync<BadHttpRequestException>(() => DrainAsync(stream, pieceSize: 2048));

        Assert.Equal(StatusCodes.Status413PayloadTooLarge, exception.StatusCode);
    }

    [Fact]
    public async Task ReadAsync_EmptyBody_ReturnsZero()
    {
        using var stream = new LimitedReadStream(new MemoryStream(), maxBytes: 1);

        Assert.Equal(0, await stream.ReadAsync(new byte[16]));
    }

    [Fact]
    public void Read_Synchronously_IsNotSupported()
    {
        using var stream = new LimitedReadStream(new MemoryStream(new byte[8]), maxBytes: 1024);

        Assert.Throws<NotSupportedException>(() => stream.Read(new byte[8], 0, 8));
    }

    private static async Task<long> DrainAsync(Stream stream, int pieceSize)
    {
        long total = 0;
        var buffer = new byte[pieceSize];
        int read;
        while ((read = await stream.ReadAsync(buffer)) > 0)
        {
            total += read;
        }

        return total;
    }
}
