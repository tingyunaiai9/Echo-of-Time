using UnityEngine;
using System.IO;

public static class ImageUtils
{
    /// <summary>
    /// 压缩Sprite图片为JPEG格式的byte[]，用于网络传输。
    /// quality参数建议为50~90，越低压缩越大但画质越差。
    /// </summary>
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

    /// <summary>
    /// 从JPEG字节流还原为Sprite
    /// </summary>
    public static Sprite SpriteFromJpegBytes(byte[] jpgBytes)
    {
        if (jpgBytes == null || jpgBytes.Length == 0)
            return null;
        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGB24, false);
        if (!tex.LoadImage(jpgBytes))
            return null;
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
    }

    /// <summary>
    /// 对原始图片字节流（byte[]，如PNG/JPG）进行压缩为JPEG格式的byte[]，用于网络传输。
    /// </summary>
    /// <param name="imageBytes">原始图片字节流（PNG/JPG等）</param>
    /// <param name="quality">JPEG压缩质量（1-100）</param>
    /// <returns>压缩后的JPEG byte[]，如失败返回null</returns>
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

