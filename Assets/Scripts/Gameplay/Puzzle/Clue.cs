using UnityEngine;
using Events;
namespace Events
{
    // 线索被发现时发布
    public class ClueDiscoveredEvent
    {
        public uint playerNetId;
        public string clueId;
        public string clueText;
    }

    // 备注：prop.cs 已使用的 ItemPickedUpEvent 与 EventBus 已存在于工程中，无需重复定义
}

/*
 * 调查类：调查线索，仅反馈信息，不会消失
 */
public class Clue : Interaction
{
    [Header("线索内容")]
    [TextArea]
    public string clueText;

    [Tooltip("是否已被调查过")]
    public bool discovered;

    // 按 F 调查，不消失
    public override void OnInteract(PlayerController player)
    {
        if (!CheckPuzzleConditions()) return;

        string who = player != null ? player.gameObject.name : "Unknown";
        uint pid = player != null ? player.netId : 0u;

        // 标记调查
        if (!discovered)
        {
            discovered = true;

            // 发布“线索发现”事件（UI 将去重接收）
            EventBus.Instance.Publish(new ClueDiscoveredEvent
            {
                playerNetId = pid,
                clueId = gameObject.name, // 以物体名作为唯一ID
                clueText = clueText
            });
        }

        Debug.Log($"调查线索 -> 对象: {gameObject.name}, 玩家: {who}\n内容: {clueText}");
    }
}