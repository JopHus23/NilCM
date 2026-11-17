using System.Text.Json;
using System.Text.Json.Serialization;
using NilCM.Core.Auth;

namespace NilCM.Netease.Auth;

/// <summary>
/// 二维码登录适配：weapi /weapi/login/qr/key 取 unikey；轮询 /login/qr/check（历史上为非加密端点，实连探针验证中）。
/// 803 成功时从 Set-Cookie 捕获 MUSIC_U。
/// </summary>
public sealed class NeteaseAuthApi(HttpClient httpClient) : IAuthApi
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<QrLoginSession> CreateQrLoginAsync(CancellationToken cancellationToken = default)
    {
        var response = await WeapiHttpClient.PostAsync(httpClient, "/weapi/login/qr/key", new { }, cookie: null, cancellationToken);
        var dto = TryDeserialize<QrKeyDto>(response.Body);
        var unikey = dto?.Data?.Unikey;
        if (string.IsNullOrWhiteSpace(unikey))
        {
            throw new InvalidOperationException($"二维码 key 获取失败：{Truncate(response.Body)}");
        }

        // 二维码内容 URL 是适配器知识：手机 App 扫码后即向该地址授权
        return new QrLoginSession(unikey, $"https://music.163.com/login?codekey={unikey}");
    }

    public async Task<QrCheckResult> CheckQrLoginAsync(string unikey, CancellationToken cancellationToken = default)
    {
        var response = await WeapiHttpClient.GetAsync(
            httpClient,
            $"/login/qr/check?key={Uri.EscapeDataString(unikey)}",
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

    private sealed record QrKeyDto([property: JsonPropertyName("data")] QrKeyDataDto? Data);

    private sealed record QrKeyDataDto([property: JsonPropertyName("unikey")] string? Unikey);

    private sealed record QrCheckDto([property: JsonPropertyName("code")] int Code);
}

