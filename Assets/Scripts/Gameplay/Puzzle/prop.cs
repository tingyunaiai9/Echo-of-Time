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
    [Header("物品显示设置")]
    [Tooltip("物品图标，用于背包 UI 显示")]
    public Sprite itemIcon;

    [Tooltip("物品显示名称，留空则使用 GameObject 名称")]
    public string itemDisplayName;
    /* 实现物体立刻消失的函数，并发布拾取事件 */
    public void DisappearImmediately(uint playerNetId = 0, string itemId = null)
    {
        gameObject.SetActive(false);
        Debug.Log($"Prop '{gameObject.name}' has immediately disappeared.");

        // 发布物品拾取事件
        var evt = new ItemPickedUpEvent
        {
            playerNetId = playerNetId,
            itemId = itemId ?? gameObject.name,
            icon = itemIcon
        };
        EventBus.Instance.Publish(evt);
        Debug.Log($"[prop] 发布 ItemPickedUpEvent - itemId: {evt.itemId}, icon: {(evt.icon != null ? evt.icon.name : "null")}");
    }

    // 覆盖交互：按 F 拾取后立刻消失
    public override void OnInteract(PlayerController player)
    {
        uint pid = player != null ? player.netId : 0u;
        DisappearImmediately(pid, string.IsNullOrEmpty(itemDisplayName) ? gameObject.name : itemDisplayName);
    }
    
}