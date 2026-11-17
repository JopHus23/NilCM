namespace NilCM.Core.Lyrics;

/// <summary>歌词数据源抽象（返回原始 LRC 文本）。实现位于 NilCM.Netease。</summary>
public interface ILyricsApi
{
    /// <summary>返回原始 LRC 文本；无歌词或获取失败返回 null（网络失败语义由实现内部消化，不向 Core 抛异常）。</summary>
    Task<string?> GetRawLyricAsync(long trackId, CancellationToken cancellationToken = default);
}
