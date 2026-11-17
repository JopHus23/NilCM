using System.Text.Json;
using NilCM.Core.Models;

namespace NilCM.Netease.Catalog;

/// <summary>目录类响应的容错 JSON 解析助手（新旧字段形态：ar/artists、al/album、dt/duration）。DTO 不出程序集。</summary>
internal static class NeteaseJson
{
    public static JsonElement? Prop(JsonElement e, string name)
    {
        if (e.ValueKind != JsonValueKind.Object || !e.TryGetProperty(name, out var v))
        {
            return null;
        }

        return v;
    }

    public static JsonElement Obj(JsonElement e, params string[] names)
    {
        foreach (var n in names)
        {
            var v = Prop(e, n);
            if (v.HasValue && v.Value.ValueKind == JsonValueKind.Object)
            {
                return v.Value;
            }
        }

        return default;
    }

    public static JsonElement Arr(JsonElement e, params string[] names)
    {
        foreach (var n in names)
        {
            var v = Prop(e, n);
            if (v.HasValue && v.Value.ValueKind == JsonValueKind.Array)
            {
                return v.Value;
            }
        }

        return default;
    }

    public static string? Str(JsonElement e, params string[] names)
    {
        foreach (var n in names)
        {
            var v = Prop(e, n);
            if (v.HasValue && v.Value.ValueKind == JsonValueKind.String)
            {
                return v.Value.GetString();
            }
        }

        return null;
    }

    public static long Num(JsonElement e, params string[] names)
    {
        foreach (var n in names)
        {
            var v = Prop(e, n);
            if (v.HasValue && v.Value.ValueKind == JsonValueKind.Number && v.Value.TryGetInt64(out var l))
            {
                return l;
            }
        }

        return 0;
    }

    /// <summary>歌曲条目 → Track（兼容 ar/artists、al/album、dt/duration 两种形态）。</summary>
    public static IReadOnlyList<Track> MapTracks(JsonElement songsArray)
    {
        var tracks = new List<Track>();
        if (songsArray.ValueKind != JsonValueKind.Array)
        {
            return tracks;
        }

        foreach (var song in songsArray.EnumerateArray())
        {
            var artistNames = new List<string>();
            long artistId = 0;
            var artists = Arr(song, "ar", "artists");
            if (artists.ValueKind == JsonValueKind.Array)
            {
                foreach (var a in artists.EnumerateArray())
                {
                    var n = Str(a, "name");
                    if (!string.IsNullOrEmpty(n))
                    {
                        artistNames.Add(n);
                    }

                    if (artistId == 0)
                    {
                        artistId = Num(a, "id");
                    }
                }
            }

            var album = Obj(song, "al", "album");
            var albumId = 0L;
            var albumName = string.Empty;
            if (album.ValueKind == JsonValueKind.Object)
            {
                albumId = Num(album, "id");
                albumName = Str(album, "name") ?? string.Empty;
            }

            tracks.Add(new Track(
                Num(song, "id"),
                Str(song, "name") ?? string.Empty,
                string.Join("/", artistNames),
                artistId,
                albumName,
                albumId,
                TimeSpan.FromMilliseconds(Num(song, "dt", "duration")),
                Num(song, "fee") == 1));
        }

        return tracks;
    }
}
