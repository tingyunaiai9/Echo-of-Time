using UnityEngine;
using System.Collections.Generic;

/*
 * 拼画管理器：管理遮罩和碎片的绑定，处理完成逻辑
 * 前提：遮罩和碎片已在 Unity 中手动布局好
 */
public class PuzzleManager : MonoBehaviour
{
    [Header("容器（自动查找子对象）")]
    [Tooltip("遮罩容器（PuzzleMaskGroup）")]
    public Transform maskContainer;

    [Tooltip("碎片容器（PieceContainer）")]
    public Transform pieceContainer;

    [Header("完成事件")]
    [Tooltip("拼图完成后的回调")]
    public UnityEngine.Events.UnityEvent onPuzzleComplete;

    // 存储遮罩映射
    private Dictionary<int, PuzzleMask> masks = new Dictionary<int, PuzzleMask>();
    private int correctPieces = 0;
    private int totalPieces;

    void Start()
    {
        InitializeMasks();
        InitializePieces();
        
        Debug.Log($"[PuzzleManager] 初始化完成，共 {totalPieces} 块碎片");
    }

    /* 初始化遮罩 */
    void InitializeMasks()
    {
        PuzzleMask[] maskArray = maskContainer.GetComponentsInChildren<PuzzleMask>();
        
        foreach (PuzzleMask mask in maskArray)
        {
            string name = mask.gameObject.name;
            if (int.TryParse(name.Replace("PuzzleMask", ""), out int id))
            {
                mask.maskId = id;
                masks[id] = mask;
            }
            else
            {
                Debug.LogWarning($"[PuzzleManager] 遮罩命名不规范: {name}");
            }
        }

        totalPieces = masks.Count;
        Debug.Log($"[PuzzleManager] 已加载 {totalPieces} 个遮罩");
    }

    /* 初始化碎片（只设置 ID 和对应遮罩引用） */
    void InitializePieces()
    {
        PuzzlePiece[] pieceArray = pieceContainer.GetComponentsInChildren<PuzzlePiece>();
        
        foreach (PuzzlePiece piece in pieceArray)
        {
            string name = piece.gameObject.name;
            if (int.TryParse(name.Replace("PuzzlePiece", ""), out int id))
            {
                piece.pieceId = id;
                
                // 将对应遮罩引用传给碎片
                if (masks.TryGetValue(id, out PuzzleMask mask))
                {
                    piece.targetMask = mask;
                }
                else
                {
                    Debug.LogWarning($"[PuzzleManager] 碎片 {id} 找不到对应遮罩");
                }
            }
            else
            {
                Debug.LogWarning($"[PuzzleManager] 碎片命名不规范: {name}");
            }
        }
    }

    /* 碎片拼接成功回调 */
    public void OnPieceCorrect(int pieceId)
    {
        correctPieces++;
        Debug.Log($"[PuzzleManager] 碎片 {pieceId} 拼接成功，进度: {correctPieces}/{totalPieces}");

        // 检查是否完成
        if (correctPieces >= totalPieces)
        {
            OnPuzzleComplete();
        }
    }

    /* 拼图完成 */
    void OnPuzzleComplete()
    {
        Debug.Log("🎉 [PuzzleManager] 拼图完成！");
        onPuzzleComplete?.Invoke();
    }

    /* 获取进度 */
    public float GetProgress()
    {
        return (float)correctPieces / totalPieces;
    }
}