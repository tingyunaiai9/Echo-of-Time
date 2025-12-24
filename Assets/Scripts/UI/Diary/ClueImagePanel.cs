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

        s_instance.ClueImageDisplay.sprite = clueSprite;
        Debug.Log("[ClueImagePanel.SetClueImage] 线索图片已更新");
    }
}