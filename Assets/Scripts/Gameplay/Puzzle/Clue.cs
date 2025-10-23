using UnityEngine;
using Events;

/*
 * 调查类：调查线索，仅反馈信息，不会消失
 */
public class Clue : Interaction
{
    [Header("线索内容")]
    [TextArea]
    public string clueText;

    [Tooltip("线索图标，用于背包 UI 显示")]
    public Sprite clueIcon;

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

            EventBus.Instance.LocalPublish(new ClueDiscoveredEvent
            {
                playerNetId = pid,
                clueId = gameObject.name, // 以物体名作为唯一ID
                clueText = clueText,
                icon = clueIcon
            });
            // 发布“线索发现”事件（UI 将去重接收）
            // EventBus.Instance.Publish(new ClueDiscoveredEvent
            // {
            //     playerNetId = pid,
            //     clueId = gameObject.name, // 以物体名作为唯一ID
            //     clueText = clueText,
            //     icon = clueIcon
            // });


        }

        Debug.Log($"调查线索 -> 对象: {gameObject.name}, 玩家: {who}\n内容: {clueText}");
    }
}