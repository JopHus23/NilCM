using NilCM.Netease.Auth;
using Xunit.Abstractions;

namespace NilCM.Netease.Tests;

/// <summary>实连探针：临时使用，验证 eapi 二维码登录端点存活状态（unikey → check 轮询）。</summary>
public class NeteaseQrLoginLiveTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };

    [Fact(Skip = "诊断探针：eapi unikey 端点存在但必需字段未知（{} → 参数错误 400），待抓包官方客户端补全字段后手动运行")]
    public async Task ProbeEapiQrLogin()
    {
        var auth = new NeteaseAuthApi(_http);

        var session = await auth.CreateQrLoginAsync();
        _output.WriteLine($"[key] unikey={session.Unikey}");
        _output.WriteLine($"[key] contentUrl={session.QrContentUrl}");

        for (var i = 1; i <= 2; i++)
        {
            var check = await auth.CheckQrLoginAsync(session.Unikey);
            _output.WriteLine($"[check#{i}] state={check.State}");
            await Task.Delay(1200);
        }
    }
}
