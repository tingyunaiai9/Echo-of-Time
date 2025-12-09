using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class PrunePanel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("光标配置")]
    [Tooltip("进入 Panel 时显示的光标图片（默认状态）")]
    public Texture2D cursorTexture;

    [Tooltip("点击时显示的光标图片")]
    public Texture2D cursorTexturePressed;

    [Tooltip("光标热点位置（相对于图片左上角的偏移）")]
    public Vector2 hotspot = Vector2.zero;

    [Tooltip("点击状态下的光标热点位置")]
    public Vector2 hotspotPressed = Vector2.zero;

    [Header("macOS 缩放配置")]
    [Tooltip("macOS 平台下光标缩放比例（建议 0.1）")]
    [Range(0.1f, 1.0f)]
    public float macOSScale = 0.1f;

    [Header("点击状态持续时间")]
    [Tooltip("点击后光标保持按下状态的时间（秒）")]
    public float pressedDuration = 0.2f;

    private bool isPointerInside = false;
    private bool isPressed = false;
    
    // 缩放后的光标纹理（仅 macOS 使用）
    private Texture2D scaledCursorTexture;
    private Texture2D scaledCursorTexturePressed;
    
    // 缩放后的热点位置
    private Vector2 scaledHotspot;
    private Vector2 scaledHotspotPressed;

    // 用于延迟恢复光标的协程引用
    private Coroutine resetCursorCoroutine;

    void Start()
    {
        // 如果未设置光标图片，输出警告
        if (cursorTexture == null)
        {
            Debug.LogWarning("[PrunePanel] 未设置 cursorTexture！");
        }
        if (cursorTexturePressed == null)
        {
            Debug.LogWarning("[PrunePanel] 未设置 cursorTexturePressed！");
        }

        // 在 macOS 上预先缩放光标纹理
        if (Application.platform == RuntimePlatform.OSXPlayer || 
            Application.platform == RuntimePlatform.OSXEditor)
        {
            if (cursorTexture != null)
            {
                scaledCursorTexture = ScaleTexture(cursorTexture, macOSScale);
                scaledHotspot = hotspot * macOSScale;
            }
            
            if (cursorTexturePressed != null)
            {
                scaledCursorTexturePressed = ScaleTexture(cursorTexturePressed, macOSScale);
                scaledHotspotPressed = hotspotPressed * macOSScale;
            }
        }
        else
        {
            // Windows 平台直接使用原始纹理
            scaledCursorTexture = cursorTexture;
            scaledCursorTexturePressed = cursorTexturePressed;
            scaledHotspot = hotspot;
            scaledHotspotPressed = hotspotPressed;
        }
    }

    // 鼠标进入 Panel 时调用
    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerInside = true;
        UpdateCursor();
    }

    // 鼠标离开 Panel 时调用
    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerInside = false;
        isPressed = false;
        
        // 停止延迟恢复协程
        if (resetCursorCoroutine != null)
        {
            StopCoroutine(resetCursorCoroutine);
            resetCursorCoroutine = null;
        }
        
        // 恢复默认光标
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    // 鼠标按下时调用
    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        UpdateCursor();
    }

    // 鼠标抬起时调用
    public void OnPointerUp(PointerEventData eventData)
    {
        // 不立即恢复，而是延迟 0.5 秒
        if (resetCursorCoroutine != null)
        {
            StopCoroutine(resetCursorCoroutine);
        }
        resetCursorCoroutine = StartCoroutine(ResetCursorAfterDelay());
    }

    // 延迟恢复光标状态的协程
    private IEnumerator ResetCursorAfterDelay()
    {
        yield return new WaitForSeconds(pressedDuration);
        
        isPressed = false;
        UpdateCursor();
        resetCursorCoroutine = null;
        
    }

    // 更新光标显示
    private void UpdateCursor()
    {
        if (!isPointerInside)
            return;

        if (isPressed && scaledCursorTexturePressed != null)
        {
            Cursor.SetCursor(scaledCursorTexturePressed, scaledHotspotPressed, CursorMode.Auto);
        }
        else if (scaledCursorTexture != null)
        {
            Cursor.SetCursor(scaledCursorTexture, scaledHotspot, CursorMode.Auto);
        }
    }

    // 缩放纹理
    private Texture2D ScaleTexture(Texture2D source, float scale)
    {
        int newWidth = Mathf.RoundToInt(source.width * scale);
        int newHeight = Mathf.RoundToInt(source.height * scale);
        
        Texture2D result = new Texture2D(newWidth, newHeight, source.format, false);
        
        for (int y = 0; y < newHeight; y++)
        {
            for (int x = 0; x < newWidth; x++)
            {
                float u = (float)x / newWidth;
                float v = (float)y / newHeight;
                result.SetPixel(x, y, source.GetPixelBilinear(u, v));
            }
        }
        
        result.Apply();
        return result;
    }

    void OnDisable()
    {
        // 当 Panel 被禁用时，确保恢复默认光标
        if (isPointerInside)
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            isPointerInside = false;
            isPressed = false;
        }
        
        // 停止延迟恢复协程
        if (resetCursorCoroutine != null)
        {
            StopCoroutine(resetCursorCoroutine);
            resetCursorCoroutine = null;
        }
    }

    void OnDestroy()
    {
        // 清理动态创建的纹理
        if (Application.platform == RuntimePlatform.OSXPlayer || 
            Application.platform == RuntimePlatform.OSXEditor)
        {
            if (scaledCursorTexture != null && scaledCursorTexture != cursorTexture)
            {
                Destroy(scaledCursorTexture);
            }
            if (scaledCursorTexturePressed != null && scaledCursorTexturePressed != cursorTexturePressed)
            {
                Destroy(scaledCursorTexturePressed);
            }
        }
    }
}