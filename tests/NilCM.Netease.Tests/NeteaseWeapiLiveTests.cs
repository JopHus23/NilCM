using NilCM.Core.Auth;
using NilCM.Core.Playback;
using NilCM.Netease.Auth;
using NilCM.Netease.Playback;

namespace NilCM.Netease.Tests;

/// <summary>
/// 实连冒烟测试（默认跳过）。验证完整链路：匿名注册 → cookie → 播放地址。
/// 2026-11 实测现状：匿名注册已收紧（各负载均 code:400），旧公开 enhance 端点/外链/X-Real-IP 伪装均失效——
/// 本测试在登录切片（扫码写入 MUSIC_U）落地前预计无法通过，仅作契约文档保留。
/// </summary>
public class NeteaseWeapiLiveTests
{
    private sealed class StaticTokenStore(string cookie) : ITokenStore
    {
        public Task<string?> GetCookieAsync(CancellationToken cancellationToken = default) => Task.FromResult<string?>(cookie);

        public Task SaveCookieAsync(string cookie, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    [Fact(Skip = "默认跳过：匿名注册已收紧，待登录切片落地后手动验证。运行方式：移除 Skip → dotnet test --filter NeteaseWeapiLiveTests")]
    public async Task AnonymousRegister_Then_FreeSongPlayable()
    {
        var http = new HttpClient { BaseAddress = new Uri("https://music.163.com/"), Timeout = TimeSpan.FromSeconds(15) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        http.DefaultRequestHeaders.Referrer = new Uri("https://music.163.com/");

        var auth = new NeteaseAuthApi(http);
        var cookie = await auth.GetAnonymousCookieAsync();
        Assert.False(string.IsNullOrWhiteSpace(cookie));

        var resolver = new NeteasePlayUrlApi(http, new StaticTokenStore(cookie), auth);
        var result = await resolver.ResolveAsync(186016, AudioQuality.High);

        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result!.Url));
    }
}



