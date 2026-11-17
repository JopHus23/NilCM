using NilCM.Netease.Crypto;

namespace NilCM.Netease.Tests;

public class WeapiCryptoTests
{
    [Fact]
    public void Encrypt_WithFixedKey_ProducesWellFormedOutput()
    {
        var (ps, esk) = WeapiCrypto.Encrypt("""{"ids":"[186016]"}""", "abcdefghij012345");

        Assert.True(Convert.FromBase64String(ps).Length > 0);  // params 可解码
        Assert.Matches("^[0-9a-f]{256}$", esk);                // RSA 输出 = 128 字节小写 hex
    }

    [Fact]
    public void Encrypt_WithFixedKey_IsDeterministic()
    {
        var a = WeapiCrypto.Encrypt("""{"username":"nilcm"}""", "abcdefghij012345");
        var b = WeapiCrypto.Encrypt("""{"username":"nilcm"}""", "abcdefghij012345");

        Assert.Equal(a, b);
    }

    [Fact]
    public void Encrypt_DifferentKeys_ProduceDifferentCiphertext()
    {
        var a = WeapiCrypto.Encrypt("""{"username":"nilcm"}""", "abcdefghij012345");
        var b = WeapiCrypto.Encrypt("""{"username":"nilcm"}""", "ZYXWVUTSRQPONMLK");

        Assert.NotEqual(a.Params, b.Params);
        Assert.NotEqual(a.EncSecKey, b.EncSecKey);
    }

    [Fact]
    public void Encrypt_LongText_ProducesMultiBlockParams()
    {
        var longText = """{"lyric":"足够长的内容用于验证多分组与 PKCS7 填充路径……"}""" + new string('词', 200);
        var (ps, _) = WeapiCrypto.Encrypt(longText, "abcdefghij012345");

        Assert.True(Convert.FromBase64String(ps).Length >= 16 * 2);
    }
}
