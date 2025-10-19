/* Gameplay/Puzzle/prop.cs
 * 交互系统基类，定义游戏内可交互对象的通用行为
 * 处理玩家与场景物体的互动逻辑
 */
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
        // 订阅物品拾取事件
        EventBus.Instance.Subscribe<ItemPickedUpEvent>(OnItemPickedUpEvent);
    }

    void OnDestroy()
    {
        // 取消订阅，防止内存泄漏
        EventBus.Instance.Unsubscribe<ItemPickedUpEvent>(OnItemPickedUpEvent);
    }

    // 事件回调：收到ItemPickedUpEvent后判断是否为本物体，若是则消失
    private void OnItemPickedUpEvent(ItemPickedUpEvent evt)
    {
        if (evt.itemId == gameObject.name)
        {
            // 避免重复消失
            if (gameObject.activeSelf)
            {
                Disappear(evt.playerNetId, evt.itemId);
            }
        }
    }

    public void InitializeInteraction(InteractionConfig config)
    {
        // 设置交互参数
        // 绑定触发条件
        // 初始化状态
    }

    public void Disappear(uint playerNetId = 0, string itemId = null)
    {
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

    public void ResetInteraction()
    {
        gameObject.SetActive(true);
    }
}