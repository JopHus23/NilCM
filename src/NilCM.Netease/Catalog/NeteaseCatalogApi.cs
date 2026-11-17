using System.Text.Json;
using NilCM.Core.Auth;
using NilCM.Core.Catalog;
using NilCM.Core.Models;

namespace NilCM.Netease.Catalog;

/// <summary>网易目录类数据源（搜索/歌单/专辑/歌手/日推；weapi + 完整登录 cookie）。DTO 不出程序集。</summary>
public sealed partial class NeteaseCatalogApi(HttpClient httpClient, ITokenStore tokenStore) : ICatalogApi, IUserApi, ICommentApi
{
    public async Task<IReadOnlyList<Track>> SearchSongsAsync(string keyword, CancellationToken cancellationToken = default)
    {
        var resp = await Post("/weapi/cloudsearch/get/web", new { s = keyword, type = 1, limit = 30, offset = 0 }, cancellationToken);
        using var doc = JsonDocument.Parse(resp.Body);
        return NeteaseJson.MapTracks(NeteaseJson.Arr(NeteaseJson.Obj(doc.RootElement, "result"), "songs"));
    }

    public async Task<IReadOnlyList<Track>> GetDailyRecommendSongsAsync(CancellationToken cancellationToken = default)
    {
        var resp = await Post("/weapi/v3/discovery/recommend/songs", new { }, cancellationToken);
        using var doc = JsonDocument.Parse(resp.Body);
        var data = NeteaseJson.Obj(doc.RootElement, "data");
        return NeteaseJson.MapTracks(NeteaseJson.Arr(data, "dailySongs", "orderSongs"));
    }

    public async Task<PlaylistDetail> GetPlaylistDetailAsync(long playlistId, CancellationToken cancellationToken = default)
    {
        var resp = await Post("/weapi/playlist/detail", new { id = playlistId }, cancellationToken);
        return ParsePlaylistDetail(resp.Body);
    }

    public async Task<Album> GetAlbumAsync(long albumId, CancellationToken cancellationToken = default)
    {
        var resp = await Post($"/weapi/album/{albumId}", new { }, cancellationToken);
        using var doc = JsonDocument.Parse(resp.Body);
        var album = NeteaseJson.Obj(doc.RootElement, "album");
        var artist = NeteaseJson.Obj(album, "artist");
        return new Album(
            NeteaseJson.Num(album, "id"),
            NeteaseJson.Str(album, "name") ?? string.Empty,
            NeteaseJson.Num(artist, "id"),
            NeteaseJson.Str(artist, "name") ?? string.Empty,
            NeteaseJson.Str(album, "picUrl") ?? string.Empty,
            NeteaseJson.Str(album, "description"),
            NeteaseJson.MapTracks(NeteaseJson.Arr(album, "songs")));
    }

    public async Task<ArtistPage> GetArtistAsync(long artistId, CancellationToken cancellationToken = default)
    {
        var songsResp = await Post($"/weapi/artist/{artistId}", new { }, cancellationToken);
        var albumsResp = await Post($"/weapi/artist/albums/{artistId}", new { limit = 50, offset = 0 }, cancellationToken);

        using var songsDoc = JsonDocument.Parse(songsResp.Body);
        using var albumsDoc = JsonDocument.Parse(albumsResp.Body);
        var artist = NeteaseJson.Obj(songsDoc.RootElement, "artist");
        var info = new Artist(
            NeteaseJson.Num(artist, "id"),
            NeteaseJson.Str(artist, "name") ?? string.Empty,
            NeteaseJson.Str(artist, "img1Url", "picUrl") ?? string.Empty);

        var albums = new List<Album>();
        foreach (var a in NeteaseJson.Arr(albumsDoc.RootElement, "hotAlbums").EnumerateArray())
        {
            albums.Add(new Album(
                NeteaseJson.Num(a, "id"),
                NeteaseJson.Str(a, "name") ?? string.Empty,
                artistId,
                info.Name,
                NeteaseJson.Str(a, "picUrl") ?? string.Empty,
                null,
                []));
        }

        return new ArtistPage(info, NeteaseJson.MapTracks(NeteaseJson.Arr(songsDoc.RootElement, "hotSongs")), albums);
    }

    private async Task<(string Body, string? SetCookie)> Post(string path, object payload, CancellationToken ct)
    {
        var cookie = await tokenStore.GetCookieAsync(ct);
        var resp = await WeapiHttpClient.PostAsync(httpClient, path, payload, cookie, ct);
        return (resp.Body, resp.SetCookie);
    }

    internal static IReadOnlyList<Track> MapSongs(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return NeteaseJson.MapTracks(NeteaseJson.Arr(NeteaseJson.Obj(doc.RootElement, "result"), "songs"));
    }
}
