/* Core/Services/Network/NetworkMessageTypes.cs
 * 网络消息类型定义，包含所有网络通信协议的数据结构
 * 统一管理网络消息的格式和类型
 */

using Mirror;
using UnityEngine;

namespace NetworkMessageTypes
{
    /*
     * 通用游戏事件消息（用于事件总线同步）
     */
    [System.Serializable]
    public struct TimelineEventMessage : NetworkMessage
    {
        public string eventType;
        public byte[] eventData;
        public int sourceID;
        public string eventGuid; // 唯一事件ID
    }

    /*
     * 图片分块传输消息
     */
    public struct ImageChunkMessage : NetworkMessage
    {
        public int imageId;       // 图片的唯一ID，防止多张图混淆
        public int chunkIndex;    // 当前是第几块
        public int totalChunks;   // 总共有几块
        public int timeline;      // 你的业务逻辑需要的 timeline 参数
        public byte[] chunkData;  // 实际的切片数据
    }
}

namespace Events
{
    public class GameStartedEvent
    {
        
    }

    /*     
     * 玩家状态相关事件
     */

    // 道具拾取事件
    [System.Serializable]
    public class ItemPickedUpEvent
    {
        public uint playerNetId;
        public string itemId;
        public string itemName;
        public string description;
        public Sprite icon;
    }

    // 线索更新事件
    [System.Serializable]
    public class ClueUpdatedEvent
    {
        public uint playerNetId;
        public string date;
        public string content;
    }

    // 聊天消息更新事件
    public class ChatMessageUpdatedEvent
    {
        public string MessageContent; // 消息内容
        public int timeline; // 时间线（0=Ancient, 1=Modern, 2=Future）
    }

    // 聊天图片更新事件
    public class ChatImageUpdatedEvent
    {
        public byte[] imageData; // 图片数据
        public int timeline; // 时间线（0=Ancient, 1=Modern, 2=Future）
    }

    public class InventoryUpdatedEvent
    {
        public uint playerNetId;
        public string[] inventoryItems;
    }

    // 线索被发现时发布
    [System.Serializable]
    public class ClueDiscoveredEvent
    {
        public uint playerNetId;
        public string clueId;
        public string clueText;
        public string clueDescription;
        public Sprite icon;
        public Sprite image;
    }

    // 玩家打开独立UI界面时发布
    [System.Serializable]
    public class FreezeEvent
    {
        public bool isOpen;
    }

    /*
     * 谜题与进度相关事件
     */
    // 玩家回答正确时发布
    [System.Serializable]
    public class AnswerCorrectEvent
    {
        public uint playerNetId;
    }

    // 开始剧情事件
    public class StartDialogueEvent
    {
        public DialogueData data;
        public StartDialogueEvent(DialogueData data) { this.data = data; }
    }

    // 剧情结束事件，用于恢复玩家移动
    public class EndDialogueEvent { }

    /*
     * 时间线交互事件
     */

    /*
     * UI与视听反馈事件
     */
    
    // 房间创建/加入进度事件
    public class RoomProgressEvent
    {
        public float Progress; // 0.0 to 1.0
        public string Message; // 当前状态描述
        public bool IsVisible; // 是否显示进度条
    }
}