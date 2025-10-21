using UnityEngine;
using Events;

public class prop : Interaction
{
    [Header("物品显示设置")]
    [Tooltip("物品图标，用于背包 UI 显示")]
    public Sprite itemIcon;

    [Tooltip("物品显示名称，留空则使用 GameObject 名称")]
    public string itemDisplayName;
    
    void Awake()
    {
        EventBus.Instance.Subscribe<ItemPickedUpEvent>(OnItemPickedUpEvent);
    }

    void OnDestroy()
    {
        EventBus.Instance.Unsubscribe<ItemPickedUpEvent>(OnItemPickedUpEvent);
    }

    private void OnItemPickedUpEvent(ItemPickedUpEvent evt)
    {
        // 关键修复：只处理自己的拾取事件，且避免重复处理
        if (evt.itemId == gameObject.name || evt.itemId == itemDisplayName)
        {
            if (gameObject.activeSelf)
            {
                Debug.Log($"[prop.OnItemPickedUpEvent] 收到拾取事件，物体将消失: {gameObject.name}");
                gameObject.SetActive(false); // 只消失，不再发布事件（避免循环）
            }
        }
    }


    // 覆盖交互：按 F 拾取后立刻消失并发布事件
    public override void OnInteract(PlayerController player)
    {
        Debug.Log($"[prop.OnInteract] 玩家 {(player != null ? player.gameObject.name : "Unknown")} 拾取物品: {gameObject.name}");

        uint pid = player != null ? player.netId : 0u;
        string displayId = string.IsNullOrEmpty(itemDisplayName) ? gameObject.name : itemDisplayName;

        // 先发布事件（让所有客户端的背包添加物品）
        var evt = new ItemPickedUpEvent
        {
            playerNetId = pid,
            itemId = displayId,
            icon = itemIcon
        };
        EventBus.Instance.Publish(evt);
        Debug.Log($"[prop.OnInteract] 已发布 ItemPickedUpEvent - itemId: {evt.itemId}, icon: {(evt.icon != null ? evt.icon.name : "null")}");
    }
}