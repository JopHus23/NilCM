using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NilCM.Core.Lyrics;
using NilCM.Core.Playback;

namespace NilCM.App.ViewModels;

/// <summary>主窗口 ViewModel：只依赖 Core 的服务，不出现任何 Avalonia 类型（纪律见 ARCHITECTURE.md §2）。</summary>
public partial class MainViewModel(LyricsService lyrics, PlaybackService playback) : ViewModelBase
{
    private readonly LyricsService _lyrics = lyrics;
    private readonly PlaybackService _playback = playback;
    private CancellationTokenSource? _pollCts;
    private bool _currentIsTrial;
    private bool _lastStateWasPlaying;
    private bool _stopRequested;
    private bool _autoAdvanceRunning;

    [ObservableProperty]
    private string _trackIdText = "347230";

    [ObservableProperty]
    private string _statusText = "输入歌曲 ID 点歌入队，或添加本地文件播放。";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _nowPlayingText = "未在播放";

    [ObservableProperty]
    private string _positionText = "00:00 / 00:00";

    [ObservableProperty]
    private double _progressPercent;

    [ObservableProperty]
    private int _volumePercent = 80;

    [ObservableProperty]
    private string _modeText = "顺序播放";

    public ObservableCollection<string> LyricLines { get; } = [];

    public ObservableCollection<string> QueueDisplay { get; } = [];

    partial void OnVolumePercentChanged(int value) => _playback.Volume = value / 100f;

    [RelayCommand]
    private async Task LoadLyricAsync(CancellationToken cancellationToken)
    {
        if (!long.TryParse(TrackIdText.Trim(), out var trackId))
        {
            StatusText = "歌曲 ID 无效（应为数字）。";
            return;
        }

        IsBusy = true;
        StatusText = "加载中…";
        try
        {
            var lines = await _lyrics.GetLyricAsync(trackId, cancellationToken);
            LyricLines.Clear();
            foreach (var line in lines)
            {
                LyricLines.Add($"{(int)line.Time.TotalMinutes:00}:{line.Time.Seconds:00}.{line.Time.Milliseconds / 10:00}  {line.Text}");
            }

            StatusText = lines.Count == 0 ? "未找到歌词。" : $"已加载 {lines.Count} 行歌词。";
        }
        catch (OperationCanceledException)
        {
            StatusText = "已取消。";
        }
        catch (Exception ex)
        {
            StatusText = $"加载失败：{ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>点歌：入队到队尾并立即播放。</summary>
    [RelayCommand]
    private async Task PlayAsync(CancellationToken cancellationToken)
    {
        if (!long.TryParse(TrackIdText.Trim(), out var trackId))
        {
            StatusText = "歌曲 ID 无效（应为数字）。";
            return;
        }

        IsBusy = true;
        try
        {
            StatusText = "解析播放地址…";
            _playback.EnqueueTrack(trackId, $"歌曲 {trackId}");
            _playback.Queue.SetCurrent(_playback.Queue.Count - 1);
            RefreshQueueDisplay();

            var started = await _playback.PlayCurrentAsync(cancellationToken);
            if (!started)
            {
                StatusText = "无法播放：解析不到播放地址（可能无版权/需 VIP，或未导入登录 cookie）。";
                NowPlayingText = "未在播放";
                return;
            }

            _stopRequested = false;
            _currentIsTrial = _playback.LastResolved?.IsTrial ?? false;
            StatusText = _currentIsTrial ? "已开始播放（试听）。" : "已开始播放。";
            StartPolling();
        }
        catch (OperationCanceledException)
        {
            StatusText = "已取消。";
        }
        catch (Exception ex)
        {
            StatusText = $"播放失败：{ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Pause() => _playback.Pause();

    [RelayCommand]
    private void Resume() => _playback.Resume();

    [RelayCommand]
    private void StopPlayback()
    {
        _stopRequested = true;
        _playback.Stop();
        StopPolling();
        NowPlayingText = "已停止";
        PositionText = "00:00 / 00:00";
        ProgressPercent = 0;
    }

    [RelayCommand]
    private async Task NextTrackAsync()
    {
        var ok = await _playback.PlayNextAsync(userSkip: true);
        RefreshQueueDisplay();
        StatusText = ok ? "已切至下一首。" : "已是队列末尾（顺序模式）。";
        _stopRequested = false;
        StartPolling();
    }

    [RelayCommand]
    private async Task PreviousTrackAsync()
    {
        var ok = await _playback.PlayPreviousAsync();
        RefreshQueueDisplay();
        StatusText = ok ? "已切至上一首。" : "队列播放失败。";
    }

    [RelayCommand]
    private void CycleMode()
    {
        _playback.Queue.Mode = _playback.Queue.Mode switch
        {
            QueueMode.Sequential => QueueMode.RepeatAll,
            QueueMode.RepeatAll => QueueMode.RepeatOne,
            _ => QueueMode.Sequential,
        };
        ModeText = _playback.Queue.Mode switch
        {
            QueueMode.Sequential => "顺序播放",
            QueueMode.RepeatAll => "列表循环",
            QueueMode.RepeatOne => "单曲循环",
            _ => "?",
        };
    }

    /// <summary>由 View 层文件选择器调用（VM 保持无 Avalonia 类型）。入队并按需开播。</summary>
    public void AddLocalFiles(IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            _playback.EnqueueLocalFile(path);
        }

        RefreshQueueDisplay();
        if (_playback.State is PlaybackState.Idle or PlaybackState.Stopped)
        {
            _playback.Queue.SetCurrent(_playback.Queue.Count - 1);
            _stopRequested = false;
            RefreshQueueDisplay();
            _ = PlayCurrentAndPollAsync();
        }
    }

    private async Task PlayCurrentAndPollAsync()
    {
        var ok = await _playback.PlayCurrentAsync();
        StatusText = ok ? "开始播放本地文件。" : "本地文件播放失败。";
        if (ok)
        {
            StartPolling();
        }
    }

    private void StartPolling()
    {
        StopPolling();
        _pollCts = new CancellationTokenSource();
        _ = PollAsync(_pollCts.Token);
    }

    private void StopPolling()
    {
        _pollCts?.Cancel();
        _pollCts = null;
    }

    /// <summary>轮询而非订阅引擎事件：引擎回调在原生线程，轮询天然规避 UI 线程切换问题（见 ARCHITECTURE.md §2 纪律 2）。</summary>
    private async Task PollAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            RefreshPlaybackUi();
            try
            {
                await Task.Delay(300, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        RefreshPlaybackUi();
    }

    private void RefreshPlaybackUi()
    {
        var state = _playback.State;

        // 播完自动切下一首（用户手动停止时不触发）
        if (_lastStateWasPlaying && state == PlaybackState.Stopped && !_stopRequested && !_autoAdvanceRunning)
        {
            _ = AutoAdvanceAsync();
        }

        _lastStateWasPlaying = state is PlaybackState.Playing;

        NowPlayingText = state switch
        {
            PlaybackState.Playing => _currentIsTrial ? "正在播放（试听）" : "正在播放",
            PlaybackState.Loading => "缓冲中…",
            PlaybackState.Paused => "已暂停",
            PlaybackState.Error => "播放出错",
            PlaybackState.Stopped => "已停止",
            _ => "未在播放",
        };

        var position = _playback.Position;
        var duration = _playback.Duration;
        PositionText = $"{Fmt(position)} / {Fmt(duration)}";
        ProgressPercent = duration > TimeSpan.Zero ? position.TotalSeconds / duration.TotalSeconds * 100 : 0;
        RefreshQueueDisplay();
    }

    private async Task AutoAdvanceAsync()
    {
        if (_autoAdvanceRunning)
        {
            return;
        }

        _autoAdvanceRunning = true;
        try
        {
            var ok = await _playback.PlayNextAsync();
            StatusText = ok ? "自动切至下一首。" : "队列已播完（顺序模式）。";
            if (!ok)
            {
                NowPlayingText = "队列已播完";
            }
        }
        finally
        {
            _autoAdvanceRunning = false;
        }
    }

    private void RefreshQueueDisplay()
    {
        var queue = _playback.Queue;
        QueueDisplay.Clear();
        for (var i = 0; i < queue.Count; i++)
        {
            QueueDisplay.Add((i == queue.CurrentIndex ? "▶ " : "   ") + queue.Items[i].Title);
        }
    }

    private static string Fmt(TimeSpan t) => $"{(int)t.TotalMinutes:00}:{t.Seconds:00}";
}
