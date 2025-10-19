/* Core/Services/Network/NetworkMessageTypes.cs
 * 网络消息类型定义，包含所有网络通信协议的数据结构
 * 统一管理网络消息的格式和类型
 */

using Mirror;

public class NetworkMessageTypes
{
    /*
     * 线索共享消息（玩家间传输线索信息）
     */
    public class ClueSharingMessage
    {
        // 线索ID和内容
        // 发送者时间线信息
        // 消息验证令牌
    }

    /*
     * 解谜协作消息（同步解谜状态和操作）
     */
    public class PuzzleCooperationMessage
    {
        // 谜题ID和当前状态
        // 协作操作类型
        // 时间戳和序列号
    }

    /*
     * 时间线同步消息（不同时间线间的状态同步）
     */
    public class TimelineSyncMessage
    {
        // 时间线ID和版本
        // 同步数据类型和内容
        // 冲突解决策略
    }

    /*
     * 玩家状态消息（在线状态、准备状态等）
     */
    public class PlayerStatusMessage
    {
        // 玩家连接状态
        // 准备状态和角色
        // 网络延迟信息
    }

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
}