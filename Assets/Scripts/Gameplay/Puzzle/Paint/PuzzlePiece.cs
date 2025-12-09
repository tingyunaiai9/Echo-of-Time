using UnityEngine;
using UnityEngine.EventSystems;
using Game.Gameplay.Puzzle.Paint2;

/*
 * 拼图碎片：可拖拽，靠近对应遮罩时自动消失
 */
[RequireComponent(typeof(CanvasGroup))]
public class PuzzlePiece : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [HideInInspector]
    public int pieceId;

    [HideInInspector]
    public PuzzleMask targetMask; // 对应的遮罩

    [Header("设置")]
    [Tooltip("吸附阈值（像素）")]
    public float snapThreshold = 100f;

    [Tooltip("返回原位的动画时长（秒）")]
    public float returnDuration = 0.3f;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Canvas canvas;
    private PuzzleManager puzzleManager;
    private Paint2PuzzleController paint2Manager;
    private bool isPlaced = false;
    private Vector2 originalPosition; // 记录初始位置
    private PuzzleMask currentHighlightedMask;


    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
        puzzleManager = GetComponentInParent<PuzzleManager>();
        paint2Manager = GetComponentInParent<Paint2PuzzleController>();
        
        originalPosition = rectTransform.anchoredPosition;
    }

    /* 开始拖拽 */
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isPlaced) return;

        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;
    }

    /* 拖拽中 */
    public void OnDrag(PointerEventData eventData)
    {
        if (isPlaced) return;

        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
        
        UpdateNearestMaskHighlight();
    }

    /* 结束拖拽 */
    public void OnEndDrag(PointerEventData eventData)
    {
        if (isPlaced) return;

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        // 结束拖拽时清除所有高光
        ClearAllHighlights();

        if (targetMask != null)
        {
            // 使用屏幕坐标（统一坐标系）
            Vector2 pieceScreenPos = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, rectTransform.position);
            Vector2 maskScreenPos = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, targetMask.GetComponent<RectTransform>().position);
            
            float distance = Vector2.Distance(pieceScreenPos, maskScreenPos);

            Debug.Log($"[PuzzlePiece] 碎片 {pieceId} 位置: {pieceScreenPos}, 遮罩位置: {maskScreenPos}, 距离: {distance}");

            if (distance < snapThreshold)
            {
                // 靠近成功，两者都消失
                isPlaced = true;
                targetMask.gameObject.SetActive(false); // 遮罩消失
                gameObject.SetActive(false);            // 碎片消失

                Debug.Log($"[PuzzlePiece] 碎片 {pieceId} 拼接成功！");

                // 通知管理器
                if (puzzleManager != null)
                {
                    puzzleManager.OnPieceCorrect(pieceId);
                }
                else if (paint2Manager != null)
                {
                    paint2Manager.OnPieceCorrect(pieceId);
                }
            }
            else
            {
                // 位置不对，返回原位
                ReturnToOriginalPosition();
            }
        }
        else
        {
            // 没有目标遮罩，返回原位
            ReturnToOriginalPosition();
        }
    }

    /* 实时更新最近 Mask 的高光 */
    private void UpdateNearestMaskHighlight()
    {
        PuzzleMask nearestMask = FindNearestMask();

        // 如果最近的 Mask 发生变化
        if (nearestMask != currentHighlightedMask)
        {
            // 清除之前的高光
            if (currentHighlightedMask != null)
            {
                currentHighlightedMask.HideHighlight();
            }

            // 高光新的 Mask
            if (nearestMask != null)
            {
                nearestMask.ShowHighlight();
            }

            currentHighlightedMask = nearestMask;
        }
    }

    /* 查找最近的 Mask */
    private PuzzleMask FindNearestMask()
    {
        PuzzleMask[] allMasks = FindObjectsByType<PuzzleMask>(FindObjectsSortMode.None);
        PuzzleMask nearest = null;
        float minDistance = snapThreshold;

        Vector2 pieceScreenPos = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, rectTransform.position);

        foreach (PuzzleMask mask in allMasks)
        {
            // 跳过已经消失的 Mask
            if (!mask.gameObject.activeSelf)
                continue;

            Vector2 maskScreenPos = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, mask.GetComponent<RectTransform>().position);
            float distance = Vector2.Distance(pieceScreenPos, maskScreenPos);

            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = mask;
            }
        }

        return nearest;
    }

    /* 清除所有高光 */
    private void ClearAllHighlights()
    {
        if (currentHighlightedMask != null)
        {
            currentHighlightedMask.HideHighlight();
            currentHighlightedMask = null;
        }
    }

    /* 回到原来位置 */
    private void ReturnToOriginalPosition()
    {
        Debug.Log($"[PuzzlePiece] 碎片 {pieceId} 位置不正确，返回原位");

        // 使用 LeanTween 播放平滑返回动画
        LeanTween.value(gameObject, rectTransform.anchoredPosition, originalPosition, returnDuration)
            .setOnUpdate((Vector2 val) =>
            {
                rectTransform.anchoredPosition = val;
            })
            .setEase(LeanTweenType.easeOutQuad); // 使用回弹效果
    }
}