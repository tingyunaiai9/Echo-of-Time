/* Core/Services/EventBus/GameEvents.cs
 * 游戏事件枚举和常量定义
 * 集中管理所有游戏内可能触发的事件类型
 */

/*
 * 游戏事件定义，包含所有跨时空合作相关的事件类型
 */
public static class GameEvents
{
    /*
     * 线索相关事件（收集、分享、更新）
     */
    public class ClueEvents
    {
        // 线索收集完成事件
        // 线索分享请求事件
        // 线索验证结果事件
    }

    /*
     * 解谜进度事件（谜题状态变化、合作解谜触发）
     */
    public class PuzzleEvents
    {
        // 谜题状态更新事件
        // 合作解谜触发事件
        // 时间线同步事件
    }

    /*
     * 时间线交互事件（时间线同步、时空传输）
     */
    public class TimelineEvents
    {
        // 时间线切换事件
        // 跨时间线通信事件
        // 时空悖论检测事件
    }

    /*
     * 网络通信事件（消息接收、连接状态）
     */
    public class NetworkEvents
    {
        // 玩家连接事件
        // 消息接收事件
        // 网络延迟事件
    }
}