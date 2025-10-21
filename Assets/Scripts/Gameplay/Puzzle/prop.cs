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
        if (evt.itemId == gameObject.name)
        {
            if (gameObject.activeSelf)
            {
                Disappear(evt.playerNetId, evt.itemId);
            }
        }
    }

    public void InitializeInteraction(InteractionConfig config)
    {
        // 设置交互参数
    }

    public void Disappear(uint playerNetId = 0, string itemId = null)
    {
        gameObject.SetActive(false);
        Debug.Log($"Prop '{gameObject.name}' has immediately disappeared.");

        var evt = new ItemPickedUpEvent
        {
            playerNetId = playerNetId,
            itemId = itemId ?? gameObject.name,
            icon = itemIcon
        };
        EventBus.Instance.Publish(evt);
        Debug.Log($"[prop] 发布 ItemPickedUpEvent - itemId: {evt.itemId}, icon: {(evt.icon != null ? evt.icon.name : "null")}");
    }

    public override void OnInteract(PlayerController player)
    {
        uint pid = player != null ? player.netId : 0u;
        Disappear(pid, string.IsNullOrEmpty(itemDisplayName) ? gameObject.name : itemDisplayName);
    }
}