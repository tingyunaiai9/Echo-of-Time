using UnityEngine;
using UnityEngine.UI;
using Events;

public class KeyPanel : MonoBehaviour
{
    [Tooltip("重要线索图片")]
    public Image KeyImage;

    void Awake()
    {
        EventBus.Subscribe<ClueDiscoveredEvent>(OnClueDiscovered);
        Debug.Log("[KeyPanel.Awake] 已订阅 ClueDiscoveredEvent");
    }

    void OnDestroy()
    {
        EventBus.Unsubscribe<ClueDiscoveredEvent>(OnClueDiscovered);
        Debug.Log("[KeyPanel.OnDestroy] 已取消订阅 ClueDiscoveredEvent");
    }

    void OnClueDiscovered(ClueDiscoveredEvent e)
    {
        Debug.Log($"[KeyPanel.OnClueDiscovered] 线索发现事件: {e.clueId}, 重要线索: {e.isKeyClue}");
        // 如果发现的线索是重要线索，更新 KeyImage
        if (e.isKeyClue && e.image != null)
        {
            KeyImage.sprite = e.image;
            
            // 设置透明度为完全不透明
            Color color = KeyImage.color;
            color.a = 1f;
            KeyImage.color = color;
            
            // 设置宽度为 400pt，高度根据原始比例计算
            float targetWidth = 400f;
            float aspectRatio = (float)e.image.texture.height / e.image.texture.width;
            float targetHeight = targetWidth * aspectRatio;
            
            RectTransform rectTransform = KeyImage.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(targetWidth, targetHeight);            
            Debug.Log($"[KeyPanel.OnClueDiscovered] 重要线索已更新: {e.clueId}, 尺寸设置为 {targetWidth}x{targetHeight}，透明度已设置为 1");
        }
    }
}
