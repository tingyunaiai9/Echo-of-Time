using System;
using System.Text;
using Mirror;
using UnityEngine;
using System.Collections.Generic;

public class NetworkEventRelay : Singleton<NetworkEventRelay>
{
    private HashSet<string> processedEventGuids = new HashSet<string>();
    // 注册Mirror消息处理器（需在NetworkManager启动时调用）
    public void RegisterMessageHandlers()
    {
        // 服务器接收客户端事件并广播
        NetworkServer.RegisterHandler<NetworkMessageTypes.TimelineEventMessage>((conn, msg) =>
        {
            if (processedEventGuids.Contains(msg.eventGuid))
                return; // 已处理，忽略
    
            processedEventGuids.Add(msg.eventGuid);
    
            Type type = Type.GetType(msg.eventType);
            if (type != null)
            {
                string json = Encoding.UTF8.GetString(msg.eventData);
                object eventObj = JsonUtility.FromJson(json, type);
    
                EventBus.PublishDynamic(type, eventObj);
    
                // 服务器广播到其它客户端
                foreach (var c in NetworkServer.connections)
                {
                    c.Value.Send(msg);
                }
            }
            else
            {
                Debug.LogWarning($"[NetworkEventRelay] 未知事件类型: {msg.eventType}");
            }
        });
    
        // 客户端接收服务器广播
        NetworkClient.RegisterHandler<NetworkMessageTypes.TimelineEventMessage>(msg =>
        {
            if (processedEventGuids.Contains(msg.eventGuid))
                return; // 已处理，忽略
    
            processedEventGuids.Add(msg.eventGuid);
    
            Type type = Type.GetType(msg.eventType);
            if (type != null)
            {
                string json = Encoding.UTF8.GetString(msg.eventData);
                object eventObj = JsonUtility.FromJson(json, type);
                EventBus.PublishDynamic(type, eventObj);
            }
            else
            {
                Debug.LogWarning($"[NetworkEventRelay] 未知事件类型: {msg.eventType}");
            }
        });
    }

    // 游戏事件的网络同步入口
    public void RelayGameEvent<T>(T eventData)
    {
        var type = typeof(T);
    
        if (NetworkServer.active)
        {
            // 服务器：本地分发并广播到所有客户端
            // 为本地事件生成唯一eventGuid，并加入去重集
            string eventGuid = Guid.NewGuid().ToString();
            processedEventGuids.Add(eventGuid);
    
            // EventBus.LocalPublish(eventData);
            BroadcastToClients(eventData, 0, eventGuid); // 传递eventGuid
        }
        else if (NetworkClient.active && NetworkClient.isConnected)
        {
            // 客户端：发送到服务器，由服务器分发
            SendEventToServer(eventData);
        }
        else
        {
            // 单机或未联网，直接本地分发
            EventBus.LocalPublish(eventData);
        }
    }

    // 服务器广播到所有客户端
    private void BroadcastToClients<T>(T eventData, int sourceID, string eventGuid)
    {
        string eventType = typeof(T).FullName;
        byte[] data = SerializeEvent(eventData);

        var msg = new NetworkMessageTypes.TimelineEventMessage
        {
            eventType = eventType,
            eventData = data,
            sourceID = sourceID,
            eventGuid = eventGuid
        };

        if (NetworkServer.active)
        {
            NetworkServer.SendToAll(msg);
            Debug.Log($"[NetworkEventRelay] Mirror广播事件: {eventType} from Timeline {sourceID}, guid={eventGuid}");
        }
        else
        {
            Debug.LogWarning("[NetworkEventRelay] 仅服务器可广播Mirror事件");
        }
    }

    // 客户端发送事件到服务器
    private void SendEventToServer<T>(T eventData)
    {
        string eventType = typeof(T).FullName;
        byte[] data = SerializeEvent(eventData);
        string eventGuid = Guid.NewGuid().ToString(); // 生成唯一事件ID

        var msg = new NetworkMessageTypes.TimelineEventMessage
        {
            eventType = eventType,
            eventData = data,
            sourceID = 0,
            eventGuid = eventGuid
        };

        processedEventGuids.Add(eventGuid); // 本地标记已处理
        NetworkClient.Send(msg);
        Debug.Log($"[NetworkEventRelay] 客户端发送事件到服务器: {eventType}, guid={eventGuid}");
    }

    // 序列化事件
    private byte[] SerializeEvent<T>(T eventData)
    {
        string json = JsonUtility.ToJson(eventData);
        return Encoding.UTF8.GetBytes(json);
    }
}