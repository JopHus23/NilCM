namespace NilCM.Core.Models;

/// <summary>一行歌词：时间轴 + 文本。</summary>
/// <remarks>Translation 为翻译歌词预留（网易 tlyric），当前切片不填充。</remarks>
public sealed record LyricLine(TimeSpan Time, string Text, string? Translation = null);
