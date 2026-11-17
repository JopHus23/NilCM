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

    [ObservableProperty]
    private string _trackIdText = "186016";

    [ObservableProperty]
    private string _statusText = "输入歌曲 ID，可获取歌词或播放。";

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

    partial void OnVolumePercentChanged(int value) => _playback.Volume = value / 100f;

    public ObservableCollection<string> LyricLines { get; } = [];

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
            var result = await _playback.PlayAsync(trackId, AudioQuality.High, cancellationToken);
            if (result is null)
            {
                StatusText = "无法播放：解析不到播放地址（可能无版权或需要 VIP）。";
                NowPlayingText = "未在播放";
                return;
            }

            _currentIsTrial = result.IsTrial;
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
        _playback.Stop();
        StopPolling();
        NowPlayingText = "已停止";
        PositionText = "00:00 / 00:00";
        ProgressPercent = 0;
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
        NowPlayingText = _playback.State switch
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
    }

    private static string Fmt(TimeSpan t) => $"{(int)t.TotalMinutes:00}:{t.Seconds:00}";
}


