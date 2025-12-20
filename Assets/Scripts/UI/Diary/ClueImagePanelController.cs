using UnityEngine;
using UnityEngine.UI;

/*
 * 控制线索大图面板的显示与尺寸适配
 * 目的：只在指定的 ImageDisplay 中显示，避免全屏拉伸
 */
public class ClueImagePanelController : MonoBehaviour
{
    [Header("面板引用")]
    public GameObject panelRoot;        // ClueImagePanel 根节点
    public Image imageDisplay;          // 指定的 ImageDisplay（UI Image）
    public Button closeButton;          // 关闭按钮（CloseImageButton）

    [Header("显示设置")]
    public float padding = 24f;         // 与父容器边距，防止贴边

    void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    // 在指定的 ImageDisplay 中显示图片（Sprite）
    public void Show(Sprite sprite)
    {
        if (sprite == null || panelRoot == null || imageDisplay == null) return;

        panelRoot.SetActive(true);

        imageDisplay.type = Image.Type.Simple;      // 简单图片类型
        imageDisplay.preserveAspect = true;         // 保持宽高比
        imageDisplay.sprite = sprite;

        var rt = imageDisplay.rectTransform;
        // 锚点与枢轴居中，避免拉伸填满父节点
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);

        FitToParent(rt, (RectTransform)imageDisplay.transform.parent, sprite.rect.size);
    }

    public void Hide()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    // 按父容器尺寸适配并居中显示
    private void FitToParent(RectTransform target, RectTransform parent, Vector2 spriteSize)
    {
        if (parent == null || target == null) return;

        var parentRect = parent.rect;
        float maxW = Mathf.Max(0f, parentRect.width  - padding * 2f);
        float maxH = Mathf.Max(0f, parentRect.height - padding * 2f);

        float aspect = (spriteSize.x > 0f) ? (spriteSize.y / spriteSize.x) : 1f;

        // 先以最大宽度适配，再按需要以高度限制
        float w = maxW;
        float h = w * aspect;
        if (h > maxH)
        {
            h = maxH;
            w = h / aspect;
        }

        target.sizeDelta = new Vector2(w, h);
        target.anchoredPosition = Vector2.zero; // 居中
    }
}
