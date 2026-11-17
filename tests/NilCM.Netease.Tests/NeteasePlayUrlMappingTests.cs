using NilCM.Netease.Playback;

namespace NilCM.Netease.Tests;

/// <summary>契约测试：录制的 JSON fixture → PlayUrlResult（不真连网）。</summary>
public class NeteasePlayUrlMappingTests
{
    private const string FixtureNormal =
        """{"data":[{"id":186016,"url":"http://m701.music.126.net/a.mp3","br":320000,"size":10491230,"md5":"x","code":200,"type":"mp3","freeTrialInfo":null}],"code":200}""";

    private const string FixtureTrial =
        """{"data":[{"id":25906124,"url":"http://m701.music.126.net/trial.mp3","br":128000,"code":200,"freeTrialInfo":{"start":0,"end":30000}}],"code":200}""";

    private const string FixtureNoUrl =
        """{"data":[{"id":186016,"url":null,"br":0,"size":0,"md5":null,"code":404,"freeTrialInfo":null}],"code":200}""";

    private const string FixtureVipNoPermission =
        """{"data":[{"id":25906124,"url":null,"br":0,"size":0,"md5":null,"code":-110,"freeTrialInfo":null}],"code":200}""";

    [Fact]
    public void MapResponse_FreeSong_ReturnsResult()
    {
        var result = NeteasePlayUrlApi.MapResponse(FixtureNormal, 186016);

        Assert.NotNull(result);
        Assert.Equal("http://m701.music.126.net/a.mp3", result.Url);
        Assert.Equal(320000, result.Bitrate);
        Assert.False(result.IsTrial);
    }

    [Fact]
    public void MapResponse_TrialSong_MarksIsTrial()
    {
        var result = NeteasePlayUrlApi.MapResponse(FixtureTrial, 25906124);

        Assert.NotNull(result);
        Assert.True(result.IsTrial);
    }

    [Fact]
    public void MapResponse_NoUrl_ReturnsNull()
    {
        Assert.Null(NeteasePlayUrlApi.MapResponse(FixtureNoUrl, 186016));
    }

    [Fact]
    public void MapResponse_VipNoPermission_ReturnsNull()
    {
        Assert.Null(NeteasePlayUrlApi.MapResponse(FixtureVipNoPermission, 25906124));
    }

    [Fact]
    public void MapResponse_EmptyData_ReturnsNull()
    {
        Assert.Null(NeteasePlayUrlApi.MapResponse("""{"data":[]}""", 1));
    }
}
