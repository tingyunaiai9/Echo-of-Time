using UnityEngine;
using Events;

public class prop : Interaction
{
    [Header("物品显示设置")]
    [Tooltip("物品图标，用于背包 UI 显示")]
    public Sprite itemIcon;

    [Tooltip("物品显示名称，留空则使用 GameObject 名称")]
    public string itemDisplayName;

    [Tooltip("拾取时获得的数量")]
    public int pickupQuantity = 1;

    [Header("物品描述")]
    [TextArea(3, 6)]
    [Tooltip("物品详细描述，显示在右侧详情栏")]
    public string itemDescription = "this is a magic prop...";

    private int instanceId; // 唯一实例标识

    /* 订阅拾取事件 */
    void Awake()
    {
        instanceId = GetInstanceID(); // 获取 Unity 内部的唯一 ID
        EventBus.Subscribe<ItemPickedUpEvent>(OnItemPickedUpEvent);
    }

    /* 在销毁时取消订阅 */
    void OnDestroy()
    {
        EventBus.Unsubscribe<ItemPickedUpEvent>(OnItemPickedUpEvent);
    }

    /* 处理拾取事件：如果是自己被拾取则隐藏物体 */
    private void OnItemPickedUpEvent(ItemPickedUpEvent evt)
    {
        if (evt.instanceId == instanceId && gameObject.activeSelf)
        {
            Debug.Log($"[prop.OnItemPickedUpEvent] 收到拾取事件，物体将消失: {gameObject.name}");
            gameObject.SetActive(false);
        }
    }

    /* 覆盖交互：按 F 拾取后立刻消失并发布事件 */
    public override void OnInteract(PlayerController player)
    {
        Debug.Log($"[prop.OnInteract] 玩家 {(player != null ? player.gameObject.name : "Unknown")} 拾取物品: {gameObject.name}");

        uint pid = player != null ? player.netId : 0u;
        string displayId = string.IsNullOrEmpty(itemDisplayName) ? gameObject.name : itemDisplayName;

        // 发布事件
        var evt = new ItemPickedUpEvent
        {
            playerNetId = pid,
            itemId = displayId,
            instanceId = instanceId, // 携带实例 ID
            icon = itemIcon,
            description = itemDescription,
            quantity = pickupQuantity
        };
        EventBus.LocalPublish(evt);
        EventBus.Publish(evt);
        Debug.Log($"[prop.OnInteract] 已发布 ItemPickedUpEvent - itemId: {evt.itemId}, instanceId: {evt.instanceId}, quantity: {evt.quantity}, icon: {(evt.icon != null ? evt.icon.name : "null")}");
    }
}