using UnityEngine;
using UnityEngine.UI;
using Events;

public class KeyPanel : MonoBehaviour
{
    [Tooltip("重要线索图片")]
    public Image KeyImage;
    private static KeyPanel s_instance;
    [Header("DetailBar 大图查看")]
    [SerializeField] GameObject imageViewer;       // 弹出层/大图面板
    [SerializeField] Image imageViewerImage;       // 用于显示大图的 Image

    void Awake()
    {
        s_instance = this;
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

    public void OnClickViewImage()
    {
        if (KeyImage == null || imageViewer == null) return;
        Debug.Log($"[{GetType().Name}.OnClickViewImage] 点击查看大图");

        if (imageViewerImage == null)
            imageViewerImage = imageViewer.GetComponentInChildren<Image>(true);

        imageViewerImage.sprite = KeyImage.sprite;
        imageViewer.SetActive(true);
        imageViewerImage.gameObject.SetActive(true);
        // imageViewerImage.SetNativeSize();     // 按需：用容器自适配可移除

    }

    public static void Reset()
    {
        if (s_instance != null)
        {
            s_instance.KeyImage.sprite = null;
            Color color = s_instance.KeyImage.color;
            color.a = 0f;
            s_instance.KeyImage.color = color;
        }
    }
}