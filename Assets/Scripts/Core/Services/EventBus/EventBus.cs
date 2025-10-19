using System;
using System.Collections.Generic;
using UnityEngine;

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

        if (isGameEvent)
        {
            NetworkEventRelay.Instance.RelayGameEvent(eventData);
        }
        else
        {
            // 非GameEvent，直接本地分发
            LocalPublish(eventData);
        }
    }

    // 本地分发事件
    public void LocalPublish<T>(T eventData)
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
}