using NilCM.Core.Playback;
using NilCM.Infrastructure.TokenStore;
using NilCM.Netease.Playback;
using Xunit.Abstractions;

namespace NilCM.Netease.Tests;

/// <summary>
/// 实连冒烟（默认跳过，需本机 %APPDATA%/NilCM/tokens.json 里有有效登录 cookie）。
/// 2026-11 已实测通过：347230 等 5 首歌曲（含 VIP 曲目）均解析出 CDN 直链；
/// 186016（晴天）当前无播放权益属个例，勿再用作默认样本。
/// </summary>
public class NeteaseQrLoginLiveTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;
    private readonly HttpClient _httpNoCookies = new(new SocketsHttpHandler { UseCookies = false })
    {
        BaseAddress = new Uri("https://music.163.com/"),
        Timeout = TimeSpan.FromSeconds(15),
    };

    [Fact(Skip = "默认跳过：需要本机 tokens.json 有效登录 cookie。运行方式：移除 Skip → dotnet test --filter NeteaseQrLoginLiveTests")]
    public async Task LoggedIn_ManualCookie_ResolvesFreeSongUrl()
    {
        var store = new JsonFileTokenStore();
        var cookie = await store.GetCookieAsync();
        Assert.False(string.IsNullOrWhiteSpace(cookie));

        var resolver = new NeteasePlayUrlApi(_httpNoCookies, store);
        var result = await resolver.ResolveAsync(347230, AudioQuality.High);

        Assert.NotNull(result);
        _output.WriteLine($"resolved: br={result!.Bitrate} trial={result.IsTrial} urlLen={result.Url.Length}");
    }
}
