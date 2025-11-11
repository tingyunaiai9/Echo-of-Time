/* Core/Services/EventBus/GameEvents.cs
 * 游戏事件枚举和常量定义
 * 集中管理所有游戏内可能触发的事件类型
 */

using UnityEngine;

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
        public string ClueEntry;
    }

    // 聊天消息更新事件
    public class ChatMessageUpdatedEvent
    {
        public string MessageContent; // 消息内容
        public MessageType MessageType; // 消息类型（Ancient, Modern, Future）
    }

    // 消息类型枚举
    public enum MessageType
    {
        Ancient, // 古代
        Modern,  // 现代
        Future   // 未来
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

    /*
     * 时间线交互事件
     */

    /*
     * UI与视听反馈事件
     */
}