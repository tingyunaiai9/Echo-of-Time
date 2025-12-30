/*
 * GameEvents.cs
 * 定义游戏中使用的所有事件类型。
 * 包括玩家状态事件、谜题进度事件、剧情事件等。
 */

using UnityEngine;

namespace Events
{
    /*
     * GameStartedEvent
     * 游戏开始事件。
     */
    public class GameStartedEvent
    {

    }

    /* 玩家状态相关事件 */

    /*
     * ItemPickedUpEvent
     * 道具拾取事件。
     * 包含拾取的玩家 ID、道具信息等。
     */
    public class ItemPickedUpEvent
    {
        public uint playerNetId; // 玩家网络 ID
        public string itemId; // 道具 ID
        public int instanceId; // 物体实例 ID
        public Sprite icon; // 道具图标
        public string description; // 道具描述
        public int quantity; // 道具数量
    }

    /*
     * ChatMessageUpdatedEvent
     * 聊天消息更新事件。
     */
    public class ChatMessageUpdatedEvent
    {
        public string MessageContent; // 消息内容
        public int timeline; // 时间线（0=古代，1=现代，2=未来）
    }

    /*
     * ChatImageUpdatedEvent
     * 聊天图片更新事件。
     */
    public class ChatImageUpdatedEvent
    {
        public byte[] imageData; // 图片数据
        public int timeline; // 时间线（0=古代，1=现代，2=未来）
    }

    /*
     * InventoryUpdatedEvent
     * 背包更新事件。
     */
    public class InventoryUpdatedEvent
    {
        public uint playerNetId; // 玩家网络 ID
        public string[] inventoryItems; // 背包中的道具列表
    }

    /*
     * ClueSharedEvent
     * 线索共享事件。
     */
    public class ClueSharedEvent
    {
        public int clueId = 0; // 线索 ID
        public int timeline; // 时间线（0=古代，1=现代，2=未来）
        public int level; // 层数（1=第一层，2=第二层，3=第三层）
        public byte[] imageData = null; // 图片数据
        public string text = null; // 文本内容
    }

    /*
     * ClueDiscoveredEvent
     * 线索被发现时发布的事件。
     */
    public class ClueDiscoveredEvent
    {
        public bool isKeyClue = false; // 是否为关键线索
        public uint playerNetId; // 玩家网络 ID
        public string clueId; // 线索 ID
        public string clueText; // 线索文本
        public string clueDescription; // 线索描述
        public Sprite icon; // 线索图标
        public Sprite image; // 线索图片
    }

    /* 谜题与进度相关事件 */

    /*
     * LevelProgressEvent
     * 每一层探索进度事件。
     */
    public class LevelProgressEvent
    {
    }

    /*
     * PuzzleCompletedEvent
     * 谜题完成事件。
     */
    public class PuzzleCompletedEvent
    {
        public string sceneName; // 谜题场景名称
    }

    /*
     * LevelChangedEvent
     * 层级变化事件。
     */
    public class LevelChangedEvent
    {
        public int oldLevel; // 旧层级
        public int newLevel; // 新层级
    }

    /*
     * AnswerCorrectEvent
     * 玩家回答正确时发布的事件。
     */
    public class AnswerCorrectEvent
    {
        public uint playerNetId; // 玩家网络 ID
    }

    /* 剧情相关事件 */

    /*
     * StartDialogueEvent
     * 开始剧情事件。
     */
    public class StartDialogueEvent
    {
        public DialogueData data; // 剧情数据
        public StartDialogueEvent(DialogueData data) { this.data = data; }
    }

    /*
     * DialogueEndEvent
     * 剧情结束事件。
     */
    public class DialogueEndEvent
    {
    }

    /*
     * IntroEndEvent
     * 引导结束事件。
     */
    public class IntroEndEvent
    {
    }

    /*
     * RoomProgressEvent
     * 房间创建/加入进度事件。
     */
    public class RoomProgressEvent
    {
        public float Progress; // 进度（0.0 到 1.0）
        public string Message; // 当前状态描述
        public bool IsVisible; // 是否显示进度条
    }
}