using UnityEngine;
using Events;

public class prop : MonoBehaviour
{
    public class InteractionConfig
    {
        // 在此添加交互配置参数，例如 public int someParameter;
    }

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
            itemId = itemId ?? gameObject.name
        };
        EventBus.Instance.Publish(evt);
    }

    public void ResetInteraction()
    {
        gameObject.SetActive(true);
    }
}