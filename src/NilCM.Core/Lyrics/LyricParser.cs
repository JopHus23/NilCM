using System.Globalization;
using System.Text.RegularExpressions;
using NilCM.Core.Models;

namespace NilCM.Core.Lyrics;

/// <summary>
/// LRC 文本 → 结构化歌词行（纯函数，可单测）。
/// 约定：<c>[offset:+n]</c> 使歌词整体提前 n 毫秒（有效时间 = 原时间 - offset），与常见播放器一致。
/// </summary>
public static partial class LyricParser
{
    [GeneratedRegex(@"\[(\d{1,3}):(\d{1,2})(?:\.(\d{1,3}))?\]")]
    private static partial Regex TimeTagRegex();

    [GeneratedRegex(@"^\s*\[offset:\s*([+-]?\d+)\s*\]", RegexOptions.IgnoreCase)]
    private static partial Regex OffsetTagRegex();

    public static IReadOnlyList<LyricLine> Parse(string? lrcText)
    {
        List<LyricLine> lines = [];
        if (string.IsNullOrWhiteSpace(lrcText))
        {
            return lines;
        }

        var offsetMs = 0;
        foreach (var rawLine in lrcText.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r').Trim();
            if (line.Length == 0)
            {
                continue;
            }

            var offsetMatch = OffsetTagRegex().Match(line);
            if (offsetMatch.Success)
            {
                offsetMs = int.Parse(offsetMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                continue;
            }

            var matches = TimeTagRegex().Matches(line);
            if (matches.Count == 0)
            {
                continue; // 元数据行 / 无时间戳行：丢弃
            }

            var text = line[(matches[^1].Index + matches[^1].Length)..].Trim();
            if (text.Length == 0)
            {
                continue; // 空文本行：丢弃
            }

            foreach (Match match in matches)
            {
                var minutes = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                var seconds = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
                var milliseconds = ParseFraction(match.Groups[3].Success ? match.Groups[3].Value : null);

                var time = TimeSpan.FromMilliseconds(minutes * 60_000 + seconds * 1_000 + milliseconds - offsetMs);
                if (time < TimeSpan.Zero)
                {
                    time = TimeSpan.Zero; // 偏移导致的负时间：钳到 0
                }

                lines.Add(new LyricLine(time, text));
            }
        }

        return lines.OrderBy(l => l.Time).ToList();
    }

    /// <summary>小数部分 → 毫秒：".5"→500ms，".05"→50ms，".123"→123ms。</summary>
    private static int ParseFraction(string? fraction) => fraction switch
    {
        null => 0,
        _ => int.Parse(fraction.PadRight(3, '0'), CultureInfo.InvariantCulture),
    };
}
