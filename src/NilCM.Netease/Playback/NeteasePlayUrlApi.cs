using System.Text.Json;
using System.Text.Json.Serialization;
using NilCM.Core.Auth;
using NilCM.Core.Playback;

namespace NilCM.Netease.Playback;

/// <summary>
/// 播放地址解析（weapi /weapi/song/enhance/player/url，需有效用户 cookie）。
/// 2026-11 实测：匿名访问已全面失效——裸请求/os=pc/X-Real-IP/外链重定向/匿名注册全部无法取得 URL。
/// 因此本解析器在登录切片（扫码登录写入 MUSIC_U 到 ITokenStore）落地前返回 null；cookie 就位后自动激活，Core/App 零改动。
/// </summary>
public sealed class NeteasePlayUrlApi(HttpClient httpClient, ITokenStore tokenStore, IAuthApi authApi) : IPlayUrlResolver
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<PlayUrlResult?> ResolveAsync(long trackId, AudioQuality quality, CancellationToken cancellationToken = default)
    {
        var cookie = await tokenStore.GetCookieAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(cookie))
        {
            cookie = await authApi.GetAnonymousCookieAsync(cancellationToken);
            await tokenStore.SaveCookieAsync(cookie, cancellationToken);
        }

        var response = await WeapiHttpClient.PostAsync(
            httpClient,
            "/weapi/song/enhance/player/url",
            new { ids = $"[{trackId}]", br = BitrateOf(quality), csrf_token = string.Empty },
            cookie,
            cancellationToken);

        return MapResponse(response.Body, trackId);
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
