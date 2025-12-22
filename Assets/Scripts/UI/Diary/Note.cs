using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Note : MonoBehaviour
{
    [Header("便签标题")]
    [Tooltip("便签显示的标签文本")]
    public TextMeshProUGUI noteTitleText;
    [Tooltip("便签显示的图片")]
    public Image noteImage;

    void Awake()
    {
        // 初始化逻辑
        if (noteTitleText == null)
            noteTitleText = GetComponentInChildren<TextMeshProUGUI>();
        if (noteImage == null)
            noteImage = GetComponentInChildren<Image>();
    }

    public void OnClickViewImage()
    {
        if (noteImage == null || noteImage.sprite == null)
        {
            Debug.LogWarning($"[{GetType().Name}.OnClickViewImage] noteImage 或其 sprite 未设置");
            return;
        }

        // 找到 ClueImagePanel（包含未激活对象）
        ClueImagePanel clueImagePanel = FindFirstObjectByType<ClueImagePanel>(FindObjectsInactive.Include);
        if (clueImagePanel == null)
        {
            Debug.LogWarning($"[{GetType().Name}.OnClickViewImage] 未找到 ClueImagePanel");
            return;
        }

        // 激活根节点
        clueImagePanel.gameObject.SetActive(true);

        // 在 ClueImagePanel 下查找名为 ImageDisplay 的 Image 组件（包含未激活）
        Image imageDisplay = null;
        var images = clueImagePanel.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i].name == "ImageDisplay")
            {
                imageDisplay = images[i];
                break;
            }
        }

        if (imageDisplay == null)
        {
            Debug.LogWarning($"[{GetType().Name}.OnClickViewImage] 未在 ClueImagePanel 下找到名为 ImageDisplay 的 Image 组件");
            return;
        }

        imageDisplay.gameObject.SetActive(true);
        imageDisplay.preserveAspect = true;
        imageDisplay.sprite = noteImage.sprite;
    }
}