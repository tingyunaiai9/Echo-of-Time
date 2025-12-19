using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.UI;


public class ClueCanvas : MonoBehaviour
{
    [Header("UI Components")]
    [Tooltip("显示线索图标的 Image 组件")]
    public Image clueImageDisplay;

    [Tooltip("显示线索描述的 Text 组件")]
    public TMP_Text clueDescriptionText;

    [Tooltip("包含所有内容的面板（用于控制显示/隐藏）")]
    public GameObject contentPanel;
    [Tooltip("通知UI面板")]
    public NotificationController notificationUI;

    private void Awake()
    {
        // 游戏开始时，确保 Content 面板是隐藏的，但 ClueCanvas 本身保持激活以便被查找
        if (contentPanel != null)
        {
            contentPanel.SetActive(false);
        }
    }

    public void ShowClue(string clueText, Sprite icon, string description)
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

        // 显示通知消息
        if (notificationUI != null)
        {
            string notificationMessage = "";
            if (clueText == "应揭之天干")
            {
                notificationMessage = "已获得线索\"应揭之天干\"并添加至日记共享线索栏";
            }
            else if (clueText == "罗盘")
            {
                notificationMessage = "已获得线索\"罗盘\"并添加至日记共享线索栏";
            }
            else if (clueText == "对照表")
            {
                notificationMessage = "已获得线索\"ASCII对照表\"并添加至背包线索栏";
            }
            else
            {
                notificationMessage = $"已获得线索\"{clueText}\"并添加至背包关键线索";
            }
            notificationUI.ShowNotification(notificationMessage);
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
