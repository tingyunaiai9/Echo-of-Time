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
    // 配置
    [Header("配置")]
    // 该小纸条对应的正确大纸条ID
    public string correctLargeNoteId;

    // 引用
    [Header("引用")]
    // 小纸条上的诗句文本组件
    public TMP_Text poemText;

    // 私有变量
    private Canvas canvas;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 originalPosition;
    private Transform originalParent;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();

        // 自动查找子对象中的文本组件
        if (poemText == null)
        {
            poemText = GetComponentInChildren<TMP_Text>();
        }

        // 保存初始位置和父对象
        originalPosition = rectTransform.anchoredPosition;
        originalParent = transform.parent;
    }

    /*
     * 开始拖拽时调用
     */
    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log($"[DragHandler] 开始拖拽: {gameObject.name}");

        // 保存当前位置
        originalPosition = rectTransform.anchoredPosition;
        originalParent = transform.parent;

        // 设置为半透明并禁用射线检测（让小纸条可以穿透到大纸条）
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;
    }

    /*
     * 拖拽过程中持续调用
     */
    public void OnDrag(PointerEventData eventData)
    {
        // 根据鼠标位置移动小纸条
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    /*
     * 结束拖拽时调用，检测放置位置
     */
    public void OnEndDrag(PointerEventData eventData)
    {
        // 恢复透明度和射线检测
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        // 检测放置位置
        LargeNoteSlot targetSlot = null;

        if (targetSlot == null)
        {
            targetSlot = FindNearestLargeNote();
        }

        // 处理检测结果
        if (targetSlot != null)
        {
            Debug.Log($"[DragHandler] 最终检测到大纸条: {targetSlot.noteId}");
            
            // 检查是否匹配
            if (targetSlot.noteId == correctLargeNoteId)
            {
                Debug.Log($"[DragHandler] 匹配成功！");
                OnCorrectMatch(targetSlot);
            }
            else
            {
                Debug.Log($"[DragHandler] 匹配失败，期望: {correctLargeNoteId}, 实际: {targetSlot.noteId}");
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
     * 通过距离检测查找最近的大纸条
     */
    private LargeNoteSlot FindNearestLargeNote()
    {
        LargeNoteSlot[] allSlots = FindObjectsByType<LargeNoteSlot>(FindObjectsSortMode.None);
        LargeNoteSlot nearest = null;
        float minDistance = 150f; // 最大检测距离

        foreach (LargeNoteSlot slot in allSlots)
        {
            float distance = Vector2.Distance(rectTransform.position, slot.GetComponent<RectTransform>().position);

            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = slot;
            }
        }

        if (nearest != null)
        {
            Debug.Log($"[DragHandler] 距离检测到最近的大纸条: {nearest.noteId}, 距离: {minDistance}");
        }

        return nearest;
    }
    
    /*
     * 匹配成功时的处理
     */
    private void OnCorrectMatch(LargeNoteSlot largeNote)
    {
        // 将诗句文本传递给大纸条
        largeNote.SetPoemText(poemText.text);

        // 隐藏小纸条
        gameObject.SetActive(false);

        // 通知谜题管理器
        PoemManager puzzleManager = FindFirstObjectByType<PoemManager>();
        if (puzzleManager != null)
        {
            puzzleManager.OnNoteMatched();
        }
    }

    /*
     * 返回原始位置
     */
    private void ReturnToOriginalPosition()
    {
        rectTransform.anchoredPosition = originalPosition;
        // 可选：添加回弹动画
        // LeanTween.move(rectTransform, originalPosition, 0.3f).setEase(LeanTweenType.easeOutBack);
    }
}