/* Gameplay/Puzzle/prop.cs
 * 交互系统基类，定义游戏内可交互对象的通用行为
 * 处理玩家与场景物体的互动逻辑
 */
using UnityEngine;
using Events;

/*
 * 交互系统基类，定义可交互对象的通用行为
 */
public class prop : MonoBehaviour
{
    /* 交互配置类定义 */
    public class InteractionConfig
    {
        // 在此添加交互配置参数，例如 public int someParameter;
    }

    /* 初始化交互配置 */
    public void InitializeInteraction(InteractionConfig config)
    {
        // 设置交互参数
        // 绑定触发条件
        // 初始化状态
    }

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


    /* 重置交互状态 */
    public void ResetInteraction()
    {
        // 恢复初始状态
        // 清除临时数据
        // 重置动画效果
        gameObject.SetActive(true); // 示例：重置时重新激活对象
    }
}