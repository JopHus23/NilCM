using System.Text.Json;
using NilCM.Core.Auth;
using NilCM.Core.Catalog;
using NilCM.Core.Models;

namespace NilCM.Netease.Catalog;

/// <summary>用户数据与歌曲评论（IUserApi / ICommentApi）。</summary>
public sealed partial class NeteaseCatalogApi
{
    public async Task<AccountInfo> GetAccountAsync(CancellationToken cancellationToken = default)
    {
        var cookie = await tokenStore.GetCookieAsync(cancellationToken);
        var resp = await WeapiHttpClient.GetAsync(httpClient, "/api/nuser/account/get", cookie, cancellationToken);
        return MapAccount(resp.Body);
    }

    public async Task<UserPlaylists> GetUserPlaylistsAsync(CancellationToken cancellationToken = default)
    {
        var me = await GetAccountAsync(cancellationToken);
        var cookie = await tokenStore.GetCookieAsync(cancellationToken);
        var resp = await WeapiHttpClient.PostAsync(
            httpClient,
            "/weapi/user/playlist",
            new { uid = me.UserId, limit = 1000, offset = 0, includeVideo = true },
            cookie,
            cancellationToken);
        return MapUserPlaylists(resp.Body, me.UserId);
    }

    public async Task<CommentPage> GetSongCommentsAsync(long songId, int page = 1, int limit = 20, CancellationToken cancellationToken = default)
    {
        var cookie = await tokenStore.GetCookieAsync(cancellationToken);
        var resp = await WeapiHttpClient.PostAsync(
            httpClient,
            $"/weapi/v1/resource/comments/R_SO_4_{songId}",
            new { limit, offset = (page - 1) * limit },
            cookie,
            cancellationToken);
        return MapComments(resp.Body);
    }

    internal static AccountInfo MapAccount(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var account = NeteaseJson.Obj(doc.RootElement, "account");
        var profile = NeteaseJson.Obj(doc.RootElement, "profile");
        return new AccountInfo(
            NeteaseJson.Num(account, "id"),
            NeteaseJson.Str(profile, "nickname") ?? string.Empty,
            NeteaseJson.Num(account, "vipType") > 0);
    }

    internal static UserPlaylists MapUserPlaylists(string json, long currentUserId)
    {
        using var doc = JsonDocument.Parse(json);
        var created = new List<Playlist>();
        var favorited = new List<Playlist>();
        foreach (var p in NeteaseJson.Arr(doc.RootElement, "playlist").EnumerateArray())
        {
            var creator = NeteaseJson.Obj(p, "creator");
            var playlist = new Playlist(
                NeteaseJson.Num(p, "id"),
                NeteaseJson.Str(p, "name") ?? string.Empty,
                NeteaseJson.Num(creator, "userId"),
                NeteaseJson.Str(creator, "nickname") ?? string.Empty,
                (int)NeteaseJson.Num(p, "trackCount"),
                NeteaseJson.Str(p, "coverImgUrl") ?? string.Empty);

            if (playlist.CreatorId == currentUserId)
            {
                created.Add(playlist);
            }
            else
            {
                favorited.Add(playlist);
            }
        }

        return new UserPlaylists(created, favorited);
    }

    internal static CommentPage MapComments(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var comments = new List<Comment>();
        foreach (var c in NeteaseJson.Arr(doc.RootElement, "comments").EnumerateArray())
        {
            var user = NeteaseJson.Obj(c, "user");
            comments.Add(new Comment(
                NeteaseJson.Num(c, "commentId"),
                NeteaseJson.Str(user, "nickname") ?? string.Empty,
                NeteaseJson.Str(c, "content") ?? string.Empty,
                DateTimeOffset.FromUnixTimeMilliseconds(NeteaseJson.Num(c, "time")),
                (int)NeteaseJson.Num(c, "likedCount")));
        }

        return new CommentPage(NeteaseJson.Num(doc.RootElement, "total"), comments);
    }
}
