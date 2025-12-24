using UnityEngine;
using System.Collections.Generic;
using Events;

/*
 * 拼画管理器：管理遮罩和碎片的绑定，处理完成逻辑
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

    [Header("关联组件")]
    [Tooltip("拼画面板管理器，用于显示成功反馈")]
    public PuzzlePanel puzzlePanel;

    [Tooltip("拼图完成后要打开的 ConsolePanel 面板（在 Canvas 下）")]
    public GameObject consolePanel;

    [Header("线索设置")]
    [Tooltip("拼图完成后获得的线索图片")]
    public Sprite clueSprite;

    [Header("共享线索设置")]
    [Tooltip("拼图完成后获得的共享线索图片")]
    public Sprite sharedClueSprite;

    [Tooltip("线索的名字")]
    public string clueName = "一幅画";

    [Tooltip("线索的描述")]
    public string clueDescription = "一幅画，上面还印着一行字，远眺……？";

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
        
        // 显示成功面板（如果已配置）
        if (puzzlePanel != null)
        {
            puzzlePanel.ShowSuccessPanel();
        }

        // 打开 ConsolePanel（如果已绑定）
        if (consolePanel != null)
        {
            consolePanel.SetActive(true);
            Debug.Log("[PuzzleManager] 已打开 ConsolePanel");
        }


        EventBus.LocalPublish(new PuzzleCompletedEvent
        {
            sceneName = "Paint"
        });

        // 发布 ClueDiscoveredEvent 事件
        if (clueSprite != null && !string.IsNullOrEmpty(clueName))
        {
            EventBus.LocalPublish(new ClueDiscoveredEvent
            {
                isKeyClue = true,
                playerNetId = 0, // 本地事件，此字段可能无影响
                clueId = clueName, // 使用线索名作为唯一ID
                clueText = clueName,
                clueDescription = clueDescription,
                icon = clueSprite,
                image = clueSprite // 大图和小图使用同一个
            });
            Debug.Log($"[PuzzleManager] 已发布线索发现事件: {clueName}");
        }
        else
        {
            Debug.LogWarning("[PuzzleManager] 未设置线索图片或名称，无法发布线索。");
        }

        // 共享图片线索到便签墙（参考 UIManager Minus 键流程）
        if (sharedClueSprite != null)
        {
            int timeline = TimelinePlayer.Local != null ? TimelinePlayer.Local.timeline : 0;
            byte[] spriteBytes = ImageUtils.CompressSpriteToJpegBytes(sharedClueSprite, 80);
            if (spriteBytes != null)
            {
                ClueBoard.AddClueEntry(timeline, spriteBytes);
                Debug.Log($"[PuzzleManager] 已共享线索图片到便签墙，大小：{spriteBytes.Length} 字节");
            }
            else
            {
                Debug.LogError("[PuzzleManager] 线索图片压缩失败，未能共享到便签墙。");
            }
        }
        // 打开控制台面板
        ConsolePanel.TogglePanel();
    }

    /* 获取进度 */
    public float GetProgress()
    {
        return (float)correctPieces / totalPieces;
    }
}