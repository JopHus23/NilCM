using System.Text.Json;
using NilCM.Netease.Crypto;

namespace NilCM.Netease;

/// <summary>eapi 调用助手（interface3 主机 + params 表单）。仅本程序集内使用。</summary>
internal static class EapiHttpClient
{
    public sealed record EapiResponse(string Body, string? SetCookie);

    /// <summary>
    /// 发送 eapi 请求。<paramref name="apiPath"/> 为摘要用的 api 路径（如 /api/song/enhance/player/url），
    /// <paramref name="postUrl"/> 为实际 POST 目标（如 https://interface3.music.163.com/eapi/...）。
    /// </summary>
    public static async Task<EapiResponse> PostAsync(
        HttpClient httpClient,
        string apiPath,
        string postUrl,
        object payload,
        string? cookie,
        CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(payload);
        var hex = EapiCrypto.Encrypt(apiPath, json);
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["params"] = hex,
        });
        using var request = new HttpRequestMessage(HttpMethod.Post, postUrl) { Content = content };
        if (!string.IsNullOrEmpty(cookie))
        {
            request.Headers.TryAddWithoutValidation("Cookie", cookie);
        }

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        string? setCookie = null;
        if (response.Headers.TryGetValues("Set-Cookie", out var values))
        {
            setCookie = string.Join(", ", values);
        }

        return new EapiResponse(body, setCookie);
    }
}
