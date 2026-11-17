namespace NilCM.Core.Playback;

/// <summary>播放地址解析抽象。失败（无版权/接口失效）返回 null；试听返回 IsTrial=true。</summary>
public interface IPlayUrlResolver
{
    Task<PlayUrlResult?> ResolveAsync(long trackId, AudioQuality quality, CancellationToken cancellationToken = default);
}
