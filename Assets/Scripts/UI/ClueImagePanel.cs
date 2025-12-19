using UnityEngine;
using UnityEngine.UI;

public class ClueImagePanel : MonoBehaviour
{
    [Tooltip("线索图片显示区域")]
    public Image ClueImageDisplay;

    private static ClueImagePanel s_instance;

    void Awake()
    {
        s_instance = this;
    }

    /* 设置线索图片 */
    public static void SetClueImage(Sprite clueSprite)
    {
        if (s_instance == null || s_instance.ClueImageDisplay == null)
        {
            Debug.LogWarning("[ClueImagePanel.SetClueImage] 实例或 ClueImageDisplay 未初始化");
            return;
        }

        // 保持原始宽高比例显示
        var display = s_instance.ClueImageDisplay;
        display.preserveAspect = true;
        if (clueSprite != null)
        {
            float w = clueSprite.rect.width;
            float h = clueSprite.rect.height;
            if (w > 0f && h > 0f)
            {
                var fitter = display.GetComponent<AspectRatioFitter>();
                if (fitter == null) fitter = display.gameObject.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent; // 在父容器内按比例适配
                fitter.aspectRatio = w / h;
            }
        }

        display.sprite = clueSprite;
        Debug.Log("[ClueImagePanel.SetClueImage] 线索图片已更新");
    }
}