using System.Text.Json;
using System.Text.Json.Serialization;
using NilCM.Core.Auth;

namespace NilCM.Netease.Auth;

/// <summary>
/// 二维码登录适配（eapi 通道，interface3 主机）：
/// key = /eapi/login/qrcode/unikey；轮询 = /eapi/login/qrcode/client/login（800 过期 / 801 待扫 / 802 已扫 / 803 成功）。
/// 803 成功时从 Set-Cookie 捕获 MUSIC_U。UI 集成（二维码渲染/轮询触发）留待 UI 阶段。
/// </summary>
public sealed class NeteaseAuthApi(HttpClient httpClient) : IAuthApi
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<QrLoginSession> CreateQrLoginAsync(CancellationToken cancellationToken = default)
    {
        var response = await EapiHttpClient.PostAsync(
            httpClient,
            "/api/login/qrcode/unikey",
            "https://interface3.music.163.com/eapi/login/qrcode/unikey",
            new { },
            cookie: null,
            cancellationToken);

        var dto = TryDeserialize<QrKeyDto>(response.Body);
        var unikey = dto?.Data?.Unikey ?? dto?.Unikey;
        if (string.IsNullOrWhiteSpace(unikey))
        {
            throw new InvalidOperationException($"二维码 key 获取失败：{Truncate(response.Body)}");
        }

        // 二维码内容 URL 是适配器知识：手机 App 扫码后即向该地址授权
        return new QrLoginSession(unikey, $"https://music.163.com/login?codekey={unikey}");
    }

    public async Task<QrCheckResult> CheckQrLoginAsync(string unikey, CancellationToken cancellationToken = default)
    {
        var response = await EapiHttpClient.PostAsync(
            httpClient,
            "/api/login/qrcode/client/login",
            "https://interface3.music.163.com/eapi/login/qrcode/client/login",
            new { key = unikey },
            cookie: null,
            cancellationToken);

        var dto = TryDeserialize<QrCheckDto>(response.Body);
        var state = dto?.Code switch
        {
            800 => QrLoginState.Expired,
            801 => QrLoginState.Waiting,
            802 => QrLoginState.Scanned,
            803 => QrLoginState.Success,
            _ => QrLoginState.Waiting,
        };

        var cookie = state == QrLoginState.Success
            ? WeapiHttpClient.TryExtractMusicU(response.SetCookie) ?? WeapiHttpClient.TryExtractMusicU(response.Body)
            : null;

        if (state == QrLoginState.Success && string.IsNullOrWhiteSpace(cookie))
        {
            throw new InvalidOperationException($"二维码登录成功但未捕获 cookie：{Truncate(response.Body)}");
        }

        return new QrCheckResult(state, cookie);
    }

    private static T? TryDeserialize<T>(string json) where T : class
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string Truncate(string s) => s.Length <= 120 ? s : s[..120];

    private sealed record QrKeyDto(
        [property: JsonPropertyName("data")] QrKeyDataDto? Data,
        [property: JsonPropertyName("unikey")] string? Unikey);

    private sealed record QrKeyDataDto([property: JsonPropertyName("unikey")] string? Unikey);

    private sealed record QrCheckDto([property: JsonPropertyName("code")] int Code);
}


