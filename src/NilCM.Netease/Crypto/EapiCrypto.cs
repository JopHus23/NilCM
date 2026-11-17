using System.Security.Cryptography;
using System.Text;

namespace NilCM.Netease.Crypto;

/// <summary>
/// eapi 加密（常量与步骤来源：社区加密实现 crypto.js，已核对原文）。仅本程序集内使用。
/// digest 消息 = nobody{apiPath}use{json}md5forencrypt；data = {apiPath}-36cd479b6b5-{json}-36cd479b6b5-{digest}；
/// params = AES-128-ECB(data, eapiKey) 的十六进制大写。
/// </summary>
internal static class EapiCrypto
{
    private const string EapiKey = "e82ckenh8dichen8";

    public static string Encrypt(string apiPath, string jsonText)
    {
        var digest = Convert.ToHexString(
            MD5.HashData(Encoding.UTF8.GetBytes($"nobody{apiPath}use{jsonText}md5forencrypt"))).ToLowerInvariant();
        var data = $"{apiPath}-36cd479b6b5-{jsonText}-36cd479b6b5-{digest}";
        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(EapiKey);
        return Convert.ToHexString(aes.EncryptEcb(Encoding.UTF8.GetBytes(data), PaddingMode.PKCS7));
    }
}
