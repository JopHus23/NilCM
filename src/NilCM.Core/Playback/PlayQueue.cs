using System.IO;

namespace NilCM.Core.Playback;

/// <summary>可播放项：网易曲目（TrackId）或本地文件（LocalPath）二选一。</summary>
public sealed record PlayItem(long? TrackId, string? LocalPath, string Title)
{
    public static PlayItem FromTrack(long trackId, string title) => new(trackId, null, title);

    public static PlayItem FromLocalFile(string path) => new(null, path, Path.GetFileName(path));
}

/// <summary>队列播放模式。Random = 整表全随机（Fisher-Yates 全洗牌，无“最近 N 首”截断）。</summary>
public enum QueueMode
{
    Sequential, // 顺序播放：到队尾即停
    RepeatAll,  // 列表循环
    RepeatOne,  // 单曲循环
    Random,     // 随机播放：整表洗牌，一轮播完自动重洗
}

/// <summary>
/// 播放队列（纯逻辑，可单测）。游标语义由 PlayQueueTests 钉死；
/// 随机模式对整个列表洗牌（构造函数可注入 Random 以便确定性测试）。
/// </summary>
public sealed class PlayQueue
{
    private readonly List<PlayItem> _items = [];
    private readonly Random _random;
    private int _currentIndex = -1;
    private List<int>? _randomOrder;
    private int _randomPos;

    public PlayQueue(Random? random = null) => _random = random ?? new Random();

    private QueueMode _mode = QueueMode.Sequential;

    public QueueMode Mode
    {
        get => _mode;
        set
        {
            _mode = value;
            if (_mode == QueueMode.Random)
            {
                EnsureRandomOrder();
            }
            else
            {
                _randomOrder = null;
                _randomPos = 0;
            }
        }
    }

    public IReadOnlyList<PlayItem> Items => _items;

    public int CurrentIndex => _currentIndex;

    public int Count => _items.Count;

    public PlayItem? Current => _currentIndex >= 0 && _currentIndex < _items.Count ? _items[_currentIndex] : null;

    /// <summary>入队。空队列入队时游标指向首项（是否立即播放由编排层决定）；随机模式下新曲目洗进本轮轮换表。</summary>
    public void Enqueue(PlayItem item)
    {
        _items.Add(item);
        if (_randomOrder != null && _randomOrder.Count == _items.Count - 1)
        {
            _randomOrder.Add(_items.Count - 1); // 新曲目追加进本轮随机序
        }

        if (_currentIndex < 0)
        {
            _currentIndex = 0;
        }
    }

    public void Clear()
    {
        _items.Clear();
        _currentIndex = -1;
        _randomOrder = null;
        _randomPos = 0;
    }

    /// <summary>游标移动到指定位置（越界忽略）。随机模式下以该项为锚重排随机序。</summary>
    public void SetCurrent(int index)
    {
        if (index < 0 || index >= _items.Count)
        {
            return;
        }

        _currentIndex = index;
        if (_mode == QueueMode.Random)
        {
            RebuildRandomOrder(anchorCurrent: true);
            _randomPos = 0;
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

        if (Mode == QueueMode.Random)
        {
            return RandomNext();
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

        if (Mode == QueueMode.Random)
        {
            return RandomPrevious();
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

    private PlayItem? RandomNext()
    {
        var rebuilt = EnsureRandomOrder();
        if (_randomOrder == null || _randomOrder.Count == 0)
        {
            return null;
        }

        if (rebuilt)
        {
            return Current; // 重建时已把游标放到新序排头（锚定当前或新洗牌首曲），本次直接返回它
        }

        if (_randomPos + 1 < _randomOrder.Count)
        {
            _randomPos++;
            _currentIndex = _randomOrder[_randomPos];
            return Current;
        }

        // 整轮播完：全表重洗（不锚定当前，避免刚播过的再次排头）
        RebuildRandomOrder(anchorCurrent: false);
        if (_randomOrder.Count == 0)
        {
            return null;
        }

        _randomPos = 0;
        _currentIndex = _randomOrder[0];
        return Current;
    }

    private PlayItem? RandomPrevious()
    {
        _ = EnsureRandomOrder();
        if (_randomOrder == null || _randomOrder.Count == 0 || _randomPos <= 0)
        {
            return Current; // 已在随机序开头：停留重播
        }

        _randomPos--;
        _currentIndex = _randomOrder[_randomPos];
        return Current;
    }

    /// <summary>确保随机序就绪。返回 true = 刚重建（游标已定位到新序排头，调用方应直接返回当前项）。</summary>
    private bool EnsureRandomOrder()
    {
        if (_items.Count == 0)
        {
            _randomOrder = null;
            _randomPos = 0;
            return false;
        }

        // 顺序表有效（长度匹配且索引在界内）则沿用
        if (_randomOrder != null && _randomOrder.Count == _items.Count && _randomOrder.All(i => i >= 0 && i < _items.Count))
        {
            return false;
        }

        RebuildRandomOrder(anchorCurrent: _currentIndex >= 0);
        if (_currentIndex >= 0 && _randomOrder != null && _randomOrder.Count > 0)
        {
            _currentIndex = _randomOrder[0];
            _randomPos = 0;
            return true;
        }

        return false;
    }

    /// <summary>Fisher-Yates 全表洗牌。anchorCurrent=true 时保持当前曲目排头（切模式/入队时不中断正在播的曲目）。</summary>
    private void RebuildRandomOrder(bool anchorCurrent)
    {
        var indices = new List<int>(Enumerable.Range(0, _items.Count));
        for (var i = indices.Count - 1; i > 0; i--)
        {
            var j = _random.Next(i + 1);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }

        if (anchorCurrent && _currentIndex >= 0 && _currentIndex < indices.Count)
        {
            indices.Remove(_currentIndex);
            indices.Insert(0, _currentIndex);
        }

        _randomOrder = indices;
        _randomPos = 0;
    }
}
