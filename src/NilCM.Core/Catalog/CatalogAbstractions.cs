using NilCM.Core.Models;

namespace NilCM.Core.Catalog;

/// <summary>目录/内容类数据源抽象（搜索、歌单、专辑、歌手、日推）。实现位于 NilCM.Netease。</summary>
public interface ICatalogApi
{
    /// <summary>歌曲搜索（关键词）。</summary>
    Task<IReadOnlyList<Track>> SearchSongsAsync(string keyword, CancellationToken cancellationToken = default);

    /// <summary>歌单详情（含曲目列表）。</summary>
    Task<PlaylistDetail> GetPlaylistDetailAsync(long playlistId, CancellationToken cancellationToken = default);

    /// <summary>专辑详情（含曲目列表）。</summary>
    Task<Album> GetAlbumAsync(long albumId, CancellationToken cancellationToken = default);

    /// <summary>歌手主页（歌手 + 热门歌曲 + 专辑列表）。</summary>
    Task<ArtistPage> GetArtistAsync(long artistId, CancellationToken cancellationToken = default);

    /// <summary>每日推荐歌曲（需登录 cookie）。</summary>
    Task<IReadOnlyList<Track>> GetDailyRecommendSongsAsync(CancellationToken cancellationToken = default);
}

/// <summary>用户数据抽象（当前账号、歌单分组）。实现位于 NilCM.Netease。</summary>
public interface IUserApi
{
    /// <summary>当前登录账号摘要（含 uid，用于区分创建/收藏歌单）。</summary>
    Task<AccountInfo> GetAccountAsync(CancellationToken cancellationToken = default);

    /// <summary>当前用户的歌单分组：自己创建的 + 收藏的（需已登录）。</summary>
    Task<UserPlaylists> GetUserPlaylistsAsync(CancellationToken cancellationToken = default);
}

/// <summary>歌曲评论抽象。实现位于 NilCM.Netease。</summary>
public interface ICommentApi
{
    /// <summary>歌曲评论（分页，page 从 1 开始）。</summary>
    Task<CommentPage> GetSongCommentsAsync(long songId, int page = 1, int limit = 20, CancellationToken cancellationToken = default);
}
