using NilCM.Core.Playback;

namespace NilCM.Core.Tests;

public class PlaybackServiceTests
{
    private sealed class FakePlayer : IAudioPlayer
    {
        public List<string> LoadedItems { get; } = [];
        public bool Played { get; private set; }
        public bool Paused { get; private set; }
        public bool Resumed { get; private set; }
        public bool Stopped { get; private set; }
        public TimeSpan? SoughtTo { get; private set; }
        public float Volume { get; set; }
        public PlaybackState State { get; set; } = PlaybackState.Idle;
        public TimeSpan Position { get; set; }
        public TimeSpan Duration { get; set; }

#pragma warning disable CS0067 // 测试桩不触发事件
        public event EventHandler<PlaybackErrorEventArgs>? PlaybackFailed;
#pragma warning restore CS0067

        public void Load(string url) => LoadedItems.Add(url);

        public void Play() => Played = true;

        public void Pause() => Paused = true;

        public void Resume() => Resumed = true;

        public void Stop() => Stopped = true;

        public void Seek(TimeSpan position) => SoughtTo = position;

        public void Dispose() { }
    }

    private sealed class FakeResolver(bool succeed = true) : IPlayUrlResolver
    {
        public Task<PlayUrlResult?> ResolveAsync(long trackId, AudioQuality quality, CancellationToken cancellationToken = default)
            => Task.FromResult<PlayUrlResult?>(
                succeed ? new PlayUrlResult(trackId, $"http://example.com/{trackId}.mp3", 320000, false) : null);
    }

    private static PlayQueue QueueWith(params long[] trackIds)
    {
        var queue = new PlayQueue();
        foreach (var id in trackIds)
        {
            queue.Enqueue(PlayItem.FromTrack(id, $"track-{id}"));
        }

        return queue;
    }

    [Fact]
    public async Task PlayCurrentAsync_TrackResolved_LoadsUrlAndPlays()
    {
        var player = new FakePlayer();
        var service = new PlaybackService(player, new FakeResolver(), QueueWith(42));

        var started = await service.PlayCurrentAsync();

        Assert.True(started);
        Assert.Equal("http://example.com/42.mp3", player.LoadedItems.Single());
        Assert.True(player.Played);
    }

    [Fact]
    public async Task PlayCurrentAsync_ResolveFails_ReturnsFalseAndLeavesPlayerUntouched()
    {
        var player = new FakePlayer();
        var service = new PlaybackService(player, new FakeResolver(succeed: false), QueueWith(42));

        var started = await service.PlayCurrentAsync();

        Assert.False(started);
        Assert.Empty(player.LoadedItems);
        Assert.False(player.Played);
    }

    [Fact]
    public async Task PlayCurrentAsync_EmptyQueue_ReturnsFalse()
    {
        var service = new PlaybackService(new FakePlayer(), new FakeResolver(), new PlayQueue());

        Assert.False(await service.PlayCurrentAsync());
    }

    [Fact]
    public async Task PlayCurrentAsync_LocalFile_LoadsPathWithoutResolver()
    {
        var player = new FakePlayer();
        var resolver = new FakeResolver(); // 本地文件不应触发解析
        var queue = new PlayQueue();
        queue.Enqueue(PlayItem.FromLocalFile(@"D:\music\a.mp3"));
        var service = new PlaybackService(player, resolver, queue);

        var started = await service.PlayCurrentAsync();

        Assert.True(started);
        Assert.Equal(@"D:\music\a.mp3", player.LoadedItems.Single());
        Assert.True(player.Played);
    }

    [Fact]
    public async Task PlayNextAsync_NavigatesQueue_SequentialTailStops()
    {
        var player = new FakePlayer();
        var queue = QueueWith(1, 2, 3);
        var service = new PlaybackService(player, new FakeResolver(), queue);

        await service.PlayCurrentAsync();
        Assert.Equal("http://example.com/1.mp3", player.LoadedItems[^1]);

        Assert.True(await service.PlayNextAsync());
        Assert.Equal("http://example.com/2.mp3", player.LoadedItems[^1]);

        Assert.True(await service.PlayNextAsync());
        Assert.Equal("http://example.com/3.mp3", player.LoadedItems[^1]);

        Assert.False(await service.PlayNextAsync()); // Sequential 尾
    }

    [Fact]
    public async Task PlayPreviousAsync_GoesBack()
    {
        var player = new FakePlayer();
        var queue = QueueWith(1, 2);
        var service = new PlaybackService(player, new FakeResolver(), queue);

        await service.PlayCurrentAsync();
        await service.PlayNextAsync();
        Assert.Equal("http://example.com/2.mp3", player.LoadedItems[^1]);

        Assert.True(await service.PlayPreviousAsync());
        Assert.Equal("http://example.com/1.mp3", player.LoadedItems[^1]);
    }

    [Fact]
    public void EnqueueTrack_And_EnqueueLocalFile_AppendItems()
    {
        var queue = new PlayQueue();
        var service = new PlaybackService(new FakePlayer(), new FakeResolver(), queue);

        service.EnqueueTrack(7, "歌名");
        service.EnqueueLocalFile(@"C:\m\b.flac");

        Assert.Equal(2, queue.Count);
        Assert.Equal(7, queue.Items[0].TrackId);
        Assert.Equal(@"C:\m\b.flac", queue.Items[1].LocalPath);
    }

    [Fact]
    public void TransportControls_PassthroughToPlayer()
    {
        var player = new FakePlayer();
        var service = new PlaybackService(player, new FakeResolver(), new PlayQueue());

        service.Pause();
        service.Resume();
        service.Stop();
        service.Seek(TimeSpan.FromSeconds(30));
        service.Volume = 0.5f;

        Assert.True(player.Paused);
        Assert.True(player.Resumed);
        Assert.True(player.Stopped);
        Assert.Equal(TimeSpan.FromSeconds(30), player.SoughtTo);
        Assert.Equal(0.5f, player.Volume);
    }
}

