namespace NilCM.Core.Playback;

/// <summary>音质等级。当前映射：Standard=128kbps，High=320kbps；无损/HiRes 随后续切片扩充。</summary>
public enum AudioQuality
{
    Standard,
    High,
}

/// <summary>一次成功的播放地址解析结果。IsTrial=true 表示试听片段（VIP/无版权回退）。</summary>
public sealed record PlayUrlResult(long TrackId, string Url, int Bitrate, bool IsTrial);
