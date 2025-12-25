using System;
using System.Collections.Generic;
using UnityEngine;

public class EventBus : Singleton<EventBus>
{
    // 存储所有事件类型的订阅者列表
    private readonly Dictionary<Type, List<Delegate>> _subscribers = new Dictionary<Type, List<Delegate>>();

    /* 订阅特定类型的事件（静态安全） */
    public static void Subscribe<T>(Action<T> handler)
    {
        if (Instance == null) return;

        var type = typeof(T);
        var subs = Instance._subscribers;
        if (!subs.ContainsKey(type))
        {
            subs[type] = new List<Delegate>();
        }
        if (!subs[type].Contains(handler))
        {
            subs[type].Add(handler);
        }
    }

    /* 取消事件订阅（静态安全） */
    public static void Unsubscribe<T>(Action<T> handler)
    {
        if (Instance == null) return;

        var type = typeof(T);
        var subs = Instance._subscribers;
        if (subs.ContainsKey(type))
        {
            subs[type].Remove(handler);
            if (subs[type].Count == 0)
            {
                subs.Remove(type);
            }
        }
    }

    /* 发布事件到所有订阅者，并进行网络同步（静态安全） */
    public static void Publish<T>(T eventData)
    {
        if (Instance == null) return;

        var type = typeof(T);

        // 判断是否为GameEvent（可根据实际需求调整类型判断）
        bool isGameEvent = type.Namespace == "Events";

        if (isGameEvent)
        {
            // 处理包含图片数据的事件，使用 ImageNetworkSender 分块发送
            if (ImageNetworkSender.LocalInstance != null)
            {
                // 检查是否为 ClueSharedEvent
                if (eventData is Events.ClueSharedEvent clueEvent)
                {
                    if (clueEvent.imageData != null && clueEvent.imageData.Length > 0)
                    {
                        ImageNetworkSender.LocalInstance.SendImage(
                            clueEvent.imageData, 
                            clueEvent.timeline, 
                            clueEvent.level, 
                            "Clue"
                        );
                        return; // 图片通过网络发送，不再走普通事件同步
                    }
                }
                // 检查是否为 ChatImageUpdatedEvent
                else if (eventData is Events.ChatImageUpdatedEvent chatImageEvent)
                {
                    if (chatImageEvent.imageData != null && chatImageEvent.imageData.Length > 0)
                    {
                        // 从事件中获取timeline，如果没有则使用本地玩家的timeline
                        int timeline = chatImageEvent.timeline;
                        int level = TimelinePlayer.Local != null ? TimelinePlayer.Local.currentLevel : 1;
                        ImageNetworkSender.LocalInstance.SendImage(
                            chatImageEvent.imageData, 
                            timeline, 
                            level, 
                            "Chat"
                        );
                        return; // 图片通过网络发送，不再走普通事件同步
                    }
                }
            }
            
            NetworkEventRelay.Instance.RelayGameEvent(eventData);
        }
        else
        {
            // LocalPublish(eventData);
            Debug.LogWarning($"EventBus: 事件类型 {type} 非游戏事件，未进行网络同步。");
        }
    }

    // 本地分发事件（静态安全）
    public static void LocalPublish<T>(T eventData)
    {
        if (Instance == null) return;

        var type = typeof(T);
        var subs = Instance._subscribers;
        if (subs.ContainsKey(type))
        {
            var handlers = new List<Delegate>(subs[type]);
            foreach (var handler in handlers)
            {
                try
                {
                    ((Action<T>)handler)?.Invoke(eventData);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"EventBus: 事件处理异常 {ex}");
                }
            }
        }
    }

    /* 动态分发（用于网络反序列化后分发，静态安全） */
    public static void PublishDynamic(Type type, object eventObj)
    {
        if (Instance == null) return;

        var subs = Instance._subscribers;
        if (subs.ContainsKey(type))
        {
            var handlers = new List<Delegate>(subs[type]);
            foreach (var handler in handlers)
            {
                try
                {
                    handler.DynamicInvoke(eventObj);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"EventBus: 动态事件处理异常 {ex}");
                }
            }
        }
    }

    // -------------------------------------------------------------------
    // --- STATIC (静态) 安全包装方法 ---
    // -------------------------------------------------------------------

    /// <summary>
    /// [静态安全] 订阅事件。
    /// 自动处理实例为 null 的情况。
    /// </summary>
    // public static void SafeSubscribe<T>(Action<T> handler)
    // {
    //     if (Instance != null)
    //     {
    //         Instance.Subscribe(handler);
    //     }
    // }

    // /// <summary>
    // /// [静态安全] 取消订阅事件。
    // /// 这将自动处理在 OnDestroy() 中调用时 Instance 为 null 的情况。
    // /// </summary>
    // public static void SafeUnsubscribe<T>(Action<T> handler)
    // {
    //     if (Instance != null)
    //     {
    //         Instance.Unsubscribe(handler);
    //     }
    // }

    // /// <summary>
    // /// [静态安全] 发布网络事件。
    // /// </summary>
    // public static void SafePublish<T>(T eventData)
    // {
    //     if (Instance != null)
    //     {
    //         Instance.Publish(eventData);
    //     }
    // }

    // /// <summary>
    // /// [静态安全] 发布本地事件。
    // /// </summary>
    // public static void SafeLocalPublish<T>(T eventData)
    // {
    //     if (Instance != null)
    //     {
    //         Instance.LocalPublish(eventData);
    //     }
    // }
}