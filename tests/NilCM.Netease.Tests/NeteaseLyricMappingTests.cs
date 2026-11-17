using NilCM.Netease.Lyrics;

namespace NilCM.Netease.Tests;

/// <summary>契约测试：录制的 JSON fixture → 原始 LRC（不真连网，可重复、可离线）。</summary>
public class NeteaseLyricMappingTests
{
    private const string FixtureWithLyric =
        """
        {"sgc":false,"sfy":false,"qfy":false,"lrc":{"version":6,"lyric":"[00:01.000]hello"},"tlyric":{"version":1,"lyric":""},"code":200}
        """;

    [Fact]
    public void ParseResponse_WithLrc_ReturnsRawLyric()
    {
        Assert.Equal("[00:01.000]hello", NeteaseLyricsApi.ParseResponse(FixtureWithLyric));
    }

    [Fact]
    public void ParseResponse_NoLyric_ReturnsNull()
    {
        Assert.Null(NeteaseLyricsApi.ParseResponse("""{"nolyric":true,"code":200}"""));
    }

    [Fact]
    public void ParseResponse_EmptyLyric_ReturnsNull()
    {
        Assert.Null(NeteaseLyricsApi.ParseResponse("""{"lrc":{"version":6,"lyric":""}}"""));
    }

    [Fact]
    public void ParseResponse_MalformedJson_ReturnsNull()
    {
        Assert.Null(NeteaseLyricsApi.ParseResponse("not-json-at-all"));
    }
}
