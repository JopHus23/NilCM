using System.Text.Json;
using System.Text.Json.Serialization;
using NilCM.Core.Auth;
using NilCM.Core.Playback;

namespace NilCM.Netease.Playback;

/// <summary>
/// 播放地址解析（weapi /weapi/song/enhance/player/url?csrf_token=...，需完整登录 cookie，含 __csrf）。
/// cookie 由用户导入（网页端复制完整 Cookie 头）或扫码登录写入 ITokenStore。
/// 免费歌可播；VIP 返回试听（IsTrial）或不可播（null）。DTO 不出程序集。
/// </summary>
public sealed class NeteasePlayUrlApi(HttpClient httpClient, ITokenStore tokenStore) : IPlayUrlResolver
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<PlayUrlResult?> ResolveAsync(long trackId, AudioQuality quality, CancellationToken cancellationToken = default)
    {
        var cookie = await tokenStore.GetCookieAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(cookie))
        {
            return null; // 未登录：见类注释（登录切片落地后此分支自然消失）
        }

        // 网页端 weapi 请求必须附带 csrf_token 查询参数（值来自 cookie 的 __csrf 字段），见实连探针记录
        var csrf = TryExtractCsrf(cookie);
        var path = $"/weapi/song/enhance/player/url?csrf_token={Uri.EscapeDataString(csrf ?? string.Empty)}";

        var response = await WeapiHttpClient.PostAsync(
            httpClient,
            path,
            new { ids = $"[{trackId}]", br = BitrateOf(quality), csrf_token = csrf ?? string.Empty },
            cookie,
            cancellationToken);

        return MapResponse(response.Body, trackId);
    }

    /// <summary>从 cookie 串提取 __csrf 值（网页 weapi 请求的查询参数）。重复出现时取最后一个（服务器惯例）。</summary>
    private static string? TryExtractCsrf(string cookie)
    {
        var matches = System.Text.RegularExpressions.Regex.Matches(cookie, @"(?:^|;\s*)__csrf=([^;]+)");
        return matches.Count > 0 ? matches[^1].Groups[1].Value.Trim() : null;
    }

    /// <summary>契约解析：录制的 JSON fixture → PlayUrlResult（可离线测试）。</summary>
    internal static PlayUrlResult? MapResponse(string json, long trackId)
    {
        PlayUrlResponseDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<PlayUrlResponseDto>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }

        var item = dto?.Data is { Count: > 0 } ? dto.Data[0] : null;
        if (item?.Url is not { Length: > 0 })
        {
            return null; // 无版权 / 需 VIP / 参数错（code 404/-110 等）
        }

        return new PlayUrlResult(trackId, item.Url, item.Br, item.FreeTrialInfo is not null);
    }

    private static int BitrateOf(AudioQuality quality) => quality switch
    {
        AudioQuality.High => 320000,
        _ => 128000,
    };

    private sealed record PlayUrlResponseDto([property: JsonPropertyName("data")] List<PlayUrlItemDto>? Data);

    private sealed record PlayUrlItemDto(
        [property: JsonPropertyName("url")] string? Url,
        [property: JsonPropertyName("br")] int Br,
        [property: JsonPropertyName("code")] int Code,
        [property: JsonPropertyName("freeTrialInfo")] FreeTrialDto? FreeTrialInfo);

    private sealed record FreeTrialDto(
        [property: JsonPropertyName("start")] int Start,
        [property: JsonPropertyName("end")] int End);
}
