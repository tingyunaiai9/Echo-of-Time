/* Gameplay/Puzzle/prop.cs
 * 交互系统基类，定义游戏内可交互对象的通用行为
 * 处理玩家与场景物体的互动逻辑
 */
using UnityEngine;
using Events;

/*
 * 交互系统基类，定义可交互对象的通用行为
 */
public class prop : Interaction
{
    /* 实现物体立刻消失的函数，并发布拾取事件 */
    public void DisappearImmediately(uint playerNetId = 0, string itemId = null)
    {
        // 通过设置GameObject为非激活状态，使其在场景中立刻消失
        gameObject.SetActive(false);
        Debug.Log($"Prop '{gameObject.name}' has immediately disappeared.");

        // 发布物品拾取事件
        var evt = new ItemPickedUpEvent
        {
            playerNetId = playerNetId,
            itemId = itemId ?? gameObject.name
        };
        EventBus.Instance.Publish(evt);
    }

    // 覆盖交互：按 F 拾取后立刻消失
    public override void OnInteract(PlayerController player)
    {
        uint pid = player != null ? player.netId : 0u;
        DisappearImmediately(pid, gameObject.name);
    }
    
}