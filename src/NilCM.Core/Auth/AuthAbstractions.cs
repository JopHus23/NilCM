namespace NilCM.Core.Auth;

/// <summary>认证抽象：当前仅匿名 cookie；二维码登录为后续切片。</summary>
public interface IAuthApi
{
    /// <summary>获取匿名 cookie（形如 "MUSIC_U=...; Expires=..."）。失败抛异常（由调用方决定降级策略）。</summary>
    Task<string> GetAnonymousCookieAsync(CancellationToken cancellationToken = default);
}

/// <summary>登录凭据持久化抽象。实现位于 NilCM.Infrastructure。</summary>
public interface ITokenStore
{
    Task<string?> GetCookieAsync(CancellationToken cancellationToken = default);

    Task SaveCookieAsync(string cookie, CancellationToken cancellationToken = default);
}
