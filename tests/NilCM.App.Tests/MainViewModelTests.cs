using NilCM.App.ViewModels;
using NilCM.Core.Lyrics;

namespace NilCM.App.Tests;

/// <summary>ViewModel 不依赖 Avalonia 运行时（纪律验证）：纯 .NET 环境即可测试。</summary>
public class MainViewModelTests
{
    private sealed class FakeLyricsApi(string? raw) : ILyricsApi
    {
        public Task<string?> GetRawLyricAsync(long trackId, CancellationToken cancellationToken = default)
            => Task.FromResult(raw);
    }

    [Fact]
    public async Task LoadLyricCommand_PopulatesLinesAndStatus()
    {
        var vm = new MainViewModel(new LyricsService(new FakeLyricsApi("[00:01.000]a\n[00:02.500]b")));
        vm.TrackIdText = "42";

        await vm.LoadLyricCommand.ExecuteAsync(null);

        Assert.Equal(2, vm.LyricLines.Count);
        Assert.Contains("已加载 2 行", vm.StatusText);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task LoadLyricCommand_InvalidTrackId_ShowsError()
    {
        var vm = new MainViewModel(new LyricsService(new FakeLyricsApi(null)));
        vm.TrackIdText = "abc";

        await vm.LoadLyricCommand.ExecuteAsync(null);

        Assert.Empty(vm.LyricLines);
        Assert.Contains("无效", vm.StatusText);
    }

    [Fact]
    public async Task LoadLyricCommand_MissingLyric_ShowsNotFound()
    {
        var vm = new MainViewModel(new LyricsService(new FakeLyricsApi(null)));
        vm.TrackIdText = "42";

        await vm.LoadLyricCommand.ExecuteAsync(null);

        Assert.Contains("未找到歌词", vm.StatusText);
    }
}
