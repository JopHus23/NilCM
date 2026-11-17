using NilCM.Core.Playback;

namespace NilCM.Core.Tests;

public class PlaybackServiceTests
{
    private sealed class FakePlayer : IAudioPlayer
    {
        public List<string> LoadedUrls { get; } = [];
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

        public void Load(string url) => LoadedUrls.Add(url);

        public void Play() => Played = true;

        public void Pause() => Paused = true;

        public void Resume() => Resumed = true;

        public void Stop() => Stopped = true;

        public void Seek(TimeSpan position) => SoughtTo = position;

        public void Dispose() { }
    }

    private sealed class FakeResolver(PlayUrlResult? result) : IPlayUrlResolver
    {
        public Task<PlayUrlResult?> ResolveAsync(long trackId, AudioQuality quality, CancellationToken cancellationToken = default)
            => Task.FromResult(result);
    }

    [Fact]
    public async Task PlayAsync_OnResolvedUrl_LoadsAndPlays()
    {
        var player = new FakePlayer();
        var service = new PlaybackService(player, new FakeResolver(new PlayUrlResult(42, "http://example.com/a.mp3", 320000, false)));

        var result = await service.PlayAsync(42, AudioQuality.High);

        Assert.NotNull(result);
        Assert.Equal("http://example.com/a.mp3", player.LoadedUrls.Single());
        Assert.True(player.Played);
    }

    [Fact]
    public async Task PlayAsync_OnResolveFailure_ReturnsNullAndLeavesPlayerUntouched()
    {
        var player = new FakePlayer();
        var service = new PlaybackService(player, new FakeResolver(null));

        var result = await service.PlayAsync(42, AudioQuality.Standard);

        Assert.Null(result);
        Assert.Empty(player.LoadedUrls);
        Assert.False(player.Played);
    }

    [Fact]
    public void TransportControls_PassthroughToPlayer()
    {
        var player = new FakePlayer();
        var service = new PlaybackService(player, new FakeResolver(null));

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
