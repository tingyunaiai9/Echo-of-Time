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
        
        // 使用 FindObjectsByType 并指定 FindObjectsInactive.Include 来查找未激活对象
        ClueImagePanel clueImagePanel = FindFirstObjectByType<ClueImagePanel>(FindObjectsInactive.Include);

        // 激活 ClueImagePanel
        clueImagePanel.gameObject.SetActive(true);
    
        // 设置 ClueImagePanel 的图片
        ClueImagePanel.SetClueImage(noteImage.sprite);
    }
}