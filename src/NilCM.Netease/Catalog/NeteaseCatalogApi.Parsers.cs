using System.Text.Json;
using NilCM.Core.Catalog;
using NilCM.Core.Models;

namespace NilCM.Netease.Catalog;

/// <summary>目录/用户/评论响应的解析器（internal static，供契约测试复用）。</summary>
public sealed partial class NeteaseCatalogApi
{
    internal static PlaylistDetail ParsePlaylistDetail(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var playlist = NeteaseJson.Obj(doc.RootElement, "playlist");
        var creator = NeteaseJson.Obj(playlist, "creator");
        var info = new Playlist(
            NeteaseJson.Num(playlist, "id"),
            NeteaseJson.Str(playlist, "name") ?? string.Empty,
            NeteaseJson.Num(creator, "userId"),
            NeteaseJson.Str(creator, "nickname") ?? string.Empty,
            (int)NeteaseJson.Num(playlist, "trackCount"),
            NeteaseJson.Str(playlist, "coverImgUrl") ?? string.Empty);
        return new PlaylistDetail(info, NeteaseJson.MapTracks(NeteaseJson.Arr(playlist, "tracks")));
    }

    internal static UserPlaylists ParseUserPlaylists(string json, long currentUserId)
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

    internal static AccountInfo ParseAccount(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var account = NeteaseJson.Obj(doc.RootElement, "account");
        var profile = NeteaseJson.Obj(doc.RootElement, "profile");
        return new AccountInfo(
            NeteaseJson.Num(account, "id"),
            NeteaseJson.Str(profile, "nickname") ?? string.Empty,
            NeteaseJson.Num(account, "vipType") > 0);
    }

    internal static CommentPage ParseComments(string json)
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
