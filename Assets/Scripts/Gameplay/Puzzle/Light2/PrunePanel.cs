using UnityEngine;
using UnityEngine.EventSystems;

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

    private bool isPointerInside = false;
    private bool isPressed = false;

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
        
        // 恢复默认光标
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        Debug.Log("[PrunePanel] 鼠标离开，光标已恢复");
    }

    // 鼠标按下时调用
    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        UpdateCursor();
        Debug.Log("[PrunePanel] 鼠标按下，光标切换为点击状态");
    }

    // 鼠标抬起时调用
    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
        UpdateCursor();
        Debug.Log("[PrunePanel] 鼠标抬起，光标恢复为默认状态");
    }

    // 更新光标显示
    private void UpdateCursor()
    {
        if (!isPointerInside)
            return;

        if (isPressed && cursorTexturePressed != null)
        {
            Cursor.SetCursor(cursorTexturePressed, hotspotPressed, CursorMode.Auto);
        }
        else if (cursorTexture != null)
        {
            Cursor.SetCursor(cursorTexture, hotspot, CursorMode.Auto);
        }
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
    }
}