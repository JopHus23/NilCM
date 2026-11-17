using NilCM.Core.Catalog;
using NilCM.Netease.Catalog;

namespace NilCM.Netease.Tests;

/// <summary>契约测试：目录类响应 fixture → Core 模型（不真连网）。</summary>
public class CatalogMappingTests
{
    private const string SearchFixture =
        """{"result":{"songs":[{"id":347230,"name":"海阔天空","ar":[{"id":777,"name":"Beyond"}],"al":{"id":99,"name":"经典"},"dt":326000,"fee":0}],"songCount":1}}""";

    private const string PlaylistDetailFixture =
        """{"playlist":{"id":5,"name":"我的歌单","trackCount":2,"coverImgUrl":"http://p/5.jpg","creator":{"userId":3250060921,"nickname":"云枫Mapher"},"tracks":[{"id":1,"name":"歌一","ar":[{"id":8,"name":"歌手甲"}],"al":{"id":9,"name":"专辑乙"},"dt":200000,"fee":1},{"id":2,"name":"歌二","artists":[{"name":"歌手乙"}],"album":{"name":"老格式专辑"},"duration":180000,"fee":0}]}}""";

    private const string UserPlaylistsFixture =
        """{"playlist":[{"id":10,"name":"我创建的","trackCount":3,"coverImgUrl":"","creator":{"userId":3250060921,"nickname":"云枫Mapher"}},{"id":11,"name":"我收藏的","trackCount":9,"coverImgUrl":"","creator":{"userId":42,"nickname":"别人"}}]}""";

    private const string AccountFixture =
        """{"code":200,"account":{"id":3250060921,"vipType":11},"profile":{"nickname":"云枫Mapher"}}""";

    private const string CommentsFixture =
        """{"total":2,"comments":[{"commentId":1,"content":"好歌！","likedCount":10,"time":1700000000000,"user":{"nickname":"听众A"}},{"commentId":2,"content":"前排","likedCount":1,"time":1700000001000,"user":{"nickname":"听众B"}}]}""";

    [Fact]
    public void MapSongs_SearchFixture_MapsTolerantFields()
    {
        var songs = NeteaseCatalogApi.MapSongs(SearchFixture);

        var song = Assert.Single(songs);
        Assert.Equal(347230, song.Id);
        Assert.Equal("海阔天空", song.Title);
        Assert.Equal("Beyond", song.ArtistName);
        Assert.Equal(777, song.ArtistId);
        Assert.Equal("经典", song.AlbumName);
        Assert.Equal(TimeSpan.FromMilliseconds(326000), song.Duration);
        Assert.False(song.IsVipOnly); // fee=0 免费歌
    }

    [Fact]
    public void MapSongs_VipFee_MarksVipOnly()
    {
        var songs = NeteaseCatalogApi.MapSongs(
            """{"result":{"songs":[{"id":9,"name":"VIP歌","ar":[{"name":"A"}],"al":{"name":"B"},"dt":1000,"fee":1}]}}""");

        Assert.True(songs[0].IsVipOnly);
    }

    [Fact]
    public void MapPlaylistDetail_MapsInfoAndTolerantTracks()
    {
        var detail = NeteaseCatalogApi.ParsePlaylistDetail(PlaylistDetailFixture);

        Assert.Equal("我的歌单", detail.Info.Name);
        Assert.Equal(3250060921, detail.Info.CreatorId);
        Assert.Equal(2, detail.Tracks.Count);
        Assert.Equal("歌一", detail.Tracks[0].Title);      // 新形态 ar/al/dt
        Assert.True(detail.Tracks[0].IsVipOnly);            // fee=1 → VIP 标记
        Assert.Equal("歌手乙", detail.Tracks[1].ArtistName); // 旧形态 artists/album/duration
        Assert.Equal(TimeSpan.FromSeconds(180), detail.Tracks[1].Duration);
    }

    [Fact]
    public void MapUserPlaylists_SplitsCreatedAndFavorited()
    {
        var playlists = NeteaseCatalogApi.ParseUserPlaylists(UserPlaylistsFixture, currentUserId: 3250060921);

        var created = Assert.Single(playlists.Created);
        Assert.Equal("我创建的", created.Name);
        var favorited = Assert.Single(playlists.Favorited);
        Assert.Equal("我收藏的", favorited.Name);
    }

    [Fact]
    public void MapAccount_MapsNicknameAndVip()
    {
        var account = NeteaseCatalogApi.ParseAccount(AccountFixture);

        Assert.Equal(3250060921, account.UserId);
        Assert.Equal("云枫Mapher", account.Nickname);
        Assert.True(account.IsVip);
    }

    [Fact]
    public void MapComments_MapsTotalAndContent()
    {
        var page = NeteaseCatalogApi.ParseComments(CommentsFixture);

        Assert.Equal(2, page.Total);
        Assert.Equal(2, page.Comments.Count);
        Assert.Equal("好歌！", page.Comments[0].Content);
        Assert.Equal("听众A", page.Comments[0].Nickname);
        Assert.Equal(10, page.Comments[0].LikedCount);
        Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(1700000000000), page.Comments[0].Time);
    }

    [Fact]
    public void ParseComments_EmptyPage_ReturnsZeroTotal()
    {
        var page = NeteaseCatalogApi.ParseComments("""{"total":0,"comments":[]}""");

        Assert.Equal(0, page.Total);
        Assert.Empty(page.Comments);
    }
}
