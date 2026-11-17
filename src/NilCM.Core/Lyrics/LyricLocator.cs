using NilCM.Core.Models;

namespace NilCM.Core.Lyrics;

/// <summary>
/// 根据播放进度定位当前歌词行（桌面歌词/滚动歌词的纯逻辑底座，UI 阶段直接复用）。
/// </summary>
public static class LyricLocator
{
    /// <summary>返回当前应显示（含卡拉OK式已唱过）的行索引；进度早于首行或无歌词返回 null。</summary>
    public static int? FindCurrentLineIndex(IReadOnlyList<LyricLine> lines, TimeSpan position)
    {
        if (lines.Count == 0 || position < lines[0].Time)
        {
            return null;
        }

        // 歌词行数量级小，线性扫描足够；lines 已按时间排序
        var index = 0;
        for (var i = 0; i < lines.Count; i++)
        {
            if (lines[i].Time > position)
            {
                break;
            }

            index = i;
        }

        return index;
    }
}
