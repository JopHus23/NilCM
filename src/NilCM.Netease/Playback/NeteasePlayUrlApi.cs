using System.Text.Json;
using System.Text.Json.Serialization;
using NilCM.Core.Auth;
using NilCM.Core.Playback;

namespace NilCM.Netease.Playback;

/// <summary>
/// 播放地址解析（weapi /weapi/song/enhance/player/url）。
/// 实测可用形态（2026-11，参照 ExtendNetease_DGJModule）：payload 为 {"ids":[数字数组],"br":整数}，
/// 不带 csrf_token；需完整登录 cookie（含 MUSIC_U，用户导入或扫码登录写入 ITokenStore）。
/// 免费歌可播；VIP 账号可播 VIP 曲目；无权益单曲返回 null（如 186016 晴天当前不可解析属个例）。
/// DTO 不出程序集。
/// </summary>
public sealed class NeteasePlayUrlApi(HttpClient httpClient, ITokenStore tokenStore) : IPlayUrlResolver
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<PlayUrlResult?> ResolveAsync(long trackId, AudioQuality quality, CancellationToken cancellationToken = default)
    {
        var cookie = await tokenStore.GetCookieAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(cookie))
        {
            return null; // 未登录
        }

        var response = await WeapiHttpClient.PostAsync(
            httpClient,
            "/weapi/song/enhance/player/url",
            new { ids = new long[] { trackId }, br = BitrateOf(quality) },
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
