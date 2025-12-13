using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ClueCanvas : MonoBehaviour
{
    [Header("UI Components")]
    [Tooltip("显示线索图标的 Image 组件")]
    public Image clueImageDisplay;

    [Tooltip("显示线索描述的 Text 组件")]
    public TMP_Text clueDescriptionText;

    [Tooltip("包含所有内容的面板（用于控制显示/隐藏）")]
    public GameObject contentPanel;

    private void Awake()
    {
        // 游戏开始时，确保 Content 面板是隐藏的，但 ClueCanvas 本身保持激活以便被查找
        if (contentPanel != null)
        {
            contentPanel.SetActive(false);
        }
    }

    public void ShowClue(Sprite icon, string description)
    {
        if (contentPanel != null) 
            contentPanel.SetActive(true);
        else 
            gameObject.SetActive(true);

        if (clueImageDisplay != null)
        {
            clueImageDisplay.sprite = icon;
            // 保持原比例，且不超过边框（按比例缩放到最大）
            clueImageDisplay.preserveAspect = true;
        }

        if (clueDescriptionText != null)
        {
            clueDescriptionText.text = description;
            // 宽度固定，长度随文字伸缩通常由 ContentSizeFitter 组件在场景中配置
        }
    }

    public void Close()
    {
        if (contentPanel != null) 
            contentPanel.SetActive(false);
        else 
            gameObject.SetActive(false);
    }
}
