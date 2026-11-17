using NilCM.Core.Lyrics;

namespace NilCM.Core.Tests;

public class LyricsServiceTests
{
    private sealed class FakeLyricsApi(string? raw) : ILyricsApi
    {
        public Task<string?> GetRawLyricAsync(long trackId, CancellationToken cancellationToken = default)
            => Task.FromResult(raw);
    }

    [Fact]
    public async Task GetLyricAsync_ParsesRawLrc()
    {
        var service = new LyricsService(new FakeLyricsApi("[00:01.000]a\n[00:02.000]b"));

        var lines = await service.GetLyricAsync(42);

        Assert.Equal(2, lines.Count);
    }

    [Fact]
    public async Task GetLyricAsync_NullRaw_ReturnsEmpty()
    {
        var service = new LyricsService(new FakeLyricsApi(null));

        Assert.Empty(await service.GetLyricAsync(42));
    }
}
