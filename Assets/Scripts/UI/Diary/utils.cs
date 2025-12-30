/*
 * utils.cs
 * 图片工具方法集合：负责图片的压缩、解码与格式转换，供网络传输与 UI 显示使用。
 */
using UnityEngine;
using System.IO;

/*
 * ImageUtils 类
 * 提供静态工具方法：将 Sprite/图片字节流压缩为 JPEG，以及从 JPEG 恢复 Sprite。
 */
public static class ImageUtils
{
    /*
     * CompressSpriteToJpegBytes
     * 将给定的 Sprite 压缩为 JPEG 格式的字节数组，适合网络传输。
     * 参数：
     * - sprite: 待压缩的 Sprite
     * - quality: JPEG 压缩质量（1-100），默认 50
     */
    public static byte[] CompressSpriteToJpegBytes(Sprite sprite, int quality = 50)
    {
        if (sprite == null || sprite.texture == null)
            return null;

        Texture2D srcTex = sprite.texture;
        Rect rect = sprite.rect;
        Texture2D tex = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.RGB24, false);

        // 使用 RenderTexture 中转，解决 Texture 不可读的问题
        RenderTexture tmp = RenderTexture.GetTemporary(srcTex.width, srcTex.height, 0);
        Graphics.Blit(srcTex, tmp);
        
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = tmp;

        tex.ReadPixels(rect, 0, 0);
        tex.Apply();

        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(tmp);

        byte[] jpgBytes = tex.EncodeToJPG(quality);

        Object.Destroy(tex);

        return jpgBytes;
    }

    /*
     * SpriteFromJpegBytes
     * 从 JPEG 字节流还原为 Sprite 对象。
     */
    public static Sprite SpriteFromJpegBytes(byte[] jpgBytes)
    {
        if (jpgBytes == null || jpgBytes.Length == 0)
            return null;
        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGB24, false);
        if (!tex.LoadImage(jpgBytes))
            return null;
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
    }

    /*
     * CompressImageBytesToJpeg
     * 将原始图片字节流（PNG/JPG 等）压缩为 JPEG 字节流。
     * 参数：
     * - imageBytes: 原始图片字节流
     * - quality: JPEG 压缩质量（1-100）
     * 返回：压缩后的 JPEG 字节流，失败返回 null
     */
    public static byte[] CompressImageBytesToJpeg(byte[] imageBytes, int quality = 50)
    {
        if (imageBytes == null || imageBytes.Length == 0)
            return null;

        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGB24, false);
        if (!tex.LoadImage(imageBytes))
        {
            Object.Destroy(tex);
            return null;
        }

        byte[] jpgBytes = tex.EncodeToJPG(quality);
        Object.Destroy(tex);
        return jpgBytes;
    }
}

