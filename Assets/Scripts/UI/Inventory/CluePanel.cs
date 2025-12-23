/* UI/CluePanel.cs
 * 线索面板控制器，显示解谜相关的提示和线索信息
 */
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Events;

/*
 * 线索面板：继承 Inventory，负责展示与去重管理线索
 */
public class CluePanel : Inventory
{
    // 已收集线索的去重集合
    readonly HashSet<string> _clueIds = new HashSet<string>();

    [Header("DetailBar 大图查看")]
    [SerializeField] Button viewImageButton;       // 右下角按钮
    [SerializeField] GameObject imageViewer;       // 弹出层/大图面板
    [SerializeField] Image imageViewerImage;       // 用于显示大图的 Image

    Sprite currentDetailImage;                     // 当前选中线索的大图

    /* 添加线索 */
    public void AddClue(string clueId, string clueText, string clueDescription = "", Sprite icon = null, Sprite image = null)
    {
        if (string.IsNullOrEmpty(clueId)) return;
        if (!_clueIds.Add(clueId)) return; // 已存在则忽略

        Debug.Log($"[CluePanel.AddClue] 添加新线索: {clueId}, text: {clueText}, icon: {(icon != null ? icon.name : "null")}");
        // 创建 UI 条目
        var item = new InventoryItem
        {
            itemId = clueId,
            itemName = clueText,
            description = clueDescription,
            quantity = 1,
            icon = icon,
            image = image
        };
        Debug.Log($"[CluePanel.AddClue] 线索描述为: {clueDescription}");
        CreateOrUpdateItemUI(item);
    }

    /* 订阅线索发现事件 */
    protected override void Awake()
    {
        base.Awake();
        if (viewImageButton != null)
            viewImageButton.onClick.AddListener(OnClickViewImage);

        EventBus.Subscribe<ClueDiscoveredEvent>(OnClueDiscovered);
        Debug.Log("[CluePanel.Awake] 已订阅 ClueDiscoveredEvent");
    }

    /* 在销毁时取消订阅 */
    protected override void OnDestroy()
    {
        EventBus.Unsubscribe<ClueDiscoveredEvent>(OnClueDiscovered);
        Debug.Log("[CluePanel.OnDestroy] 已取消订阅 ClueDiscoveredEvent");
        base.OnDestroy();
    }

    /* 处理线索发现事件 */
    void OnClueDiscovered(ClueDiscoveredEvent e)
    {
        Debug.Log($"[CluePanel.OnClueDiscovered] 收到事件 - clueId: {e.clueId}, text: {e.clueText}, icon: {(e.icon != null ? e.icon.name : "null")}");
        AddClue(e.clueId, e.clueText, e.clueDescription, e.icon, e.image);
    }

    // 在你已有的“显示线索详情”的地方调用此方法即可
    public void SetDetailImage(Sprite img)
    {
        currentDetailImage = img;
        if (viewImageButton != null)
            viewImageButton.gameObject.SetActive(img != null);
    }
    
    protected override void OnItemClicked(string itemId)
    {
        base.OnItemClicked(itemId); // 仍然使用基类的详情展示：名称、描述、图标

        // 从基类受保护字典中取 InventoryItem，携带 image
        if (itemData != null && itemData.TryGetValue(itemId, out var item))
        {
            Debug.Log($"[{GetType().Name}.OnItemClicked] 线索详情图像设置: {(item.image != null ? item.image.name : "null")}");
            SetDetailImage(item.image); // 让按钮只在有图时可见
        }
        else
        {
            SetDetailImage(null);
        }
    }


    void OnClickViewImage()
    {
        if (currentDetailImage == null || imageViewer == null) return;
        Debug.Log($"[{GetType().Name}.OnClickViewImage] 点击查看大图");

        if (imageViewerImage == null)
            imageViewerImage = imageViewer.GetComponentInChildren<Image>(true);
        if (imageViewerImage == null)
        {
            Debug.LogError("[CluePanel] 未找到用于显示大图的 Image 组件。");
            return;
        }
        // 保持原始宽高比例
        imageViewerImage.preserveAspect = true;
        var sprite = currentDetailImage;
        // if (sprite != null)
        // {
        //     float w = sprite.rect.width;
        //     float h = sprite.rect.height;
        //     if (w > 0f && h > 0f)
        //     {
        //         var fitter = imageViewerImage.GetComponent<AspectRatioFitter>();
        //         if (fitter == null) fitter = imageViewerImage.gameObject.AddComponent<AspectRatioFitter>();
        //         fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent; // 在父容器内按比例适配
        //         fitter.aspectRatio = w / h;
        //     }
        // }

        imageViewer.SetActive(true);
        imageViewerImage.gameObject.SetActive(true);
        imageViewerImage.sprite = currentDetailImage;
        // imageViewerImage.SetNativeSize();     // 按需：用容器自适配可移除
        
    }

    // 可选：给关闭按钮绑定
    public void CloseImageViewer()
    {
        if (imageViewer != null) imageViewer.SetActive(false);
    }
}