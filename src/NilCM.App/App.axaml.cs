using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using NilCM.App.ViewModels;
using NilCM.App.Views;
using NilCM.Core.Auth;
using NilCM.Core.Lyrics;
using NilCM.Core.Playback;
using NilCM.Infrastructure.TokenStore;
using NilCM.Netease.Auth;
using NilCM.Netease.Lyrics;
using NilCM.Netease.Playback;
using NilCM.Playback.Engine;

namespace NilCM.App;

public partial class App : Application
{
    /// <summary>组合根：唯一知道所有具体实现的地方（见 ARCHITECTURE.md §1 依赖规则 3）。</summary>
    public static IServiceProvider Services { get; } = BuildServices();

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainViewModel>(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static IServiceProvider BuildServices()
    {
        var services = new ServiceCollection();

        // 数据源：网易（易腐囚笼）。接口失效时只改 NilCM.Netease 内部，Core/App 零改动。
        services.AddSingleton(_ =>
        {
            var http = new HttpClient
            {
                BaseAddress = new Uri("https://music.163.com/"),
                Timeout = TimeSpan.FromSeconds(15),
            };
            http.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            http.DefaultRequestHeaders.Referrer = new Uri("https://music.163.com/");
            return http;
        });
        services.AddSingleton<ILyricsApi, NeteaseLyricsApi>();

        // 认证 / 凭据：匿名 cookie（weapi）+ JSON 文件持久化
        services.AddSingleton<IAuthApi, NeteaseAuthApi>();
        services.AddSingleton<ITokenStore, JsonFileTokenStore>();

        // 播放：地址解析（weapi）+ LibVLC 引擎 + 编排
        services.AddSingleton<IPlayUrlResolver, NeteasePlayUrlApi>();
        services.AddSingleton<IAudioPlayer, LibVlcAudioPlayer>();
        services.AddSingleton<PlaybackService>();

        // Core 编排（纯逻辑）
        services.AddSingleton<LyricsService>();

        // ViewModel（不引用任何 Avalonia 类型——纪律见 ARCHITECTURE.md §2）
        services.AddSingleton<MainViewModel>();

        return services.BuildServiceProvider();
    }
}
