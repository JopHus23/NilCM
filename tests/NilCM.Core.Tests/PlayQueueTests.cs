using NilCM.Core.Playback;

namespace NilCM.Core.Tests;

public class PlayQueueTests
{
    private static PlayItem Track(long id) => PlayItem.FromTrack(id, $"track-{id}");

    private static PlayItem Local(string path) => PlayItem.FromLocalFile(path);

    [Fact]
    public void EmptyQueue_NextAndPrevious_ReturnNull()
    {
        var queue = new PlayQueue();

        Assert.Null(queue.Next());
        Assert.Null(queue.Previous());
        Assert.Null(queue.Current);
        Assert.Equal(0, queue.Count);
    }

    [Fact]
    public void Enqueue_OnEmptyQueue_SetsCurrentToFirst()
    {
        var queue = new PlayQueue();
        queue.Enqueue(Track(1));

        Assert.Equal(Track(1), queue.Current);
        Assert.Equal(0, queue.CurrentIndex);
    }

    [Fact]
    public void Sequential_AtTail_NextReturnsNull_KeepsCursor()
    {
        var queue = new PlayQueue { Mode = QueueMode.Sequential };
        queue.Enqueue(Track(1));
        queue.Enqueue(Track(2));
        queue.Enqueue(Track(3));

        Assert.Equal(Track(2), queue.Next());
        Assert.Equal(Track(3), queue.Next());
        Assert.Null(queue.Next());
        Assert.Equal(Track(3), queue.Current); // 游标不动
    }

    [Fact]
    public void Sequential_AtHead_PreviousReturnsCurrent()
    {
        var queue = new PlayQueue { Mode = QueueMode.Sequential };
        queue.Enqueue(Track(1));
        queue.Enqueue(Track(2));

        Assert.Equal(Track(1), queue.Previous()); // 队首重播语义
        Assert.Equal(0, queue.CurrentIndex);
    }

    [Fact]
    public void RepeatAll_WrapsBothDirections()
    {
        var queue = new PlayQueue { Mode = QueueMode.RepeatAll };
        queue.Enqueue(Track(1));
        queue.Enqueue(Track(2));

        queue.Next(); // -> 2
        var wrapped = queue.Next();
        Assert.Equal(Track(1), wrapped); // 回绕到 1

        var wrappedBack = queue.Previous();
        Assert.Equal(Track(2), wrappedBack); // 回绕到 2
    }

    [Fact]
    public void RepeatOne_AutoAdvance_ReplaysCurrent_But_UserSkipAdvances()
    {
        var queue = new PlayQueue { Mode = QueueMode.RepeatOne };
        queue.Enqueue(Track(1));
        queue.Enqueue(Track(2));
        queue.Next(userSkip: true); // 用户切到第 2 首（RepeatOne 下自动连播不会前进，需 userSkip）

        // 自动连播：单曲循环重播当前
        Assert.Equal(Track(2), queue.Next());
        Assert.Equal(1, queue.CurrentIndex);
        Assert.Equal(Track(2), queue.Previous());

        // 用户手动切歌：强制前进并回绕
        Assert.Equal(Track(1), queue.Next(userSkip: true));
        Assert.Equal(0, queue.CurrentIndex);
    }

    [Fact]
    public void SetCurrent_MovesCursor_IgnoresOutOfRange()
    {
        var queue = new PlayQueue();
        queue.Enqueue(Track(1));
        queue.Enqueue(Track(2));

        queue.SetCurrent(1);
        Assert.Equal(Track(2), queue.Current);

        queue.SetCurrent(5); // 越界忽略
        Assert.Equal(Track(2), queue.Current);
    }

    [Fact]
    public void Clear_ResetsEverything()
    {
        var queue = new PlayQueue();
        queue.Enqueue(Track(1));
        queue.Clear();

        Assert.Equal(0, queue.Count);
        Assert.Null(queue.Current);
        Assert.Null(queue.Next());
    }

    [Fact]
    public void LocalFileItem_PreservesPath()
    {
        var queue = new PlayQueue();
        queue.Enqueue(Local(@"D:\music\a.mp3"));

        Assert.Equal(@"D:\music\a.mp3", queue.Current!.LocalPath);
        Assert.Null(queue.Current.TrackId);
        Assert.Equal("a.mp3", queue.Current.Title); // 文件名作为标题
    }
}
