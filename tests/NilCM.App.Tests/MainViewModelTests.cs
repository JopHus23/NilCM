using NilCM.App.ViewModels;
using NilCM.Core.Lyrics;
using NilCM.Core.Playback;

namespace NilCM.App.Tests;

/// <summary>ViewModel 不依赖 Avalonia 运行时（纪律验证）：纯 .NET 环境即可测试。</summary>
public class MainViewModelTests
{
    private sealed class FakeLyricsApi(string? raw) : ILyricsApi
    {
        public Task<string?> GetRawLyricAsync(long trackId, CancellationToken cancellationToken = default)
            => Task.FromResult(raw);
    }

    private sealed class FakePlayer : IAudioPlayer
    {
        public PlaybackState State { get; set; } = PlaybackState.Playing;
        public TimeSpan Position { get; set; }
        public TimeSpan Duration { get; set; }
        public float Volume { get; set; }

#pragma warning disable CS0067 // 测试桩不触发事件
        public event EventHandler<PlaybackErrorEventArgs>? PlaybackFailed;
#pragma warning restore CS0067

        public void Load(string url) { }

        public void Play() { }

        public void Pause() { }

        public void Resume() { }

        public void Stop() { }

        public void Seek(TimeSpan position) { }

        public void Dispose() { }
    }

    private sealed class FakeResolver : IPlayUrlResolver
    {
        public Task<PlayUrlResult?> ResolveAsync(long trackId, AudioQuality quality, CancellationToken cancellationToken = default)
            => Task.FromResult<PlayUrlResult?>(new PlayUrlResult(trackId, "http://example.com/a.mp3", 320000, false));
    }

    private static MainViewModel CreateVm(string? lyricRaw = "[00:01.000]a\n[00:02.500]b")
        => new(
            new LyricsService(new FakeLyricsApi(lyricRaw)),
            new PlaybackService(new FakePlayer(), new FakeResolver(), new PlayQueue()));

    [Fact]
    public async Task LoadLyricCommand_PopulatesLinesAndStatus()
    {
        var vm = CreateVm();
        vm.TrackIdText = "42";

        await vm.LoadLyricCommand.ExecuteAsync(null);

        Assert.Equal(2, vm.LyricLines.Count);
        Assert.Contains("已加载 2 行", vm.StatusText);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task LoadLyricCommand_MissingLyric_ShowsNotFound()
    {
        var vm = CreateVm(lyricRaw: null);
        vm.TrackIdText = "42";

        await vm.LoadLyricCommand.ExecuteAsync(null);

        Assert.Contains("未找到歌词", vm.StatusText);
    }

    [Fact]
    public async Task PlayCommand_WithResolvableTrack_ShowsPlaying()
    {
        var vm = CreateVm();
        vm.TrackIdText = "42";

        await vm.PlayCommand.ExecuteAsync(null);

        Assert.Contains("已开始播放", vm.StatusText);
        Assert.Equal("正在播放", vm.NowPlayingText);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task PlayCommand_InvalidTrackId_ShowsError()
    {
        var vm = CreateVm();
        vm.TrackIdText = "abc";

        await vm.PlayCommand.ExecuteAsync(null);

        Assert.Contains("无效", vm.StatusText);
        Assert.Equal("未在播放", vm.NowPlayingText);
    }
}

