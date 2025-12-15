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
    [Header("DetailBar 大图查看")]
    [SerializeField] Button viewImageButton;       // 右下角按钮

    void Awake()
    {
        viewImageButton = FindFirstObjectByType<Button>();
        // 初始化逻辑
        if (noteTitleText == null)
            noteTitleText = GetComponentInChildren<TextMeshProUGUI>();
        if (noteImage == null)
            noteImage = GetComponentInChildren<Image>();
        if (viewImageButton != null)
            viewImageButton.onClick.AddListener(OnClickViewImage);
    }

    public void OnClickViewImage()
    {
        if (noteImage == null || noteImage.sprite == null)
        {
            Debug.LogWarning($"[{GetType().Name}.OnClickViewImage] noteImage 或其 sprite 未设置");
            return;
        }

        Debug.Log($"[{GetType().Name}.OnClickViewImage] 点击查看大图");

        // 查找另一个场景中的 ClueImagePanel（包括非激活对象）
        ClueImagePanel clueImagePanel = FindFirstObjectByType<ClueImagePanel>();
        if (clueImagePanel == null)
        {
            Debug.LogError("[Note.OnClickViewImage] 无法找到 ClueImagePanel");
            return;
        }

        // 激活 ClueImagePanel
        clueImagePanel.gameObject.SetActive(true);

        // 设置 ClueImagePanel 的图片
        ClueImagePanel.SetClueImage(noteImage.sprite);
    }
}