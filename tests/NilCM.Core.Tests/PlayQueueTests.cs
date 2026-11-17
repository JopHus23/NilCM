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

    [Fact]
    public void RandomMode_FullPass_VisitsEveryItemExactlyOnce()
    {
        var queue = new PlayQueue(new Random(1234)) { Mode = QueueMode.Random };
        for (long i = 1; i <= 6; i++)
        {
            queue.Enqueue(Track((int)i));
        }

        var visited = new List<long>();
        for (var i = 0; i < 6; i++)
        {
            var item = queue.Next();
            Assert.NotNull(item);
            visited.Add(item!.TrackId!.Value);
        }

        // 全表随机：一轮内不重复、不遗漏（对比官方客户端“最近 N 首”的截断式随机）
        Assert.Equal(6, visited.Distinct().Count());
        Assert.Equal(new HashSet<long> { 1, 2, 3, 4, 5, 6 }, visited.ToHashSet());
    }

    [Fact]
    public void RandomMode_Previous_WalksBackSameShuffleOrder()
    {
        var queue = new PlayQueue(new Random(1234)) { Mode = QueueMode.Random };
        for (long i = 1; i <= 6; i++)
        {
            queue.Enqueue(Track((int)i));
        }

        var first = queue.Next()!;   // 重建锚定：新序排头
        var second = queue.Next()!;  // 洗牌序第二项
        Assert.NotEqual(first.TrackId, second.TrackId);

        // 上一首沿洗牌序回走：回到排头
        var back = queue.Previous()!;
        Assert.Equal(first.TrackId, back.TrackId);
    }

    [Fact]
    public void RandomMode_ReshufflesAfterFullPass_AndStillCoversAll()
    {
        var queue = new PlayQueue(new Random(99)) { Mode = QueueMode.Random };
        for (long i = 1; i <= 5; i++)
        {
            queue.Enqueue(Track((int)i));
        }

        var pass1 = new List<long>();
        for (var i = 0; i < 5; i++)
        {
            pass1.Add(queue.Next()!.TrackId!.Value);
        }

        // 整轮播完自动重洗，新一轮仍覆盖全部曲目
        var pass2 = new List<long>();
        for (var i = 0; i < 5; i++)
        {
            pass2.Add(queue.Next()!.TrackId!.Value);
        }

        Assert.Equal(5, pass1.Distinct().Count());
        Assert.Equal(5, pass2.Distinct().Count());
        Assert.Equal(new HashSet<long> { 1, 2, 3, 4, 5 }, pass2.ToHashSet());
    }

    [Fact]
    public void RandomMode_EnqueueDuringRandom_NewItemEntersRotation()
    {
        var queue = new PlayQueue(new Random(7)) { Mode = QueueMode.Random };
        for (long i = 1; i <= 3; i++)
        {
            queue.Enqueue(Track((int)i));
        }

        var pass = new List<long>();
        for (var i = 0; i < 3; i++)
        {
            pass.Add(queue.Next()!.TrackId!.Value);
        }

        queue.Enqueue(Track(4)); // 随机轮换中途入队

        // 新曲目最终会进入轮换：最多扫描两轮内必能遇到
        var seen = new HashSet<long>();
        var found = false;
        for (var i = 0; i < 16 && !found; i++)
        {
            var trackId = queue.Next()!.TrackId!.Value;
            seen.Add(trackId);
            found = trackId == 4;
        }

        Assert.True(found, $"扫描 16 次未遇到新入队的曲目 4：seen=[{string.Join(",", seen)}]");
    }
}
