/* VolcSigner.cs
 *
 * 移植自火山引擎官方 Java 签名示例 (Sign.java)
 * 负责执行所有 SigV4 风格的加密哈希运算，用于生成鉴权标头。
 * 包含：HMAC-SHA256, SHA256, 派生密钥 (kSigning)
 */
using System;
using System.Text;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Linq;

public static class VolcSigner
{
    private static readonly Encoding s_utf8 = Encoding.UTF8;

    /// <summary>
    /// 对字节数组执行 SHA256 哈希，并返回十六进制字符串 (兼容 Unity)
    /// </summary>
    public static string HashSHA256(byte[] data)
    {
        // 使用 .NET Standard 2.0 / .NET Framework 兼容的创建实例方法
        using (var sha256 = SHA256.Create())
        {
            byte[] hashBytes = sha256.ComputeHash(data);
            return ToHexString(hashBytes);
        }
    }

    /// <summary>
    /// 对字符串执行 SHA256 哈希，并返回十六进制字符串
    /// </summary>
    public static string HashSHA256(string data)
    {
        return HashSHA256(s_utf8.GetBytes(data));
    }

    /// <summary>
    /// 执行 HMAC-SHA256 哈希
    /// </summary>
    /// <param name="key">HMAC 密钥 (byte[])</param>
    /// <param name="data">要哈希的数据 (string)</param>
    /// <returns>哈希结果 (byte[])</returns>
    public static byte[] HmacSHA256(byte[] key, string data)
    {
        using (var hmac = new HMACSHA256(key))
        {
            return hmac.ComputeHash(s_utf8.GetBytes(data));
        }
    }

    /// <summary>
    /// 步骤 4：派生签名密钥 (kSigning)
    /// </summary>
    public static byte[] GenSigningSecretKeyV4(string secretKey, string date, string region, string service)
    {
        byte[] kDate = HmacSHA256(s_utf8.GetBytes(secretKey), date);
        byte[] kRegion = HmacSHA256(kDate, region);
        byte[] kService = HmacSHA256(kRegion, service);
        return HmacSHA256(kService, "request");
    }

    /// <summary>
    /// 字节数组转为小写十六进制字符串
    /// </summary>
    public static string ToHexString(byte[] data)
    {
        return BitConverter.ToString(data).Replace("-", "").ToLowerInvariant();
    }

    /// <summary>
    /// (未在即梦 API 中使用，但为标准 RFC3986 编码器)
    /// </summary>
    public static string Rfc3986Encode(string data)
    {
        // Uri.EscapeDataString 遵循 RFC3986
        return Uri.EscapeDataString(data);
    }
}