namespace NilCM.Core.Playback;

/// <summary>音频引擎抽象（实现位于 NilCM.Playback）。引擎线程差异必须由实现内部消化。</summary>
public interface IAudioPlayer : IDisposable
{
    PlaybackState State { get; }

    TimeSpan Position { get; }

    TimeSpan Duration { get; }

    /// <summary>音量，0.0 - 1.0。</summary>
    float Volume { get; set; }

    /// <summary>装载待播资源（URL / 本地路径）。装载后调用 <see cref="Play"/>。</summary>
    void Load(string url);

    void Play();

    void Pause();

    void Resume();

    void Stop();

    void Seek(TimeSpan position);

    /// <summary>引擎层失败（加载/解码/引擎错误）。业务层失败（无版权等）属于 IPlayUrlResolver 的职责，不走此事件。</summary>
    event EventHandler<PlaybackErrorEventArgs>? PlaybackFailed;
}

/// <summary>引擎层失败详情。</summary>
public sealed record PlaybackErrorEventArgs(string Code, string Message);
