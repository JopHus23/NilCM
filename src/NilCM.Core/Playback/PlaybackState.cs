namespace NilCM.Core.Playback;

/// <summary>播放状态。实现须保证该属性可在任意线程安全读取。</summary>
public enum PlaybackState
{
    Idle,
    Loading,
    Playing,
    Paused,
    Stopped,
    Error,
}
