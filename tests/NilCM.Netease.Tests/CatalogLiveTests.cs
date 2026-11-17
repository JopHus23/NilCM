using NilCM.Core.Catalog;
using NilCM.Infrastructure.TokenStore;
using NilCM.Netease.Catalog;
using Xunit.Abstractions;

namespace NilCM.Netease.Tests;

/// <summary>实连探针：临时使用，验证目录功能层全链（需本机 tokens.json 有效登录 cookie）。</summary>
public class CatalogLiveTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;
    private readonly HttpClient _http = new(new SocketsHttpHandler { UseCookies = false })
    {
        BaseAddress = new Uri("https://music.163.com/"),
        Timeout = TimeSpan.FromSeconds(15),
    };

    [Fact(Skip = "默认跳过：需要本机 tokens.json 有效登录 cookie。运行方式：移除 Skip → dotnet test --filter CatalogLiveTests")]
    public async Task Catalog_EndToEnd_WithUserCookie()
    {
        var store = new JsonFileTokenStore();
        var catalog = new NeteaseCatalogApi(_http, store);

        var account = await catalog.GetAccountAsync();
        _output.WriteLine($"[account] {account.Nickname} uid={account.UserId} vip={account.IsVip}");
        Assert.True(account.UserId > 0);

        var songs = await catalog.SearchSongsAsync("海阔天空");
        _output.WriteLine($"[search] {songs.Count} 首；第一首={songs[0].Title} artistId={songs[0].ArtistId} albumId={songs[0].AlbumId}");
        Assert.NotEmpty(songs);

        var playlists = await catalog.GetUserPlaylistsAsync();
        _output.WriteLine($"[playlists] 创建={playlists.Created.Count} 收藏={playlists.Favorited.Count}");
        Assert.True(playlists.Created.Count + playlists.Favorited.Count > 0);

        var daily = await catalog.GetDailyRecommendSongsAsync();
        _output.WriteLine($"[daily] {daily.Count} 首；第一首={daily[0].Title}");
        Assert.NotEmpty(daily);

        var artist = await catalog.GetArtistAsync(songs[0].ArtistId);
        _output.WriteLine($"[artist] {artist.Info.Name} 热门歌曲={artist.HotSongs.Count} 专辑={artist.Albums.Count}");
        Assert.NotEmpty(artist.HotSongs);

        var album = await catalog.GetAlbumAsync(songs[0].AlbumId);
        _output.WriteLine($"[album] {album.Name} 曲目={album.Tracks.Count}");
        Assert.NotEmpty(album.Tracks);

        var comments = await catalog.GetSongCommentsAsync(songs[0].Id);
        _output.WriteLine($"[comments] total={comments.Total} 本页={comments.Comments.Count}");
        Assert.True(comments.Total > 0);
    }
}
