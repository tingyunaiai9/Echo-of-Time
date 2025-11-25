/* UI/Diary/VolcSigner.cs
 * 火山引擎 SigV4 签名工具类
 * 移植自官方 Java 示例，用于执行 SigV4 所需的哈希与 HMAC 运算
 * 提供 HashSHA256、HmacSHA256、GenSigningSecretKeyV4 等基础工具方法
 */
using System;
using System.Text;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Linq;

/*
 * 火山引擎签名辅助静态类
 * 封装所有与签名相关的底层运算逻辑
 */
public static class VolcSigner
{
    private static readonly Encoding s_utf8 = Encoding.UTF8;

    /*
     * 对字节数组执行 SHA256 哈希，并返回十六进制字符串 (兼容 Unity)
     */
    public static string HashSHA256(byte[] data)
    {
        // 使用 .NET Standard 2.0 / .NET Framework 兼容的创建实例方法
        using (var sha256 = SHA256.Create())
        {
            byte[] hashBytes = sha256.ComputeHash(data);
            return ToHexString(hashBytes);
        }
    }

    /*
     * 对字符串执行 SHA256 哈希，并返回十六进制字符串
     */
    public static string HashSHA256(string data)
    {
        return HashSHA256(s_utf8.GetBytes(data));
    }

    /*
     * 执行 HMAC-SHA256 哈希
     * @param key  HMAC 密钥 (byte[])
     * @param data 要哈希的数据 (string)
     * @return     哈希结果 (byte[])
     */
    public static byte[] HmacSHA256(byte[] key, string data)
    {
        using (var hmac = new HMACSHA256(key))
        {
            return hmac.ComputeHash(s_utf8.GetBytes(data));
        }
    }

    /*
     * 步骤 4：派生签名密钥 (kSigning)
     */
    public static byte[] GenSigningSecretKeyV4(string secretKey, string date, string region, string service)
    {
        byte[] kDate = HmacSHA256(s_utf8.GetBytes(secretKey), date);
        byte[] kRegion = HmacSHA256(kDate, region);
        byte[] kService = HmacSHA256(kRegion, service);
        return HmacSHA256(kService, "request");
    }

    /*
     * 字节数组转为小写十六进制字符串
     */
    public static string ToHexString(byte[] data)
    {
        return BitConverter.ToString(data).Replace("-", "").ToLowerInvariant();
    }

    /*
     * 标准 RFC3986 编码器（当前未在即梦 API 中使用）
     */
    public static string Rfc3986Encode(string data)
    {
        // Uri.EscapeDataString 遵循 RFC3986
        return Uri.EscapeDataString(data);
    }
}