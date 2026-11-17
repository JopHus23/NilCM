using LibVLCSharp.Shared;
using NilCM.Core.Playback;

namespace NilCM.Playback.Engine;

/// <summary>
/// LibVLC 引擎适配：native 库加载、VLC 状态映射、引擎线程事件——坑全部关在本类。
/// App/ViewModel 永远看不到 LibVLCSharp 的类型。
/// </summary>
public sealed class LibVlcAudioPlayer : IAudioPlayer
{
    private readonly LibVLC _libVlc;
    private readonly MediaPlayer _player;

    public LibVlcAudioPlayer()
    {
        // 全限定调用：裸写 Core.Initialize() 会被父命名空间 NilCM.Core 遮蔽
        LibVLCSharp.Shared.Core.Initialize();
        _libVlc = new LibVLC(enableDebugLogs: false);
        _player = new MediaPlayer(_libVlc);
        _player.EncounteredError += (_, _) =>
            PlaybackFailed?.Invoke(this, new PlaybackErrorEventArgs("engine_error", "播放引擎错误（加载/解码失败）。"));
    }

    public PlaybackState State => _player.State switch
    {
        VLCState.Opening or VLCState.Buffering => PlaybackState.Loading,
        VLCState.Playing => PlaybackState.Playing,
        VLCState.Paused => PlaybackState.Paused,
        VLCState.Stopped or VLCState.Ended => PlaybackState.Stopped,
        VLCState.Error => PlaybackState.Error,
        _ => PlaybackState.Idle,
    };

    public TimeSpan Position => TimeSpan.FromMilliseconds(Math.Max(0, _player.Time));

    public TimeSpan Duration => TimeSpan.FromMilliseconds(Math.Max(0, _player.Length));

    public float Volume
    {
        get => Math.Clamp(_player.Volume, 0, 100) / 100f;
        set => _player.Volume = (int)Math.Round(Math.Clamp(value, 0f, 1f) * 100);
    }

    public void Load(string url)
    {
        _player.Media = new Media(_libVlc, new Uri(url));
    }

    public void Play() => _player.Play();

    public void Pause()
    {
        if (_player.State == VLCState.Playing)
        {
            _player.Pause(); // LibVLCSharp 3.x：Pause() 为切换语义，先判状态
        }
    }

    public void Resume()
    {
        if (_player.State == VLCState.Paused)
        {
            _player.Play(); // 暂停态下 Play() 即恢复
        }
    }

    public void Stop() => _player.Stop();

    public void Seek(TimeSpan position) => _player.Time = (long)Math.Max(0, position.TotalMilliseconds);

    public event EventHandler<PlaybackErrorEventArgs>? PlaybackFailed;

    public void Dispose()
    {
        _player.Dispose();
        _libVlc.Dispose();
    }
}
