using UnityEngine;

/*
 * 机关类：触发或切换机关状态，不会消失
 */
public class Mechanism : Interaction
{
    [Header("机关状态")]
    public bool isActivated;

    [Tooltip("可选：机关名称/ID")]
    public string mechanismId;

    /* 按 F 触发/切换机关，不消失 */
    public override void OnInteract(PlayerController player)
    {
        if (!CheckPuzzleConditions()) return;

        isActivated = !isActivated;
        string who = player != null ? player.gameObject.name : "Unknown";
        Debug.Log($"触发机关 -> 对象: {gameObject.name}, 玩家: {who}, 状态: {(isActivated ? "已激活" : "已关闭")}");

        // 可在此驱动动画、开门、解锁等逻辑
        // 示例：
        // GetComponent<Animator>()?.SetBool("Active", isActivated);
        // DoorController.Open() / Close()
    }
}