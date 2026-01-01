/*
 * MirrorObject.cs
 * 单个镜子物体的交互逻辑：拖拽、占槽、高亮与占用状态管理。
 */
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

/*
 * MirrorObject 类
 * 负责单个镜子的拖拽与镜槽占用，不处理镜子数量与 UI 计数。
 */
[RequireComponent(typeof(CanvasGroup))]
public class MirrorObject : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("拖拽设置")]
    [Tooltip("Canvas用于拖拽时的层级管理")]
    private Canvas canvas;
    
    [Tooltip("RectTransform组件")]
    private RectTransform rectTransform;
    
    [Tooltip("CanvasGroup组件")]
    private CanvasGroup canvasGroup;
    
    [Tooltip("原始父对象")]
    private Transform originalParent;
    
    [Tooltip("原始位置")]
    private Vector2 originalPosition;
    
    [Tooltip("拖拽时的克隆对象")]
    private GameObject draggedClone;
    
    [Tooltip("当前占用的镜槽")]
    private GameObject currentMirrorSlot;

    [Header("管理引用")]
    [Tooltip("镜子面板控制器，用于管理数量与重置")]
    [SerializeField] private MirrorPanel mirrorPanel;
    
    [Header("镜槽高亮设置")]
    [Tooltip("高亮时的 Outline 颜色")]
    public Color highlightOutlineColor = Color.yellow;
    
    [Tooltip("高亮时的 Outline 宽度")]
    public Vector2 highlightOutlineDistance = new Vector2(2, -2);
    
    [Header("检测配置")]
    [Tooltip("最大检测距离（像素）")]
    public float maxDetectionDistance = 150f;
    
    // 记录当前高亮的镜槽
    private GameObject currentHighlightedSlot = null;
    
    // ===== 静态字典：管理所有镜槽的占用状态 =====
    // Key: 镜槽GameObject, Value: 占用该镜槽的MirrorObject实例
    private static Dictionary<GameObject, MirrorObject> slotOccupancy = new Dictionary<GameObject, MirrorObject>();
    
    /* 初始化组件和变量 */
    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
        originalPosition = rectTransform.anchoredPosition;
        originalParent = transform.parent;

        if (mirrorPanel == null)
        {
            mirrorPanel = GetComponentInParent<MirrorPanel>();
        }
        mirrorPanel?.RegisterMirror(this);
    }
    
    // ======拖拽事件实现======

    /*
     * 开始拖拽事件
     */
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (mirrorPanel != null && !mirrorPanel.HasAvailableMirrors()) return;
        
        originalPosition = rectTransform.anchoredPosition;
        originalParent = transform.parent;
        CreateDraggedClone();
        canvasGroup.alpha = 0.6f;
    }

    /*
     * 拖拽中事件
     */
    public void OnDrag(PointerEventData eventData)
    {
        if (draggedClone == null) return;
        
        RectTransform cloneRect = draggedClone.GetComponent<RectTransform>();
        cloneRect.anchoredPosition += eventData.delta / canvas.scaleFactor;
        UpdateNearestSlotHighlight();
    }

    /*
     * 拖拽结束事件
     */
    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 1f;
        ClearAllHighlights();
        
        GameObject mirrorSlot = FindNearestMirrorSlot();
        if (mirrorSlot != null)
            ActivateMirrorSlot(mirrorSlot);
        
        DestroyDraggedClone();
    }

    // ===== 镜槽占用管理方法 =====
    
    /*
     * 检查镜槽是否被其他对象占用
     */
    private bool IsSlotOccupiedByOther(GameObject slot)
    {
        return slotOccupancy.ContainsKey(slot) && slotOccupancy[slot] != this;
    }
    
    /*
     * 占用镜槽
     */
    private void OccupySlot(GameObject slot)
    {
        slotOccupancy[slot] = this;
    }
    
    /*
     * 释放镜槽
     */
    private void ReleaseSlot(GameObject slot)
    {
        if (slotOccupancy.ContainsKey(slot) && slotOccupancy[slot] == this)
            slotOccupancy.Remove(slot);
    }

    /*
     * 创建拖拽时的克隆对象
     */
    private void CreateDraggedClone()
    {
        if (canvas == null) return;
        
        draggedClone = Instantiate(gameObject, canvas.transform);
        draggedClone.name = "DraggedMirror";
        
        RectTransform cloneRect = draggedClone.GetComponent<RectTransform>();
        cloneRect.position = rectTransform.position;
        cloneRect.sizeDelta = rectTransform.sizeDelta;
        cloneRect.localScale = rectTransform.localScale;
        
        CanvasGroup cloneCanvasGroup = draggedClone.GetComponent<CanvasGroup>();
        if (cloneCanvasGroup != null)
        {
            cloneCanvasGroup.alpha = 0.6f;
            cloneCanvasGroup.blocksRaycasts = false;
        }
        
        Destroy(draggedClone.GetComponent<MirrorObject>());
        
        Image cloneImage = draggedClone.GetComponent<Image>();
        if (cloneImage != null)
            cloneImage.raycastTarget = false;
    }

    /*
     * 销毁拖拽克隆
     */
    private void DestroyDraggedClone()
    {
        if (draggedClone != null)
        {
            Destroy(draggedClone);
            draggedClone = null;
        }
    }
    
    
    // =====高亮镜槽实现======
    
    /*
     * 更新最近镜槽的高亮状态
     */
    private void UpdateNearestSlotHighlight()
    {
        GameObject nearestSlot = FindNearestMirrorSlot();
        
        if (nearestSlot != currentHighlightedSlot)
        {
            if (currentHighlightedSlot != null)
                ResetSlotColor(currentHighlightedSlot);
            
            if (nearestSlot != null)
                HighlightSlot(nearestSlot);
            
            currentHighlightedSlot = nearestSlot;
        }
    }
    
    /*
     * 清除所有镜槽的高亮状态
     */
    private void ClearAllHighlights()
    {
        // 清除高亮状态（新版本不使用颜色高亮）
        currentHighlightedSlot = null;
    }
    
    /*
     * 高亮指定镜槽
     */
    private void HighlightSlot(GameObject mirrorSlot)
    {
        Outline outline = mirrorSlot.GetComponent<Outline>();
        if (outline != null)
        {
            outline.enabled = true;
            outline.effectColor = highlightOutlineColor;
            outline.effectDistance = highlightOutlineDistance;
        }
    }
    
    /*
     * 重置镜槽颜色
     */
    private void ResetSlotColor(GameObject mirrorSlot)
    {
        Outline outline = mirrorSlot.GetComponent<Outline>();
        if (outline != null)
        {
            outline.enabled = false;
        }
    }

    // =====镜槽检测与激活实现======
    
    /*
     * 查找最近的镜槽
     */
    private GameObject FindNearestMirrorSlot()
    {
        if (draggedClone == null) return null;

        RectTransform cloneRect = draggedClone.GetComponent<RectTransform>();
        BoxCollider2D[] allColliders = FindObjectsByType<BoxCollider2D>(FindObjectsSortMode.None);
        GameObject nearest = null;
        float minDistance = maxDetectionDistance;

        foreach (BoxCollider2D collider in allColliders)
        {
            GameObject obj = collider.gameObject;
            if (obj.layer == LayerMask.NameToLayer("Light") && obj.name.Contains("Mirror"))
            {
                if (IsSlotOccupiedByOther(obj)) continue;

                RectTransform slotRect = obj.GetComponent<RectTransform>();
                if (slotRect != null)
                {
                    float distance = Vector2.Distance(cloneRect.position, slotRect.position);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        nearest = obj;
                    }
                }
            }
        }

        return nearest;
    }

    /*
     * 激活指定镜槽
     */
    private void ActivateMirrorSlot(GameObject mirrorSlot)
    {
        if (mirrorPanel != null && !mirrorPanel.TryConsumeMirror())
            return;

        OccupySlot(mirrorSlot);
        
        // 启用 BoxCollider2D
        BoxCollider2D collider = mirrorSlot.GetComponent<BoxCollider2D>();
        if (collider != null) collider.enabled = true;

        // 恢复颜色和透明度
        Image image = mirrorSlot.GetComponent<Image>();
        if (image != null)
        {
            image.color = Color.white; // 恢复为原始颜色（完全不透明的白色）
        }

        currentMirrorSlot = mirrorSlot;
    }

    /*
     * 取消激活指定镜槽
     */
    private void DeactivateMirrorSlot(GameObject mirrorSlot)
    {
        if (mirrorSlot == null) return;
        
        // 禁用 BoxCollider2D
        BoxCollider2D collider = mirrorSlot.GetComponent<BoxCollider2D>();
        if (collider != null) collider.enabled = false;

        // 恢复为灰度，透明度 50%
        Image image = mirrorSlot.GetComponent<Image>();
        if (image != null)
        {
            Color grayColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            image.color = grayColor;
        }
    }

    /*
     * 从当前镜槽移除
     */
    public void RemoveFromMirrorSlot()
    {
        if (currentMirrorSlot != null)
        {
            ReleaseSlot(currentMirrorSlot);
            DeactivateMirrorSlot(currentMirrorSlot);
            currentMirrorSlot = null;
        }
    }

    /*
     * 对象销毁时的清理操作
     */
    /* 对象销毁时的清理操作 */
    void OnDestroy()
    {
        mirrorPanel?.UnregisterMirror(this);
        RemoveFromMirrorSlot();
        DestroyDraggedClone();
    }

    /* 设置面板引用（由 MirrorPanel 调用） */
    public void SetPanel(MirrorPanel panel)
    {
        mirrorPanel = panel;
    }

    /* 设置可交互与射线阻挡（由 MirrorPanel 控制） */
    public void SetInteractable(bool enabled)
    {
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = enabled;
            canvasGroup.interactable = enabled;
        }
    }

    /* 重置单个镜子的占位与高亮状态（由 MirrorPanel 调用） */
    public void ResetMirrorPlacement()
    {
        currentHighlightedSlot = null;
        RemoveFromMirrorSlot();
        DestroyDraggedClone();
    }

    /* 清空全局镜槽占用（由 MirrorPanel 调用） */
    public static void ClearSlotOccupancy()
    {
        slotOccupancy.Clear();
    }
}