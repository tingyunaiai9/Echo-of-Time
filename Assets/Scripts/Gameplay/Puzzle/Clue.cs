using UnityEngine;
using Events;
using Game.UI;

/*
 * 调查类：调查线索，仅反馈信息，不会消失
 */
public class Clue : Interaction
{
    [Header("线索内容")]
    [Tooltip("线索唯一ID")]
    public int clueID;
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

            if (clueID == 5) // 如果是天干线索，添加日记共享文字线索
            {
                ClueBoard.AddClueEntry(TimelinePlayer.Local.timeline, TimelinePlayer.Local.currentLevel, clueDescription);
            }
            else if (clueID == 2) // 如果是罗盘线索，添加日记共享图片线索
            {
                ClueBoard.AddClueEntry(TimelinePlayer.Local.timeline, TimelinePlayer.Local.currentLevel, ImageUtils.CompressSpriteToJpegBytes(clueImage, 80));
            }
            else if (clueID == 1 || clueID == 3) // 如果是手绢或者便签，添加到日记关键线索当中
            {
                EventBus.LocalPublish(new ClueDiscoveredEvent
                {
                    isKeyClue = true,
                    playerNetId = pid,
                    clueId = gameObject.name,
                    clueText = clueText,
                    clueDescription = clueDescription,
                    icon = clueIcon,
                    image = clueImage
                });
            }
            else // 如果当前是ASCII对照表线索或者第三层当中的线索，添加至背包当中
            {
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
        }
        UIManager.Instance.SetFrozen(true);
        // 查找并显示 ClueCanvas
        GameObject canvasObj = GameObject.Find("ClueCanvas");
        ClueCanvas canvas = canvasObj != null ? canvasObj.GetComponent<ClueCanvas>() : null;

        if (canvas != null)
        {
            canvas.ShowClue(clueText, clueImage, clueDescription);
        }
        else
        {
            Debug.LogWarning("ClueCanvas not found in the scene!");
        }

        Debug.Log($"调查线索 -> 对象: {gameObject.name}, 玩家: {who}\n内容: {clueText}");
    }
}