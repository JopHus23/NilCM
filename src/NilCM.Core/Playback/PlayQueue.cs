using System.IO;

namespace NilCM.Core.Playback;

/// <summary>可播放项：网易曲目（TrackId）或本地文件（LocalPath）二选一。</summary>
public sealed record PlayItem(long? TrackId, string? LocalPath, string Title)
{
    public static PlayItem FromTrack(long trackId, string title) => new(trackId, null, title);

    public static PlayItem FromLocalFile(string path) => new(null, path, Path.GetFileName(path));
}

/// <summary>队列播放模式。随机模式为后续扩展（见 ARCHITECTURE.md §6）。</summary>
public enum QueueMode
{
    Sequential, // 顺序播放：到队尾即停
    RepeatAll,  // 列表循环
    RepeatOne,  // 单曲循环
}

/// <summary>播放队列（纯逻辑，可单测）。游标语义由 PlayQueueTests 钉死。</summary>
public sealed class PlayQueue
{
    private readonly List<PlayItem> _items = [];
    private int _currentIndex = -1;

    public QueueMode Mode { get; set; } = QueueMode.Sequential;

    public IReadOnlyList<PlayItem> Items => _items;

    public int CurrentIndex => _currentIndex;

    public int Count => _items.Count;

    public PlayItem? Current => _currentIndex >= 0 && _currentIndex < _items.Count ? _items[_currentIndex] : null;

    /// <summary>入队。空队列入队时游标指向首项（是否立即播放由编排层决定）。</summary>
    public void Enqueue(PlayItem item)
    {
        _items.Add(item);
        if (_currentIndex < 0)
        {
            _currentIndex = 0;
        }
    }

    public void Clear()
    {
        _items.Clear();
        _currentIndex = -1;
    }

    /// <summary>游标移动到指定位置（越界忽略）。</summary>
    public void SetCurrent(int index)
    {
        if (index >= 0 && index < _items.Count)
        {
            _currentIndex = index;
        }
    }

    /// <summary>
    /// 取下一项并移动游标。Sequential 到队尾返回 null（游标不动）。
    /// userSkip=true 表示用户手动切歌：忽略单曲循环（强制前进，队尾回绕），避免手动切歌被单曲循环卡住。
    /// </summary>
    public PlayItem? Next(bool userSkip = false)
    {
        if (_items.Count == 0)
        {
            return null;
        }

        if (!userSkip && Mode == QueueMode.RepeatOne)
        {
            return Current;
        }

        if (_currentIndex + 1 < _items.Count)
        {
            _currentIndex++;
            return Current;
        }

        if (Mode == QueueMode.RepeatAll || userSkip)
        {
            _currentIndex = 0;
            return Current;
        }

        return null;
    }

    /// <summary>取上一项并移动游标。Sequential 在队首返回当前项（重播语义）。</summary>
    public PlayItem? Previous()
    {
        if (_items.Count == 0)
        {
            return null;
        }

        if (Mode == QueueMode.RepeatOne)
        {
            return Current;
        }

        if (_currentIndex > 0)
        {
            _currentIndex--;
            return Current;
        }

        if (Mode == QueueMode.RepeatAll)
        {
            _currentIndex = _items.Count - 1;
            return Current;
        }

        return Current;
    }
}
