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
        // 获取组件引用
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();

        // 记录原始信息
        originalPosition = rectTransform.anchoredPosition;
        originalParent = transform.parent;
        
        // 注册到静态列表
        if (!allMirrorObjects.Contains(this))
        {
            allMirrorObjects.Add(this);
        }
        
        // 查找UI组件(在同级Hierarchy下)
        FindUIComponents();
    }
    
    void Start()
    {
        // 初始化镜子计数显示
        UpdateMirrorCountDisplay();
        
        // 添加重置按钮监听
        if (resetButton != null)
        {
            resetButton.onClick.AddListener(OnResetButtonClicked);
        }
    }
    
    /*
     * 查找UI组件
     */
    private void FindUIComponents()
    {
        // 获取父对象
        Transform parent = transform.parent;
        if (parent == null) return;
        
        // 查找 MirrorCount (TextMeshProUGUI)
        Transform mirrorCountTransform = parent.Find("MirrorCount");
        if (mirrorCountTransform != null)
        {
            mirrorCountText = mirrorCountTransform.GetComponent<TextMeshProUGUI>();
            if (mirrorCountText != null)
            {
                Debug.Log("[MirrorObject] 找到 MirrorCount 文本组件");
            }
        }
        else
        {
            Debug.LogWarning("[MirrorObject] 未找到 MirrorCount 对象");
        }
        
        // 查找 ResetButton
        Transform resetButtonTransform = parent.Find("ResetButton");
        if (resetButtonTransform != null)
        {
            resetButton = resetButtonTransform.GetComponent<Button>();
            if (resetButton != null)
            {
                Debug.Log("[MirrorObject] 找到 ResetButton 组件");
            }
        }
        else
        {
            Debug.LogWarning("[MirrorObject] 未找到 ResetButton 对象");
        }
        
        // 查找 MirrorImage (本脚本挂载的对象)
        mirrorImage = gameObject;
    }
    
    /*
     * 更新镜子计数显示
     */
    private void UpdateMirrorCountDisplay()
    {
        if (mirrorCountText != null)
        {
            mirrorCountText.text = mirrorCount.ToString();
        }
        
        // 检查是否需要禁用拖拽
        UpdateDragEnabled();
    }
    
    /*
     * 更新拖拽启用状态
     */
    private void UpdateDragEnabled()
    {
        bool canDrag = mirrorCount > 0;
        
        // 更新所有 MirrorObject 实例的拖拽状态
        foreach (var mirrorObj in allMirrorObjects)
        {
            if (mirrorObj != null && mirrorObj.canvasGroup != null)
            {
                mirrorObj.canvasGroup.blocksRaycasts = canDrag;
                mirrorObj.canvasGroup.interactable = canDrag;
            }
        }
        
        if (!canDrag)
        {
            Debug.Log("[MirrorObject] 镜子计数为0，已禁用拖拽");
        }
    }
    
    /*
     * 重置按钮点击事件
     */
    private void OnResetButtonClicked()
    {
        Debug.Log("[MirrorObject] 重置按钮被点击");
        
        // 重置镜子计数
        mirrorCount = MAX_MIRROR_COUNT;
        UpdateMirrorCountDisplay();
        
        // 重置所有镜槽状态
        ResetAllMirrorSlots();
        
        // 移除所有镜子的占用状态
        foreach (var mirrorObj in allMirrorObjects)
        {
            if (mirrorObj != null)
            {
                mirrorObj.RemoveFromMirrorSlot();
            }
        }
        
        Debug.Log("[MirrorObject] 所有镜槽已重置，镜子计数已恢复为6");
    }
    
    /*
     * 重置所有镜槽状态
     */
    private void ResetAllMirrorSlots()
    {
        // 获取所有镜槽对象
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject obj in allObjects)
        {
            if (obj.name == "MirrorLine" && obj.layer == LayerMask.NameToLayer("Light"))
            {
                // 禁用碰撞器
                BoxCollider2D collider = obj.GetComponent<BoxCollider2D>();
                if (collider != null)
                {
                    collider.enabled = false;
                }
                
                // 恢复原始颜色
                Image mirrorImage = obj.GetComponent<Image>();
                if (mirrorImage != null)
                {
                    mirrorImage.color = originalMirrorColor;
                }
            }
        }
        
        // 清空占用字典
        slotOccupancy.Clear();
    }
    
    // ======拖拽事件实现======

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 检查是否还有可用的镜子计数
        if (mirrorCount <= 0)
        {
            Debug.Log("[MirrorObject] 镜子计数为0，无法拖拽");
            return;
        }
        
        Debug.Log("[MirrorObject] 开始拖拽镜子");
                
        // 记录当前位置用于返回
        originalPosition = rectTransform.anchoredPosition;
        originalParent = transform.parent;
        
        // 创建拖拽克隆
        CreateDraggedClone();
        
        // 设置原物体为半透明（可选）
        canvasGroup.alpha = 0.6f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (draggedClone == null) return;
        
        // 移动克隆对象
        RectTransform cloneRect = draggedClone.GetComponent<RectTransform>();
        cloneRect.anchoredPosition += eventData.delta / canvas.scaleFactor;
        
        // 实时检测最近的镜槽并高亮
        UpdateNearestSlotHighlight();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // 恢复原物体透明度
        canvasGroup.alpha = 1f;
        
        // 清除所有高亮
        ClearAllHighlights();
        
        // 检测是否放置到镜槽上
        GameObject mirrorSlot = FindNearestMirrorSlot();
        
        if (mirrorSlot != null)
        {
            Debug.Log($"[MirrorObject] 最终检测到镜槽: {mirrorSlot.name}");
            
            // 放置到镜槽上
            ActivateMirrorSlot(mirrorSlot);
            Debug.Log($"[MirrorObject] 镜子已放置到镜槽: {mirrorSlot.name}");
        }
        else
        {
            Debug.Log("[MirrorObject] 未检测到任何镜槽");
        }
        
        // 销毁拖拽克隆
        DestroyDraggedClone();
    }

    // ===== 镜槽占用管理方法 =====
    
    /*
     * 检查镜槽是否被其他镜子占用
     */
    private bool IsSlotOccupiedByOther(GameObject slot)
    {
        if (!slotOccupancy.ContainsKey(slot))
            return false;
        
        // 如果是当前镜子占用的，返回false（允许重复选择）
        return slotOccupancy[slot] != this;
    }
    
    /*
     * 占用镜槽
     */
    private void OccupySlot(GameObject slot)
    {
        slotOccupancy[slot] = this;
        Debug.Log($"[MirrorObject] 镜槽 {slot.name} 被镜子 {gameObject.name} 占用");
    }
    
    /*
     * 释放镜槽
     */
    private void ReleaseSlot(GameObject slot)
    {
        if (slotOccupancy.ContainsKey(slot) && slotOccupancy[slot] == this)
        {
            slotOccupancy.Remove(slot);
            Debug.Log($"[MirrorObject] 镜槽 {slot.name} 已被镜子 {gameObject.name} 释放");
        }
    }

    /*
     * 创建拖拽时的克隆对象
     */
    private void CreateDraggedClone()
    {
        if (canvas == null) return;
        
        // 克隆当前对象
        draggedClone = Instantiate(gameObject, canvas.transform);
        draggedClone.name = "DraggedMirror";
        
        // 设置位置和大小
        RectTransform cloneRect = draggedClone.GetComponent<RectTransform>();
        cloneRect.position = rectTransform.position;
        cloneRect.sizeDelta = rectTransform.sizeDelta;
        cloneRect.localScale = rectTransform.localScale;
        
        // 设置半透明
        CanvasGroup cloneCanvasGroup = draggedClone.GetComponent<CanvasGroup>();
        if (cloneCanvasGroup != null)
        {
            cloneCanvasGroup.alpha = 0.6f;
            cloneCanvasGroup.blocksRaycasts = false;
        }
        
        // 移除克隆上的拖拽脚本，避免递归
        MirrorObject cloneScript = draggedClone.GetComponent<MirrorObject>();
        if (cloneScript != null)
        {
            Destroy(cloneScript);
        }
        
        // 禁用克隆的射线检测
        Image cloneImage = draggedClone.GetComponent<Image>();
        if (cloneImage != null)
        {
            cloneImage.raycastTarget = false;
        }
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
     * 实时更新最近镜槽的高亮状态
     */
    private void UpdateNearestSlotHighlight()
    {
        GameObject nearestSlot = FindNearestMirrorSlot();
        
        // 如果检测到的最近镜槽发生变化
        if (nearestSlot != currentHighlightedSlot)
        {
            // 清除之前的高亮
            if (currentHighlightedSlot != null)
            {
                ResetSlotColor(currentHighlightedSlot);
            }
            
            // 高亮新的最近镜槽
            if (nearestSlot != null)
            {
                HighlightSlot(nearestSlot);
            }
            
            currentHighlightedSlot = nearestSlot;
        }
    }
    
    /*
     * 清除所有镜槽的高亮状态
     */
    private void ClearAllHighlights()
    {
        // 通过名称和Layer查找所有MirrorLine对象
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject obj in allObjects)
        {
            if (obj.name == "MirrorLine" && obj.layer == LayerMask.NameToLayer("Light"))
            {
                ResetSlotColor(obj);
            }
        }
        currentHighlightedSlot = null;
    }
    
    /*
     * 高亮镜槽
     */
    private void HighlightSlot(GameObject mirrorSlot)
    {
        Image mirrorImage = mirrorSlot.GetComponent<Image>();
        if (mirrorImage != null)
        {
            mirrorImage.color = Color.yellow; // 高亮颜色
        }
    }
    
    /*
     * 重置镜槽颜色
     */
    private void ResetSlotColor(GameObject mirrorSlot)
    {
        Image mirrorImage = mirrorSlot.GetComponent<Image>();
        if (mirrorImage != null)
        {
            // 检查是否是被占用的镜槽
            if (slotOccupancy.ContainsKey(mirrorSlot))
            {
                mirrorImage.color = activeMirrorColor;
            }
            else
            {
                mirrorImage.color = originalMirrorColor;
            }
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
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        GameObject nearest = null;
        float minDistance = maxDetectionDistance;

        foreach (GameObject obj in allObjects)
        {
            if (obj.name == "MirrorLine" && obj.layer == LayerMask.NameToLayer("Light"))
            {
                // 使用字典检查镜槽是否被其他镜子占用
                if (IsSlotOccupiedByOther(obj))
                {
                    Debug.Log($"[MirrorObject] 镜槽 {obj.name} 已被其他镜子占用，跳过");
                    continue;
                }

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
     * 激活镜槽
     */
    private void ActivateMirrorSlot(GameObject mirrorSlot)
    {        
        // 占用镜槽
        OccupySlot(mirrorSlot);
        
        // 启用新镜槽的 BoxCollider2D
        BoxCollider2D collider = mirrorSlot.GetComponent<BoxCollider2D>();
        if (collider != null)
        {
            collider.enabled = true;
            Debug.Log($"[MirrorObject] 镜槽 {mirrorSlot.name} 的碰撞器已启用");
        }
        
        // 改变镜槽颜色为蓝色
        Image mirrorImage = mirrorSlot.GetComponent<Image>();
        if (mirrorImage != null)
        {
            mirrorImage.color = activeMirrorColor;
            Debug.Log($"[MirrorObject] 镜槽 {mirrorSlot.name} 的颜色已改为蓝色");
        }

        // 记录当前占用的镜槽
        currentMirrorSlot = mirrorSlot;
        
        // 镜槽激活后减少镜子计数
        mirrorCount--;
        UpdateMirrorCountDisplay();
        Debug.Log($"[MirrorObject] 镜子计数减1，当前计数: {mirrorCount}");
        
    }

    /*
     * 取消激活镜槽
     */
    private void DeactivateMirrorSlot(GameObject mirrorSlot)
    {
        if (mirrorSlot == null) return;
        
        // 禁用镜槽的碰撞器
        BoxCollider2D collider = mirrorSlot.GetComponent<BoxCollider2D>();
        if (collider != null)
        {
            collider.enabled = false;
            Debug.Log($"[MirrorObject] 镜槽 {mirrorSlot.name} 的碰撞器已禁用");
        }
        
        // 恢复镜槽原始颜色
        Image mirrorImage = mirrorSlot.GetComponent<Image>();
        if (mirrorImage != null)
        {
            mirrorImage.color = originalMirrorColor;
            Debug.Log($"[MirrorObject] 镜槽 {mirrorSlot.name} 的颜色已恢复");
        }
    }

    /*
     * 从镜槽移除镜子
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

    void OnDestroy()
    {
        // 从静态列表中移除
        if (allMirrorObjects.Contains(this))
        {
            allMirrorObjects.Remove(this);
        }
        
        // 移除重置按钮监听
        if (resetButton != null)
        {
            resetButton.onClick.RemoveListener(OnResetButtonClicked);
        }
        
        // 销毁时移除镜子占用和拖拽克隆
        RemoveFromMirrorSlot();
        DestroyDraggedClone();
    }
}