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
        public string imageType;  // 图片类型: "Chat", "Clue"
    }
}
