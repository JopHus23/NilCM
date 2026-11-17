using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using NilCM.Core.Lyrics;

namespace NilCM.Netease.Lyrics;

/// <summary>
/// 网易歌词接口适配（易腐囚笼内部）：DTO 永不出本程序集。
/// 端点：/api/song/lyric（公开端点，免加密/免登录）。2026-11 实测可用；
/// 旧端点 /api/lyric 已下线（业务返回 code:404 接口未找到）——接口失效时只改本文件，Core/App 零改动。
/// </summary>
public sealed class NeteaseLyricsApi(HttpClient httpClient) : ILyricsApi
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<string?> GetRawLyricAsync(long trackId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["id"] = trackId.ToString(CultureInfo.InvariantCulture),
                ["lv"] = "-1",
                ["kv"] = "-1",
                ["tv"] = "-1",
            });
            using var response = await httpClient.PostAsync("/api/song/lyric", content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return ParseResponse(await response.Content.ReadAsStringAsync(cancellationToken));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return null; // 失败语义：无歌词。不向 Core 抛网络异常。
        }
    }

    /// <summary>契约解析：录制的 JSON fixture → 原始 LRC（可离线测试）。</summary>
    internal static string? ParseResponse(string json)
    {
        LyricResponseDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<LyricResponseDto>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }

        var lyric = dto?.Lrc?.Lyric;
        return string.IsNullOrWhiteSpace(lyric) ? null : lyric;
    }
}

internal sealed record LyricResponseDto([property: JsonPropertyName("lrc")] LyricContentDto? Lrc);

internal sealed record LyricContentDto([property: JsonPropertyName("lyric")] string? Lyric);
