using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Mirror;
using Events;

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

    /* 发布事件到所有订阅者，并进行网络同步 */
    public void Publish<T>(T eventData)
    {
        var type = typeof(T);

        // 判断是否为GameEvent（可根据实际需求调整类型判断）
        bool isGameEvent = type.Namespace == "Events";

        // 网络同步逻辑
        if (isGameEvent)
        {
            if (NetworkServer.active)
            {
                // 服务器：本地分发并广播到所有客户端
                LocalPublish(eventData);
                BroadcastCrossTimeline(eventData, 0); // 0为示例timelineId
            }
            else if (NetworkClient.active)
            {
                // 客户端：发送到服务器，由服务器分发
                SendEventToServer(eventData);
            }
            else
            {
                // 单机或未联网，直接本地分发
                LocalPublish(eventData);
            }
        }
        else
        {
            // 非GameEvent，直接本地分发
            LocalPublish(eventData);
        }
    }

    // 本地分发事件
    private void LocalPublish<T>(T eventData)
    {
        var type = typeof(T);
        if (_subscribers.ContainsKey(type))
        {
            var handlers = new List<Delegate>(_subscribers[type]);
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
    [System.Serializable]
    public struct TimelineEventMessage : NetworkMessage
    {
        public string eventType;
        public byte[] eventData;
        public int sourceTimeline;
    }

    /* Mirror跨时间线事件广播（服务器端调用） */
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

    // 客户端向服务器发送事件
    private void SendEventToServer<T>(T eventData)
    {
        string eventType = typeof(T).FullName;
        byte[] data = SerializeEvent(eventData);

        TimelineEventMessage msg = new TimelineEventMessage
        {
            eventType = eventType,
            eventData = data,
            sourceTimeline = 0 // 可根据实际需求设置
        };

        NetworkClient.Send(msg);
        Debug.Log($"[EventBus] 客户端发送事件到服务器: {eventType}");
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
        // 服务器接收客户端事件并广播
        NetworkServer.RegisterHandler<TimelineEventMessage>((conn, msg) =>
        {
            Type type = Type.GetType(msg.eventType);
            if (type != null)
            {
                string json = Encoding.UTF8.GetString(msg.eventData);
                object eventObj = JsonUtility.FromJson(json, type);

                // 服务器本地分发
                bus.PublishDynamic(type, eventObj);

                // 服务器广播到其它客户端（不回发给发送者）
                foreach (var c in NetworkServer.connections)
                {
                    if (c.Value != conn)
                    {
                        c.Value.Send(msg);
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[EventBus] 未知事件类型: {msg.eventType}");
            }
        });

        // 客户端接收服务器广播
        NetworkClient.RegisterHandler<TimelineEventMessage>(msg =>
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