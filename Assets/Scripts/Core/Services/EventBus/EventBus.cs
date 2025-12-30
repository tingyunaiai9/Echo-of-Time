// EventBus.cs
// 事件总线类，用于管理事件的订阅、取消订阅和发布。
// 支持本地事件分发和网络事件同步。

using System;
using System.Collections.Generic;
using UnityEngine;

/*
 * EventBus 类
 * 事件总线的单例实现，提供事件的订阅、取消订阅和发布功能。
 * 支持静态方法调用，确保线程安全。
 */
public class EventBus : Singleton<EventBus>
{
    // 存储所有事件类型的订阅者列表
    private readonly Dictionary<Type, List<Delegate>> _subscribers = new Dictionary<Type, List<Delegate>>();

    /*
     * Subscribe<T>
     * 订阅特定类型的事件。
     * 参数：
     * - handler: 事件处理函数。
     */
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

    /*
     * Unsubscribe<T>
     * 取消订阅特定类型的事件。
     * 参数：
     * - handler: 事件处理函数。
     */
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

    /*
     * Publish<T>
     * 发布事件到所有订阅者，并进行网络同步。
     * 参数：
     * - eventData: 事件数据。
     */
    public static void Publish<T>(T eventData)
    {
        if (Instance == null) return;

        var type = typeof(T);

        // 判断是否为游戏事件（根据命名空间判断）
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
                        // 从事件中获取 timeline，如果没有则使用本地玩家的 timeline
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
            // 非游戏事件，仅本地发布
            Debug.LogWarning($"EventBus: 事件类型 {type} 非游戏事件，未进行网络同步。");
        }
    }

    /*
     * LocalPublish<T>
     * 本地分发事件。
     * 参数：
     * - eventData: 事件数据。
     */
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

    /*
     * PublishDynamic
     * 动态分发事件，用于网络反序列化后分发。
     * 参数：
     * - type: 事件类型。
     * - eventObj: 事件对象。
     */
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
}