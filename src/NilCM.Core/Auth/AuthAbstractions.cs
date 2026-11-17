namespace NilCM.Core.Auth;

/// <summary>二维码登录状态机。</summary>
public enum QrLoginState
{
    Waiting, // 801 二维码待扫描
    Scanned, // 802 已扫描，待手机确认
    Success, // 803 登录成功
    Expired, // 800 二维码已过期，需重新生成
}

/// <summary>一次二维码登录会话：unikey + 用于渲染二维码图片的内容 URL。</summary>
public sealed record QrLoginSession(string Unikey, string QrContentUrl);

/// <summary>轮询结果。仅 Success 时携带 Cookie（形如 "MUSIC_U=...; ..."）。</summary>
public sealed record QrCheckResult(QrLoginState State, string? Cookie);

/// <summary>认证抽象：二维码登录。实现位于 NilCM.Netease。</summary>
public interface IAuthApi
{
    /// <summary>创建二维码登录会话。</summary>
    Task<QrLoginSession> CreateQrLoginAsync(CancellationToken cancellationToken = default);

    /// <summary>轮询二维码状态。Success 时返回携带 Cookie 的结果。</summary>
    Task<QrCheckResult> CheckQrLoginAsync(string unikey, CancellationToken cancellationToken = default);
}

/// <summary>登录凭据持久化抽象。实现位于 NilCM.Infrastructure。</summary>
public interface ITokenStore
{
    Task<string?> GetCookieAsync(CancellationToken cancellationToken = default);

    Task SaveCookieAsync(string cookie, CancellationToken cancellationToken = default);
}

