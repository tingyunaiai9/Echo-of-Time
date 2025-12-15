using UnityEngine;
using Events;

/*
 * 调查类：调查线索，仅反馈信息，不会消失
 */
public class Clue : Interaction
{
    [Header("线索内容")]
    [TextArea(2, 4)]
    [Tooltip("线索简短文本，显示在物品栏")]
    public string clueText;

    [Header("线索详细描述")]
    [TextArea(4, 8)]
    [Tooltip("线索详细描述，显示在右侧详情栏")]
    public string clueDescription = "这是一条重要的线索...";

    [Tooltip("线索图标，用于背包 UI 显示")]
    public Sprite clueIcon;

    [Tooltip("线索对应的大图（在详情栏右下角按钮查看）")]
    public Sprite clueImage;

    [Tooltip("是否已被调查过")]
    public bool discovered;

    /* 覆盖交互：调查线索，发布线索发现事件 */
    public override void OnInteract(PlayerController player)
    {
        if (!CheckPuzzleConditions()) return;

        string who = player != null ? player.gameObject.name : "Unknown";
        uint pid = player != null ? player.netId : 0u;

        // 标记调查
        if (!discovered)
        {
            discovered = true;

            EventBus.LocalPublish(new ClueDiscoveredEvent
            {
                playerNetId = pid,
                clueId = gameObject.name,
                clueText = clueText,
                clueDescription = clueDescription,
                icon = clueIcon,
                image = clueImage
            });
        }

        // 查找并显示 ClueCanvas
        GameObject canvasObj = GameObject.Find("ClueCanvas");
        ClueCanvas canvas = canvasObj != null ? canvasObj.GetComponent<ClueCanvas>() : null;

        if (canvas != null)
        {
            canvas.ShowClue(clueImage, clueDescription);
            
            Sprite sprite = clueImage;
            int timeline = TimelinePlayer.Local.timeline;
            // 压缩图片，避免过大
            byte[] spriteBytes = ImageUtils.CompressSpriteToJpegBytes(sprite, 80);
            Debug.Log($"[UIManager] 线索图片压缩成功，大小：{spriteBytes.Length} 字节");
            ClueBoard.AddClueEntry(timeline, spriteBytes);
        }
        else
        {
            Debug.LogWarning("ClueCanvas not found in the scene!");
        }

        Debug.Log($"调查线索 -> 对象: {gameObject.name}, 玩家: {who}\n内容: {clueText}");
    }
}