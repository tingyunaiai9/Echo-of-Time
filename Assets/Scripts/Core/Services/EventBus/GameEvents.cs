/* Core/Services/EventBus/GameEvents.cs
 * 游戏事件枚举和常量定义
 * 集中管理所有游戏内可能触发的事件类型
 */

using UnityEngine;

namespace Events
{
    /*     
     * 玩家状态相关事件
     */
    public class PlayerSpawnedEvent
    {
        public uint playerNetId;
        public Vector3 spawnPosition;
    }

    public class PlayerHealthChangedEvent
    {
        public uint playerNetId;
        public int newHealth;
        public int delta;
    }

    public class PlayerDiedEvent
    {
        public uint playerNetId;
    }

    public class PlayerRespawnedEvent
    {
        public uint playerNetId;
        public Vector3 respawnPosition;
    }

    /*
     * 物品/交互相关事件
     */
    public class ItemPickedUpEvent
    {
        public uint playerNetId;
        public string itemId;
    }

    public class InventoryUpdatedEvent
    {
        public uint playerNetId;
        public string[] inventoryItems;
    }

    public class PuzzleInteractedEvent
    {
        public uint playerNetId;
        public string puzzleId;
        public string action;
    }

    /*
     * 谜题与进度相关事件
     */
    public class PuzzleStateChangedEvent
    {
        public string puzzleId;
        public string newState;
    }

    public class PuzzleSolvedEvent
    {
        public string puzzleId;
        public uint solverNetId;
    }

    public class ClueRevealedEvent
    {
        public string clueId;
        public string description;
    }

    public class GameProgressUpdatedEvent
    {
        public float progress; // 0~1
    }

    public class ObjectiveCompletedEvent
    {
        public string objectiveId;
    }

    /*
     * 时间线交互事件
     */
    public class TimelineStartEvent
    {
        public int timelineId;
    }

    public class TimelineSwitchedEvent
    {
        public int fromTimelineId;
        public int toTimelineId;
    }

    public class TimelineSyncEvent
    {
        public int timelineId;
        public string syncData;
    }

    public class ParadoxDetectedEvent
    {
        public int timelineId;
        public string paradoxInfo;
    }

    /*
     * 网络通信事件
     */
    public class ClientConnectedEvent
    {
        public uint clientNetId;
    }

    public class ClientDisconnectedEvent
    {
        public uint clientNetId;
    }

    public class LocalPlayerReadyEvent
    {
        public uint playerNetId;
    }

    /*
     * UI与视听反馈事件
     */
    public class UpdateHUDEvent
    {
        public uint playerNetId;
        public string hudType;
        public string hudValue;
    }

    public class ToggleMenuEvent
    {
        public bool isOpen;
    }

    public class PlaySFXEvent
    {
        public string sfxName;
        public Vector3 position;
    }

    public class PlayVFXEvent
    {
        public string vfxName;
        public Vector3 position;
    }

    public class SceneTransitionEvent
    {
        public string sceneName;
    }
}