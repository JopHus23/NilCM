namespace NilCM.Core.Playback;

/// <summary>
/// 播放用例编排：队列导航 + （网易曲目）解析地址 → 装载 → 播放；本地文件跳过解析直喂引擎。
/// 依赖全部为 Core 接口与类型，可单测。
/// </summary>
public sealed class PlaybackService(IAudioPlayer player, IPlayUrlResolver playUrls, PlayQueue queue)
{
    public PlayQueue Queue => queue;

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

    /// <summary>最近一次成功解析的播放地址（供 UI 展示试听标记等）。</summary>
    public PlayUrlResult? LastResolved { get; private set; }

    /// <summary>播放队列当前项。本地文件直接装载；网易曲目解析失败返回 false。</summary>
    public async Task<bool> PlayCurrentAsync(CancellationToken cancellationToken = default)
    {
        var item = queue.Current;
        if (item is null)
        {
            return false;
        }

        if (item.LocalPath is not null)
        {
            LastResolved = null;
            player.Load(item.LocalPath);
            player.Play();
            return true;
        }

        if (item.TrackId is null)
        {
            return false;
        }

        var resolved = await playUrls.ResolveAsync(item.TrackId.Value, AudioQuality.High, cancellationToken);
        if (resolved is null)
        {
            return false;
        }

        LastResolved = resolved;
        player.Load(resolved.Url);
        player.Play();
        return true;
    }

    /// <summary>下一项。userSkip=true 为用户手动切歌（忽略单曲循环强制前进）。Sequential 队尾返回 false。</summary>
    public async Task<bool> PlayNextAsync(bool userSkip = false, CancellationToken cancellationToken = default)
    {
        if (queue.Next(userSkip) is null)
        {
            return false;
        }

        return await PlayCurrentAsync(cancellationToken);
    }

    /// <summary>上一项。</summary>
    public Task<bool> PlayPreviousAsync(CancellationToken cancellationToken = default)
    {
        queue.Previous();
        return PlayCurrentAsync(cancellationToken);
    }

    /// <summary>点歌：入队到队尾。</summary>
    public void EnqueueTrack(long trackId, string title) => queue.Enqueue(PlayItem.FromTrack(trackId, title));

    /// <summary>入队本地文件。</summary>
    public void EnqueueLocalFile(string path) => queue.Enqueue(PlayItem.FromLocalFile(path));

    public void Pause() => player.Pause();

    public void Resume() => player.Resume();

    public void Stop() => player.Stop();

    public void Seek(TimeSpan position) => player.Seek(position);
}

