using NilCM.Core.Models;

namespace NilCM.Core.Lyrics;

/// <summary>歌词用例编排（纯逻辑、可单测）：取原始 LRC → 解析为结构化歌词行。</summary>
public sealed class LyricsService(ILyricsApi lyricsApi)
{
    public async Task<IReadOnlyList<LyricLine>> GetLyricAsync(long trackId, CancellationToken cancellationToken = default)
    {
        var raw = await lyricsApi.GetRawLyricAsync(trackId, cancellationToken);
        return LyricParser.Parse(raw);
    }
}
