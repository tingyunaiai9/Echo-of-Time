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
        if (noteImage == null) return;
        Debug.Log($"[{GetType().Name}.OnClickViewImage] 点击查看大图");

        ClueImagePanel.OnClickViewImage(noteImage.sprite);
    }
}