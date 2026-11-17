using System.Text.Json;
using System.Text.Json.Serialization;
using NilCM.Core.Auth;

namespace NilCM.Infrastructure.TokenStore;

/// <summary>cookie 持久化（JSON 文件，%APPDATA%/NilCM/tokens.json）。</summary>
/// <remarks>已知债务：Windows DPAPI 加密未做（ARCHITECTURE.md §2），当前明文存储；macOS/Linux 等价方案待补。</remarks>
public sealed class JsonFileTokenStore : ITokenStore
{
    private readonly string _filePath;

    public JsonFileTokenStore()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "NilCM",
            "tokens.json"))
    {
    }

    public JsonFileTokenStore(string filePath) => _filePath = filePath;

    public async Task<string?> GetCookieAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
        {
            return null;
        }

        try
        {
            await using var stream = File.OpenRead(_filePath);
            var dto = await JsonSerializer.DeserializeAsync<TokenFileDto>(stream, cancellationToken: cancellationToken);
            return dto?.Cookie;
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            return null; // 文件损坏视为无凭据
        }
    }

    public async Task SaveCookieAsync(string cookie, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        await using var stream = File.Create(_filePath);
        var dto = new TokenFileDto(cookie, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        await JsonSerializer.SerializeAsync(stream, dto, cancellationToken: cancellationToken);
    }

    private sealed record TokenFileDto(
        [property: JsonPropertyName("cookie")] string? Cookie,
        [property: JsonPropertyName("savedAt")] long SavedAt);
}
