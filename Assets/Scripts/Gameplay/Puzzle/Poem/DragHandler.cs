using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/*
 * 小纸条拖拽处理脚本
 * 实现拖拽功能，并在放置到正确的大纸条时触发匹配逻辑
 */
[RequireComponent(typeof(CanvasGroup))]
public class DragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("配置")]
    public string correctLargeNoteId;

    [Header("引用")]
    public TMP_Text poemText;

    [Header("检测配置")]
    [Tooltip("最大检测距离（像素）")]
    public float maxDetectionDistance = 150f;

    private Canvas canvas;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 originalPosition;
    private Transform originalParent;

    // ⭐ 记录当前高亮的大纸条
    private LargeNoteSlot currentHighlightedSlot = null;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
        poemText = GetComponentInChildren<TMP_Text>();
        
        originalPosition = rectTransform.anchoredPosition;
        originalParent = transform.parent;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log($"[DragHandler] 开始拖拽: {gameObject.name}");

        originalPosition = rectTransform.anchoredPosition;
        originalParent = transform.parent;

        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // 移动小纸条
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;

        // ⭐ 实时检测最近的大纸条并高亮
        UpdateNearestSlotHighlight();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        // ⭐ 清除所有高亮
        ClearAllHighlights();

        // 检测放置位置
        LargeNoteSlot targetSlot = FindNearestLargeNote();

        if (targetSlot != null)
        {
            Debug.Log($"[DragHandler] 最终检测到大纸条: {targetSlot.noteId}");
            
            if (targetSlot.noteId == correctLargeNoteId)
            {
                Debug.Log($"[DragHandler] 匹配成功！");
                OnCorrectMatch(targetSlot);
            }
            else
            {
                Debug.Log($"[DragHandler] 匹配失败");
                ReturnToOriginalPosition();
            }
        }
        else
        {
            Debug.Log("[DragHandler] 未检测到任何大纸条");
            ReturnToOriginalPosition();
        }
    }

    /*
     * ⭐ 实时更新最近大纸条的高亮状态
     */
    private void UpdateNearestSlotHighlight()
    {
        LargeNoteSlot nearestSlot = FindNearestLargeNote();

        // 如果检测到的最近纸条发生变化
        if (nearestSlot != currentHighlightedSlot)
        {
            // 清除之前的高亮
            if (currentHighlightedSlot != null)
            {
                currentHighlightedSlot.ResetBorder();
            }

            // 高亮新的最近纸条
            if (nearestSlot != null)
            {
                nearestSlot.HighlightBorder();
            }

            currentHighlightedSlot = nearestSlot;
        }
    }

    /*
     * ⭐ 清除所有大纸条的高亮状态
     */
    private void ClearAllHighlights()
    {
        LargeNoteSlot[] allSlots = FindObjectsByType<LargeNoteSlot>(FindObjectsSortMode.None);
        foreach (LargeNoteSlot slot in allSlots)
        {
            slot.ResetBorder();
        }
        currentHighlightedSlot = null;
    }

    private LargeNoteSlot FindNearestLargeNote()
    {
        LargeNoteSlot[] allSlots = FindObjectsByType<LargeNoteSlot>(FindObjectsSortMode.None);
        LargeNoteSlot nearest = null;
        float minDistance = maxDetectionDistance;

        foreach (LargeNoteSlot slot in allSlots)
        {
            // ⭐ 跳过已填充的大纸条
            if (slot.IsFilled())
                continue;

            float distance = Vector2.Distance(rectTransform.position, slot.GetComponent<RectTransform>().position);

            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = slot;
            }
        }

        return nearest;
    }

    private void OnCorrectMatch(LargeNoteSlot largeNote)
    {
        // ⭐ 设置匹配成功的绿色边框
        largeNote.SetMatchedBorder();

        // 将诗句文本传递给大纸条
        largeNote.SetPoemText(poemText.text);

        // 隐藏小纸条
        gameObject.SetActive(false);

        // 通知谜题管理器
        BasePoemManager puzzleManager = FindFirstObjectByType<BasePoemManager>();
        if (puzzleManager != null)
        {
            puzzleManager.OnNoteMatched();
        }
    }

    private void ReturnToOriginalPosition()
    {
        LeanTween.value(gameObject, rectTransform.anchoredPosition, originalPosition, 0.3f)
            .setOnUpdate((Vector2 val) => {
                rectTransform.anchoredPosition = val;
            })
            .setEase(LeanTweenType.easeOutBack);
    }

    public string GetPoemText()
    {
        return poemText != null ? poemText.text : "";
    }
}