namespace NilCM.Core.Playback;

/// <summary>
/// 播放用例编排：解析地址 → 装载 → 播放；并透传传输控制。
/// 依赖全部为 Core 接口，可单测。
/// </summary>
public sealed class PlaybackService(IAudioPlayer player, IPlayUrlResolver playUrls)
{
    public PlaybackState State => player.State;

    public TimeSpan Position => player.Position;

    public TimeSpan Duration => player.Duration;

    public float Volume
    {
        get => player.Volume;
        set => player.Volume = value;
    }

    public event EventHandler<PlaybackErrorEventArgs>? PlaybackFailed
    {
        add => player.PlaybackFailed += value;
        remove => player.PlaybackFailed -= value;
    }

    /// <summary>解析并播放。返回 null = 解析失败（无版权/接口失效）；非 null = 已开始播放的结果。</summary>
    public async Task<PlayUrlResult?> PlayAsync(long trackId, AudioQuality quality = AudioQuality.High, CancellationToken cancellationToken = default)
    {
        var resolved = await playUrls.ResolveAsync(trackId, quality, cancellationToken);
        if (resolved is null)
        {
            return null;
        }

        player.Load(resolved.Url);
        player.Play();
        return resolved;
    }

    public void Pause() => player.Pause();

    public void Resume() => player.Resume();

    public void Stop() => player.Stop();

    public void Seek(TimeSpan position) => player.Seek(position);
}
