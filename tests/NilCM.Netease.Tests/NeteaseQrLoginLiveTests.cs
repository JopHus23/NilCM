using NilCM.Core.Playback;
using NilCM.Infrastructure.TokenStore;
using NilCM.Netease.Playback;
using Xunit.Abstractions;

namespace NilCM.Netease.Tests;

/// <summary>
/// 实连冒烟（默认跳过，需本机 %APPDATA%/NilCM/tokens.json 里有有效登录 cookie）。
/// 2026-11 实测记录：web weapi 旧版 enhance（br 参数制）对全部访客失效；带完整 cookie+csrf 的 weapi v1（level 制）返回
/// 参数错误 400——正确负载形态待参照可用模块（弹幕姬网易云模块）复刻。
/// </summary>
public class NeteaseQrLoginLiveTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;
    private readonly HttpClient _httpNoCookies = new(new SocketsHttpHandler { UseCookies = false })
    {
        BaseAddress = new Uri("https://music.163.com/"),
        Timeout = TimeSpan.FromSeconds(15),
    };

    [Fact(Skip = "默认跳过：需要本机 tokens.json 有效登录 cookie；负载形态待参照可用模块修正后手动运行")]
    public async Task LoggedIn_ManualCookie_ResolvesFreeSongUrl()
    {
        var store = new JsonFileTokenStore();
        var cookie = await store.GetCookieAsync();
        Assert.False(string.IsNullOrWhiteSpace(cookie));

        var resolver = new NeteasePlayUrlApi(_httpNoCookies, store);
        var result = await resolver.ResolveAsync(186016, AudioQuality.High);

        Assert.NotNull(result);
        _output.WriteLine($"resolved: br={result!.Bitrate} trial={result.IsTrial} urlLen={result.Url.Length}");
    }
}




