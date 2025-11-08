using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

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
    
    [Header("镜槽颜色设置")]
    [Tooltip("镜槽激活时的颜色")]
    public Color activeMirrorColor = Color.blue;
    
    [Tooltip("镜槽原始颜色")]
    private Color originalMirrorColor = Color.gray;
    
    [Header("检测配置")]
    [Tooltip("最大检测距离（像素）")]
    public float maxDetectionDistance = 150f;
    
    // 记录当前高亮的镜槽
    private GameObject currentHighlightedSlot = null;
    
    // ===== 静态字典：管理所有镜槽的占用状态 =====
    // Key: 镜槽GameObject, Value: 占用该镜槽的MirrorObject实例
    private static Dictionary<GameObject, MirrorObject> slotOccupancy = new Dictionary<GameObject, MirrorObject>();
    
    // ===== 镜子计数管理 =====
    [Header("UI引用")]
    [Tooltip("镜子计数文本")]
    private TextMeshProUGUI mirrorCountText;
    
    [Tooltip("重置按钮")]
    private Button resetButton;
    
    [Tooltip("镜像图片(MirrorImage)")]
    private GameObject mirrorImage;
    
    // 静态变量：镜子计数
    private static int mirrorCount = 6;
    private const int MAX_MIRROR_COUNT = 6;
    
    // 静态列表：记录所有MirrorObject实例
    private static List<MirrorObject> allMirrorObjects = new List<MirrorObject>();

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
        originalPosition = rectTransform.anchoredPosition;
        originalParent = transform.parent;
        
        allMirrorObjects.Add(this);
        
        // 查找UI组件
        Transform parent = transform.parent;
        if (parent != null)
        {
            mirrorCountText = parent.Find("MirrorCount")?.GetComponent<TextMeshProUGUI>();
            resetButton = parent.Find("ResetButton")?.GetComponent<Button>();
        }
        mirrorImage = gameObject;
    }
    
    void Start()
    {
        UpdateMirrorCountDisplay();
        resetButton?.onClick.AddListener(OnResetButtonClicked);
    }
    
    /*
     * 更新镜子计数显示
     */
    private void UpdateMirrorCountDisplay()
    {
        if (mirrorCountText != null)
            mirrorCountText.text = mirrorCount.ToString();
        
        // 更新所有镜子的拖拽状态
        bool canDrag = mirrorCount > 0;
        foreach (var mirrorObj in allMirrorObjects)
        {
            if (mirrorObj?.canvasGroup != null)
            {
                mirrorObj.canvasGroup.blocksRaycasts = canDrag;
                mirrorObj.canvasGroup.interactable = canDrag;
            }
        }
    }
    
    /*
     * 重置按钮点击事件
     */
    private void OnResetButtonClicked()
    {
        mirrorCount = MAX_MIRROR_COUNT;
        UpdateMirrorCountDisplay();
        
        // 重置所有镜槽
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject obj in allObjects)
        {
            if (obj.name == "MirrorLine" && obj.layer == LayerMask.NameToLayer("Light"))
            {
                BoxCollider2D collider = obj.GetComponent<BoxCollider2D>();
                if (collider != null) collider.enabled = false;
                
                Image img = obj.GetComponent<Image>();
                if (img != null) img.color = originalMirrorColor;
            }
        }
        slotOccupancy.Clear();
        
        // 移除所有镜子的占用状态
        foreach (var mirrorObj in allMirrorObjects)
            mirrorObj?.RemoveFromMirrorSlot();
    }
    
    // ======拖拽事件实现======

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (mirrorCount <= 0) return;
        
        originalPosition = rectTransform.anchoredPosition;
        originalParent = transform.parent;
        CreateDraggedClone();
        canvasGroup.alpha = 0.6f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (draggedClone == null) return;
        
        RectTransform cloneRect = draggedClone.GetComponent<RectTransform>();
        cloneRect.anchoredPosition += eventData.delta / canvas.scaleFactor;
        UpdateNearestSlotHighlight();
    }

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
    
    private bool IsSlotOccupiedByOther(GameObject slot)
    {
        return slotOccupancy.ContainsKey(slot) && slotOccupancy[slot] != this;
    }
    
    private void OccupySlot(GameObject slot)
    {
        slotOccupancy[slot] = this;
    }
    
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
    
    private void ClearAllHighlights()
    {
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject obj in allObjects)
        {
            if (obj.name == "MirrorLine" && obj.layer == LayerMask.NameToLayer("Light"))
                ResetSlotColor(obj);
        }
        currentHighlightedSlot = null;
    }
    
    private void HighlightSlot(GameObject mirrorSlot)
    {
        Image img = mirrorSlot.GetComponent<Image>();
        if (img != null) img.color = Color.yellow;
    }
    
    private void ResetSlotColor(GameObject mirrorSlot)
    {
        Image img = mirrorSlot.GetComponent<Image>();
        if (img != null)
            img.color = slotOccupancy.ContainsKey(mirrorSlot) ? activeMirrorColor : originalMirrorColor;
    }

    // =====镜槽检测与激活实现======
    
    private GameObject FindNearestMirrorSlot()
    {
        if (draggedClone == null) return null;

        RectTransform cloneRect = draggedClone.GetComponent<RectTransform>();
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        GameObject nearest = null;
        float minDistance = maxDetectionDistance;

        foreach (GameObject obj in allObjects)
        {
            if (obj.name == "MirrorLine" && obj.layer == LayerMask.NameToLayer("Light"))
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

    private void ActivateMirrorSlot(GameObject mirrorSlot)
    {
        OccupySlot(mirrorSlot);
        
        BoxCollider2D collider = mirrorSlot.GetComponent<BoxCollider2D>();
        if (collider != null) collider.enabled = true;
        
        Image img = mirrorSlot.GetComponent<Image>();
        if (img != null) img.color = activeMirrorColor;

        currentMirrorSlot = mirrorSlot;
        mirrorCount--;
        UpdateMirrorCountDisplay();
    }

    private void DeactivateMirrorSlot(GameObject mirrorSlot)
    {
        if (mirrorSlot == null) return;
        
        BoxCollider2D collider = mirrorSlot.GetComponent<BoxCollider2D>();
        if (collider != null) collider.enabled = false;
        
        Image img = mirrorSlot.GetComponent<Image>();
        if (img != null) img.color = originalMirrorColor;
    }

    public void RemoveFromMirrorSlot()
    {
        if (currentMirrorSlot != null)
        {
            ReleaseSlot(currentMirrorSlot);
            DeactivateMirrorSlot(currentMirrorSlot);
            currentMirrorSlot = null;
        }
    }

    void OnDestroy()
    {
        allMirrorObjects.Remove(this);
        resetButton?.onClick.RemoveListener(OnResetButtonClicked);
        RemoveFromMirrorSlot();
        DestroyDraggedClone();
    }
}