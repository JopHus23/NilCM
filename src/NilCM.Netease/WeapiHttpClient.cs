using System.Text.Json;
using NilCM.Netease.Crypto;

namespace NilCM.Netease;

/// <summary>weapi 调用助手：序列化 → 加密 → 表单 POST。仅本程序集内使用。</summary>
internal static class WeapiHttpClient
{
    public sealed record WeapiResponse(string Body, string? SetCookie);

    public static async Task<WeapiResponse> PostAsync(
        HttpClient httpClient,
        string path,
        object payload,
        string? cookie,
        CancellationToken cancellationToken)
    {
        var (ps, esk) = WeapiCrypto.Encrypt(JsonSerializer.Serialize(payload));
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["params"] = ps,
            ["encSecKey"] = esk,
        });
        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = content };
        if (!string.IsNullOrEmpty(cookie))
        {
            request.Headers.TryAddWithoutValidation("Cookie", cookie);
        }

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        string? setCookie = null;
        if (response.Headers.TryGetValues("Set-Cookie", out var values))
        {
            setCookie = string.Join(", ", values);
        }

        return new WeapiResponse(body, setCookie);
    }

    /// <summary>从任意 cookie 文本/Set-Cookie 头中提取 MUSIC_U=xxx 片段（Set-Cookie 里的 Expires 逗号会破坏按 "; " 切分）。</summary>
    public static string? TryExtractMusicU(string? source)
    {
        if (string.IsNullOrEmpty(source))
        {
            return null;
        }

        var match = System.Text.RegularExpressions.Regex.Match(source, @"MUSIC_U=[A-Za-z0-9]+");
        return match.Success ? match.Value : null;
    }

    /// <summary>普通 GET（非加密端点，如 /login/qr/check）。返回 body + Set-Cookie。</summary>
    public static async Task<WeapiResponse> GetAsync(HttpClient httpClient, string path, string? cookie, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (!string.IsNullOrEmpty(cookie))
        {
            request.Headers.TryAddWithoutValidation("Cookie", cookie);
        }

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        string? setCookie = null;
        if (response.Headers.TryGetValues("Set-Cookie", out var values))
        {
            setCookie = string.Join(", ", values);
        }

        return new WeapiResponse(body, setCookie);
    }
}
