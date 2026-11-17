namespace NilCM.Core.Models;

/// <summary>歌曲（搜索/歌单/专辑/日推等场景的通用目录模型）。</summary>
public sealed record Track(
    long Id,
    string Title,
    string ArtistName,
    long ArtistId,
    string AlbumName,
    long AlbumId,
    TimeSpan Duration,
    bool IsVipOnly);

/// <summary>歌单。</summary>
public sealed record Playlist(
    long Id,
    string Name,
    long CreatorId,
    string CreatorNickname,
    int TrackCount,
    string CoverUrl);

/// <summary>歌单详情：歌单信息 + 曲目列表。</summary>
public sealed record PlaylistDetail(Playlist Info, IReadOnlyList<Track> Tracks);

/// <summary>专辑（含曲目列表）。</summary>
public sealed record Album(
    long Id,
    string Name,
    long ArtistId,
    string ArtistName,
    string CoverUrl,
    string? Description,
    IReadOnlyList<Track> Tracks);

/// <summary>歌手。</summary>
public sealed record Artist(long Id, string Name, string CoverUrl);

/// <summary>歌手主页：歌手 + 热门歌曲 + 专辑列表。</summary>
public sealed record ArtistPage(Artist Info, IReadOnlyList<Track> HotSongs, IReadOnlyList<Album> Albums);

/// <summary>当前登录账号摘要。</summary>
public sealed record AccountInfo(long UserId, string Nickname, bool IsVip);

/// <summary>用户歌单分组：自己创建的 + 收藏的。</summary>
public sealed record UserPlaylists(IReadOnlyList<Playlist> Created, IReadOnlyList<Playlist> Favorited);

/// <summary>一条歌曲评论。</summary>
public sealed record Comment(long Id, string Nickname, string Content, DateTimeOffset Time, int LikedCount);

/// <summary>评论分页结果。</summary>
public sealed record CommentPage(long Total, IReadOnlyList<Comment> Comments);
