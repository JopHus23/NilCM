using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NilCM.Core.Lyrics;

namespace NilCM.App.ViewModels;

/// <summary>主窗口 ViewModel：只依赖 Core 的服务，不出现任何 Avalonia 类型（纪律见 ARCHITECTURE.md §2）。</summary>
public partial class MainViewModel(LyricsService lyrics) : ViewModelBase
{
    private readonly LyricsService _lyrics = lyrics;

    [ObservableProperty]
    private string _trackIdText = "186016";

    [ObservableProperty]
    private string _statusText = "输入歌曲 ID，点击「获取歌词」。";

    [ObservableProperty]
    private bool _isBusy;

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
}

