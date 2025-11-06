using UnityEngine;
using UnityEngine.EventSystems;

/*
 * 拼图碎片：可拖拽，靠近对应遮罩时自动消失
 */
[RequireComponent(typeof(CanvasGroup))]
public class PuzzlePiece : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [HideInInspector]
    public int pieceId;

    [HideInInspector]
    public PuzzleMask targetMask; // 对应的遮罩（由 PuzzleManager 设置）

    [Header("设置")]
    [Tooltip("吸附阈值（像素）")]
    public float snapThreshold = 100f;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Canvas canvas;
    private PuzzleManager puzzleManager;
    private bool isPlaced = false;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
        puzzleManager = GetComponentInParent<PuzzleManager>();
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
    }

    /* 结束拖拽 */
    public void OnEndDrag(PointerEventData eventData)
    {
        if (isPlaced) return;

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        // 检查是否靠近对应遮罩
        if (targetMask != null)
        {
            float distance = Vector2.Distance(rectTransform.position, targetMask.GetComponent<RectTransform>().position);

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
            }
        }
    }
}