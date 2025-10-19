/* Core/Services/EventBus/EventBus.cs
 * 事件总线系统核心，提供事件的发布、订阅和分发功能
 * 实现游戏内模块间的松耦合通信
 */

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Mirror;

public class EventBus : Singleton<EventBus>
{
    // 存储所有事件类型的订阅者列表
    private readonly Dictionary<Type, List<Delegate>> _subscribers = new Dictionary<Type, List<Delegate>>();

    /* 订阅特定类型的事件 */
    public void Subscribe<T>(Action<T> handler)
    {
        var type = typeof(T);
        if (!_subscribers.ContainsKey(type))
        {
            _subscribers[type] = new List<Delegate>();
        }
        if (!_subscribers[type].Contains(handler))
        {
            _subscribers[type].Add(handler);
        }
    }

    /* 取消事件订阅 */
    public void Unsubscribe<T>(Action<T> handler)
    {
        var type = typeof(T);
        if (_subscribers.ContainsKey(type))
        {
            _subscribers[type].Remove(handler);
            if (_subscribers[type].Count == 0)
            {
                _subscribers.Remove(type);
            }
        }
    }

    /* 发布事件到所有订阅者 */
    public void Publish<T>(T eventData)
    {
        var type = typeof(T);
        if (_subscribers.ContainsKey(type))
        {
            // 拷贝一份，防止订阅者在回调中修改列表
            var handlers = new List<Delegate>(_subscribers[type]);
            foreach (var handler in handlers)
            {
                try
                {
                    ((Action<T>)handler)?.Invoke(eventData);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"EventBus: 事件处理异常 {ex}");
                }
            }
        }
    }

    /* 动态分发（用于网络反序列化后分发） */
    public void PublishDynamic(Type type, object eventObj)
    {
        if (_subscribers.ContainsKey(type))
        {
            var handlers = new List<Delegate>(_subscribers[type]);
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

    // Mirror网络消息定义
    public struct TimelineEventMessage : NetworkMessage
    {
        public string eventType;
        public byte[] eventData;
        public int sourceTimeline;
    }

    /* Mirror跨时间线事件广播 */
    public void BroadcastCrossTimeline<T>(T eventData, int sourceTimeline)
    {
        string eventType = typeof(T).FullName;
        byte[] data = SerializeEvent(eventData);

        TimelineEventMessage msg = new TimelineEventMessage
        {
            eventType = eventType,
            eventData = data,
            sourceTimeline = sourceTimeline
        };

        // 仅服务器可广播
        if (NetworkServer.active)
        {
            NetworkServer.SendToAll(msg);
            Debug.Log($"[EventBus] Mirror广播事件: {eventType} from Timeline {sourceTimeline}");
        }
        else
        {
            Debug.LogWarning("[EventBus] 仅服务器可广播Mirror事件");
        }
    }

    // 简单序列化（使用JsonUtility，事件类需为public且字段为public）
    private byte[] SerializeEvent<T>(T eventData)
    {
        string json = JsonUtility.ToJson(eventData);
        return Encoding.UTF8.GetBytes(json);
    }

    // 事件消息处理注册（在NetworkManager或游戏初始化时调用）
    public static void RegisterMirrorHandlers(EventBus bus)
    {
        NetworkServer.RegisterHandler<TimelineEventMessage>((conn, msg) =>
        {
            Type type = Type.GetType(msg.eventType);
            if (type != null)
            {
                string json = Encoding.UTF8.GetString(msg.eventData);
                object eventObj = JsonUtility.FromJson(json, type);
                bus.PublishDynamic(type, eventObj);
            }
            else
            {
                Debug.LogWarning($"[EventBus] 未知事件类型: {msg.eventType}");
            }
        });
    }
}