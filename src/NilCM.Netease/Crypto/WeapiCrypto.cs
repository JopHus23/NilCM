using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace NilCM.Netease.Crypto;

/// <summary>
/// weapi 加密（常量与步骤经社区加密实现双源核对：raw + jsDelivr，jsososo fork util/crypto.js）。仅本程序集内使用。
/// 步骤：text → AES-CBC(预置密钥) → base64 → AES-CBC(随机密钥) → base64 = params；
/// 随机密钥倒序 → 左补零到 128 字节 → RSA 无填充模幂 → hex = encSecKey。
/// </summary>
internal static class WeapiCrypto
{
    private const string Charset = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private const string PublicKeySpkiBase64 =
        "MIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQDgtQn2JZ34ZC28NWYpAUd98iZ37BUrX/aKzmFbt7clFSs6sXqHauqKWqdtLkF2KexO40H1YTX8z2lSgBBOAxLsvaklV8k4cBFK9snQXE9/DDaFt6Rr7iVZMldczhC0JNgTz+SHXT6CBHuX3e9SdB1Ua44oncaTWz7OBGLbCiK45wIDAQAB";

    private static ReadOnlySpan<byte> PresetKey => "0CoJUm6Qyw8W8jud"u8;
    private static ReadOnlySpan<byte> Iv => "0102030405060708"u8;

    /// <summary>生产入口：每次随机 16 位密钥（base62 字符集）。</summary>
    public static (string Params, string EncSecKey) Encrypt(string text) => Encrypt(text, NewSecretKey());

    /// <summary>固定密钥入口（可测试：同输入同输出）。</summary>
    public static (string Params, string EncSecKey) Encrypt(string text, string secretKey)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secretKey);
        var firstPass = Convert.ToBase64String(AesCbc(Encoding.UTF8.GetBytes(text), PresetKey.ToArray()));
        var parameters = Convert.ToBase64String(AesCbc(Encoding.UTF8.GetBytes(firstPass), keyBytes));
        var encSecKey = RsaNoPaddingHex(keyBytes.Reverse().ToArray());
        return (parameters, encSecKey);
    }

    public static string NewSecretKey()
    {
        var random = RandomNumberGenerator.GetBytes(16);
        var chars = new char[16];
        for (var i = 0; i < 16; i++)
        {
            chars[i] = Charset[random[i] % Charset.Length];
        }

        return new string(chars);
    }

    private static byte[] AesCbc(byte[] data, byte[] key)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        return aes.EncryptCbc(data, Iv, PaddingMode.PKCS7);
    }

    private static string RsaNoPaddingHex(byte[] reversedSecretKey)
    {
        using var rsa = RSA.Create();
        rsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(PublicKeySpkiBase64), out _);
        var publicKey = rsa.ExportParameters(false);

        var padded = new byte[128]; // 左补零到 128 字节（Node 版：Buffer.alloc(128 - len) 前置）
        Array.Copy(reversedSecretKey, 0, padded, 128 - reversedSecretKey.Length, reversedSecretKey.Length);

        var m = ToBigIntegerBigEndian(padded);
        var e = ToBigIntegerBigEndian(publicKey.Exponent!);
        var n = ToBigIntegerBigEndian(publicKey.Modulus!);
        var cipher = BigInteger.ModPow(m, e, n);
        return ToBigEndianHexFixed128(cipher);
    }

    private static BigInteger ToBigIntegerBigEndian(byte[] bigEndianBytes)
    {
        var little = (byte[])bigEndianBytes.Clone();
        Array.Reverse(little);
        var unsigned = new byte[little.Length + 1]; // 高位补 0，保证按正数解析
        little.CopyTo(unsigned, 0);
        return new BigInteger(unsigned);
    }

    private static string ToBigEndianHexFixed128(BigInteger value)
    {
        var little = value.ToByteArray();
        var result = new byte[128];
        Array.Copy(little, result, Math.Min(little.Length, 128));
        Array.Reverse(result);
        return Convert.ToHexString(result).ToLowerInvariant();
    }
}
