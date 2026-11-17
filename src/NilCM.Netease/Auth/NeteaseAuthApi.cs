using System.Text.Json;
using System.Text.Json.Serialization;
using NilCM.Core.Auth;

namespace NilCM.Netease.Auth;

/// <summary>匿名认证（weapi /weapi/register/anonimous）→ 携带 MUSIC_U 的匿名 cookie。免费歌可播；VIP 仅试听/不可播。</summary>
public sealed class NeteaseAuthApi(HttpClient httpClient) : IAuthApi
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<string> GetAnonymousCookieAsync(CancellationToken cancellationToken = default)
    {
        var response = await WeapiHttpClient.PostAsync(httpClient, "/weapi/register/anonimous", new { }, cookie: null, cancellationToken);

        AnonRegisterDto? dto = null;
        try
        {
            dto = JsonSerializer.Deserialize<AnonRegisterDto>(response.Body, JsonOptions);
        }
        catch (JsonException)
        {
            // 走 header 回退
        }

        var cookie = dto?.Data?.Cookie ?? WeapiHttpClient.TryExtractMusicU(response.SetCookie);
        if (string.IsNullOrWhiteSpace(cookie))
        {
            var body = response.Body.Length <= 120 ? response.Body : response.Body[..120];
            throw new InvalidOperationException($"匿名注册失败：{body}");
        }

        return cookie;
    }

    private sealed record AnonRegisterDto(
        [property: JsonPropertyName("data")] AnonDataDto? Data,
        [property: JsonPropertyName("code")] int Code);

    private sealed record AnonDataDto([property: JsonPropertyName("cookie")] string? Cookie);
}
